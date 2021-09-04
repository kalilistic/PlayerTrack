using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

using Dalamud.DrunkenToad;
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
        private readonly Queue<LodestoneRequest> requestQueue = new ();
        private bool isProcessing;
        private long lodestoneReprocess;
        private int subsequentFailures;

        /// <summary>
        /// Initializes a new instance of the <see cref="LodestoneService"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin.</param>
        public LodestoneService(PlayerTrackPlugin plugin)
        {
            var httpClientHandler = new HttpClientHandler();
            this.plugin = plugin;
            this.lodestoneReprocess = DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneReprocessDelay;
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
                return new LodestoneRequest[0];
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

        private bool ShouldProcess()
        {
            if (this.plugin.Configuration.RestrictInCombat &&
                PlayerTrackPlugin.Condition.InCombat()) return false;
            return true;
        }

        private void ProcessRequests(object source, ElapsedEventArgs e)
        {
            if (!this.plugin.IsDoneLoading) return;
            if (!this.ShouldProcess()) return;
            if (this.isProcessing) return;
            this.isProcessing = true;
            try
            {
                // check if should reprocess
                if (DateUtil.CurrentTime() > this.lodestoneReprocess)
                {
                    this.lodestoneReprocess =
                        DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneReprocessDelay;
                    this.plugin.PlayerService.ReprocessPlayersForLodestone();
                    this.isProcessing = false;
                    return;
                }

                // process requests
                var requestFailureCount = 0;
                while (this.requestQueue.Count > 0 &&
                       DateUtil.CurrentTime() > this.LodestoneCooldown &&
                       this.plugin.Configuration.SyncToLodestone &&
                       this.ShouldProcess())
                {
                    var request = this.requestQueue.Dequeue();
                    var requestedFinished = false;
                    while (!requestedFinished && requestFailureCount < this.plugin.Configuration.LodestoneMaxRetry)
                    {
                        var response = this.GetCharacterId(request);
                        if (response.Status == LodestoneStatus.Verified)
                        {
                            this.plugin.PlayerService.UpdateLodestone(response);
                            requestFailureCount = 0;
                            requestedFinished = true;
                        }
                        else
                        {
                            requestFailureCount++;
                        }
                    }

                    if (requestFailureCount >= this.plugin.Configuration.LodestoneMaxRetry)
                    {
                        var assumeLodestoneAvailable = true;
                        if (this.subsequentFailures >= this.plugin.Configuration.LodestoneMaxSubsequentFailures)
                        {
                            assumeLodestoneAvailable = false;
                            this.subsequentFailures = 0;
                        }

                        if (assumeLodestoneAvailable || this.IsLodestoneAvailableInternal())
                        {
                            var response = new LodestoneResponse
                            {
                                PlayerKey = request.PlayerKey,
                                Status = LodestoneStatus.Failed,
                            };
                            this.plugin.PlayerService.UpdateLodestone(response);
                            requestFailureCount = 0;
                            Logger.LogDebug($"Lodestone is available so marking player as failed.");
                        }
                        else
                        {
                            this.LodestoneCooldown =
                                DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneCooldownDuration;
                            Logger.LogDebug($"Lodestone is unavailable so setting cooldown for all requests.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process lodestone requests.");
            }

            this.isProcessing = false;
        }

        private bool IsLodestoneAvailableInternal()
        {
            var request = new LodestoneRequest
            {
                PlayerKey = "MACARONI_GRATIN_90",
                PlayerName = "Macaroni Gratin",
                WorldName = "Aegis",
            };
            var result = this.GetCharacterId(request);
            return result.Status == LodestoneStatus.Verified;
        }

        private LodestoneResponse GetCharacterId(LodestoneRequest request)
        {
            var response = new LodestoneResponse();
            try
            {
                var result = this.GetCharacterIdAsync(request.PlayerName, request.WorldName).Result;
                if (result.StatusCode != HttpStatusCode.OK) return response;
                var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
                if (json == null) return response;
                if (json.Results == null) return response;
                if (json.Results[0] == null) return response;
                response.Status = LodestoneStatus.Verified;
                response.LodestoneId = (uint)json.Results[0].ID;
                response.PlayerKey = request.PlayerKey;
                return response;
            }
            catch
            {
                return response;
            }
        }

        private async Task<HttpResponseMessage> GetCharacterIdAsync(string characterName, string worldName)
        {
            var url = "https://xivapi.com/character/search?name=" + characterName +
                      "&server=" + worldName + "&columns=ID";
            return await this.httpClient.GetAsync(new Uri(url));
        }
    }
}
