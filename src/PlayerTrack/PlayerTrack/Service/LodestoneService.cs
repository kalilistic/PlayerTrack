using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

using Dalamud.DrunkenToad;
using Dalamud.Logging;
using Newtonsoft.Json;

using Timer = System.Timers.Timer;

namespace PlayerTrack
{
    /// <summary>
    /// Lodestone Service.
    /// </summary>
    public class LodestoneService
    {
        /// <summary>
        /// Lodestone cooldown (unix ms).
        /// </summary>
        public long LodestoneCooldown = DateUtil.CurrentTime();
        private readonly HttpClient httpClient;
        private readonly Timer onRequestTimer;
        private readonly PlayerTrackPlugin plugin;
        private readonly Queue<LodestoneRequest> requestQueue = new();
        private readonly int maxRequestCount = 60;
        private bool isProcessing;
        private long lodestoneCooldown;
        private long lodestoneLastRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="LodestoneService"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin.</param>
        public LodestoneService(PlayerTrackPlugin plugin)
        {
            var httpClientHandler = new HttpClientHandler();
            this.plugin = plugin;
            this.lodestoneCooldown = DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneReprocessDelay;
            this.httpClient = new HttpClient(httpClientHandler, true)
            {
                Timeout = TimeSpan.FromMilliseconds(this.plugin.Configuration.LodestoneTimeout),
            };
            this.onRequestTimer = new Timer
                { Interval = this.plugin.Configuration.LodestoneQueueFrequency, Enabled = true };
            this.onRequestTimer.Elapsed += this.ProcessRequests;
        }

        /// <summary>
        /// Get requests.
        /// </summary>
        /// <returns>outstanding requests.</returns>
        public LodestoneRequest[] GetRequests()
        {
            try
            {
                return this.requestQueue.ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get lodestone requests.");
                return Array.Empty<LodestoneRequest>();
            }
        }

        /// <summary>
        /// Add lodestone request.
        /// </summary>
        /// <param name="request">lodestone request to add.</param>
        public void AddRequest(LodestoneRequest request)
        {
            try
            {
                if (this.requestQueue.Any(existingRequest => existingRequest.PlayerKey == request.PlayerKey)) return;
                this.requestQueue.Enqueue(request);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Dispose lodestone service.
        /// </summary>
        public void Dispose()
        {
            this.onRequestTimer.Elapsed -= this.ProcessRequests;
            this.onRequestTimer.Stop();
            this.requestQueue.Clear();
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Open lodestone profile.
        /// </summary>
        /// <param name="lodestoneId">lodestone id.</param>
        public void OpenLodestoneProfile(uint lodestoneId)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://" + this.plugin.Configuration.LodestoneLocale + ".finalfantasyxiv.com/lodestone/character/" + lodestoneId,
                UseShellExecute = true,
            });
        }

        /// <summary>
        /// Check if lodestone is available by lodestone cooldown state.
        /// </summary>
        /// <returns>indicator if lodestone is up.</returns>
        public bool IsLodestoneAvailable()
        {
            return this.LodestoneCooldown < DateUtil.CurrentTime();
        }

