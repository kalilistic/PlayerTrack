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
        private readonly Queue<LodestoneRequest> requestQueue = new();
        private bool isProcessing;
        private long lodestoneCooldown;

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
            if (this.isProcessing) return;
            this.isProcessing = true;
            try
            {
                // check if should reprocess
                if (DateUtil.CurrentTime() > this.lodestoneCooldown)
                {
                    this.lodestoneCooldown =
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
                        var response = new LodestoneResponse();
                        var result = this.GetCharacterIdAsync(request.PlayerName, request.WorldName).Result;
                        if (result.StatusCode == HttpStatusCode.OK)
                        {
                            var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
                            response.Status = LodestoneStatus.Verified;
                            response.LodestoneId = (uint)json!.lodestoneId;
                            response.PlayerKey = request.PlayerKey;
                            this.plugin.PlayerService.UpdateLodestone(response);
                            requestFailureCount = 0;
                            requestedFinished = true;
                        }
                        else if (result.StatusCode == HttpStatusCode.NotFound)
                        {
                            Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " was an invalid player.");
                            response.PlayerKey = request.PlayerKey;
                            response.Status = LodestoneStatus.Failed;
                            this.plugin.PlayerService.UpdateLodestone(response);
                            requestFailureCount = 0;
                            requestedFinished = true;
                        }
                        else if (result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " not found on lodestone.");
                            response.PlayerKey = request.PlayerKey;
                            response.Status = LodestoneStatus.Failed;
                            this.plugin.PlayerService.UpdateLodestone(response, false);
                            requestFailureCount = 0;
                            requestedFinished = true;
                        }
                        else
                        {
                            Logger.LogDebug(request.PlayerName + "/" + request.WorldName + " failed to lookup.");
                            requestFailureCount++;
                        }
                    }

                    if (requestFailureCount >= this.plugin.Configuration.LodestoneMaxRetry)
                    {
                        this.LodestoneCooldown =
                            DateUtil.CurrentTime() + this.plugin.Configuration.LodestoneCooldownDuration;
                        Logger.LogDebug($"Lodestone is unavailable so setting cooldown for all requests.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process lodestone requests.");
            }

            this.isProcessing = false;
        }

        private bool ShouldProcess()
        {
            if (this.plugin.Configuration.RestrictInCombat &&
                PlayerTrackPlugin.Condition.InCombat()) return false;
            return true;
        }

        private async Task<HttpResponseMessage> GetCharacterIdAsync(string characterName, string worldName)
        {
            var url = "https://api.kalilistic.io/lodestone/player-id?playerName=" + characterName +
                      "&worldName=" + worldName;
            return await this.httpClient.GetAsync(new Uri(url));
        }
    }
}