        private void ProcessRequests(object? source, ElapsedEventArgs e)
        {
            if (!this.plugin.IsDoneLoading) return;
            if (!this.ShouldProcess()) return;
            if (DateUtil.CurrentTime() < this.lodestoneLastRequest + this.plugin.Configuration.LodestoneBatchDelay) return;
            if (this.isProcessing) return;
            this.isProcessing = true;

            // check if should reprocess
            if (DateUtil.CurrentTime() > this.lodestoneCooldown)
            {
                this.lodestoneCooldown =
                    DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneReprocessDelay;
                this.plugin.PlayerService.ReprocessPlayersForLodestone();
                this.isProcessing = false;
                return;
            }

            try
            {
                PluginLog.Debug("LODESTONE REQUEST: START");

                // set time for batching requests
                this.lodestoneLastRequest = DateUtil.CurrentTime();

                // build list of requests
                var lodestoneRequests = new List<LodestoneRequest>();
                while (this.requestQueue.Count > 0 && lodestoneRequests.Count < this.maxRequestCount)
                {
                    lodestoneRequests.Add(this.requestQueue.Dequeue());
                }

                var requestedFinished = false;
                var requestFailureCount = 0;
                while (!requestedFinished && requestFailureCount < this.plugin.Configuration.LodestoneMaxRetry)
                {
                    // call lodestone API
                    var result = this.GetCharacterIdsAsync(lodestoneRequests).Result;
                    PluginLog.Debug($"LODESTONE RESPONSE: STATUS CODE: {result.StatusCode}.");

                    // handle full request success
                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        // mark as complete and reset failure count
                        requestFailureCount = 0;
                        requestedFinished = true;

                        // deserialize player list response
                        var responseList = JsonConvert.DeserializeObject<LodestoneResponse[]>(result.Content.ReadAsStringAsync().Result);
                        PluginLog.Debug($"LODESTONE RESPONSE: RESPONSE COUNT: {responseList?.Length}.");

                        // handle empty response
                        if (responseList == null)
                        {
                            PluginLog.Debug($"LODESTONE REQUEST: ATTEMPT#{requestFailureCount + 1} FAILED.");
                            requestFailureCount++;
                            continue;
                        }

                        // loop through each player lookup
                        foreach (var request in lodestoneRequests)
                        {
                            // find corresponding response
                            LodestoneResponse? response = null;
                            foreach (var responseIn in responseList)
                            {
                                if (responseIn.PlayerName.Equals(request.PlayerName) &&
                                    responseIn.WorldName.Equals(request.WorldName))
                                {
                                    response = responseIn;
                                }
                            }

                            // handle individual lookup status codes
                            if (response == null)
                            {
                                response = new LodestoneResponse();
                                Logger.LogVerbose(request.PlayerName + "/" + request.WorldName + " couldn't find matching response.");
                                response.PlayerKey = request.PlayerKey;
                                response.Status = LodestoneStatus.Failed;
                                this.plugin.PlayerService.UpdateLodestone(response);
                            }
                            else if (response.StatusCode == 200)
                            {
                                Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " found successfully!");
                                response.Status = LodestoneStatus.Verified;
                                response.LodestoneId = response.LodestoneId;
                                response.PlayerKey = request.PlayerKey;
                                this.plugin.PlayerService.UpdateLodestone(response);
                            }
                            else if (response.StatusCode == 404)
                            {
                                PluginLog.LogDebug(request.PlayerName + "/" + request.WorldName + " was not found.");
                                response.PlayerKey = request.PlayerKey;
                                response.Status = LodestoneStatus.Failed;
                                this.plugin.PlayerService.UpdateLodestone(response);
                            }
                            else if (response.StatusCode == 400)
                            {
                                Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " was invalid.");
                                response.PlayerKey = request.PlayerKey;
                                response.Status = LodestoneStatus.Failed;
                                this.plugin.PlayerService.UpdateLodestone(response, false);
                            }
                            else
                            {
                                Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " lookup went wrong.");
                                response.PlayerKey = request.PlayerKey;
                                response.Status = LodestoneStatus.Failed;
                                this.plugin.PlayerService.UpdateLodestone(response);
                            }
                        }
                    }

                    // handle full request failure
                    else
                    {
                        PluginLog.Debug($"LODESTONE REQUEST: ATTEMPT#{requestFailureCount + 1} FAILED.");
                        requestFailureCount++;
                    }

                    // activate cooldown if failed out
                    if (requestFailureCount >= this.plugin.Configuration.LodestoneMaxRetry)
                    {
                        this.LodestoneCooldown =
                            DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneCooldownDuration;
                        PluginLog.Debug($"Lodestone is unavailable so setting cooldown for all requests.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LODESTONE REQUEST: FAILURE");
            }

            PluginLog.Debug("LODESTONE REQUEST: FINISHED");
            this.isProcessing = false;
        }

        private bool ShouldProcess()
        {
            if (this.plugin.Configuration.RestrictInCombat &&
                PlayerTrackPlugin.Condition.InCombat()) return false;
            return true;
        }

        private async Task<HttpResponseMessage> GetCharacterIdsAsync(List<LodestoneRequest> requests)
        {
            var content = new StringContent(JsonConvert.SerializeObject(requests));
            return await this.httpClient.PostAsync(new Uri("https://api.kalilistic.io/v1/lodestone/players"), content);
        }
    }
}
