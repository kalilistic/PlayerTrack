using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;

public class LodestoneService : IDisposable
{
    private readonly ConcurrentDictionary<int, LodestoneLookup> lodestoneLookups = new();
    private readonly ConcurrentQueue<LodestoneRequest> lodestoneQueue = new();
    private readonly object locker = new();
    private HttpClient httpClient = null!;
    private Timer processTimer = null!;
    private bool isProcessing;
    private long lodestoneCallAvailableAt;
    private int lodestoneSequentialFailureCount;
    private long overworldCallAvailableAt;

    public LodestoneService()
    {
        this.SetupHttpClient();
        this.SetupTimer();
        this.LoadRequests();
    }

    public void Start() => this.processTimer.Start();

    public void Stop()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.Stop()");
        this.processTimer.Elapsed -= this.ProcessRequests;
        this.processTimer.Stop();
    }

    public void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.Dispose()");
        this.lodestoneQueue.Clear();
        this.httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetupHttpClient()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.SetupHttpClient()");
        var httpClientHandler = new HttpClientHandler();
        this.httpClient = new HttpClient(httpClientHandler, true)
        {
            Timeout = TimeSpan.FromMilliseconds(60000),
        };
        this.httpClient.DefaultRequestHeaders.Add("Application-Name", "PlayerTrack");
        this.httpClient.DefaultRequestHeaders.Add("Application-Version", ServiceContext.ConfigService.GetConfig().PluginVersion.ToString());
        this.httpClient.DefaultRequestHeaders.Add("Acceleration-Enabled", ServiceContext.ConfigService.GetConfig().AccelerateLodestoneLookup.ToString());
    }

    private void SetupTimer()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.SetupTimer()");
        this.processTimer = new Timer { Interval = 15000, Enabled = true };
        this.processTimer.Elapsed += this.ProcessRequests;
    }

    private void ProcessRequests(object? sender, ElapsedEventArgs e)
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.ProcessRequests()");
        try
        {
            if (!ServiceContext.ConfigService.GetConfig().LodestoneEnableLookup)
            {
                DalamudContext.PluginLog.Verbose("Lodestone lookup disabled, skipping.");
                return;
            }

            if (UnixTimestampHelper.CurrentTime() < this.lodestoneCallAvailableAt)
            {
                DalamudContext.PluginLog.Verbose("Lodestone lookup not available, skipping.");
                return;
            }

            var territoryType = DalamudContext.ClientStateHandler.TerritoryType;
            var inOverworld = !DalamudContext.DataManager.Locations[territoryType].InContent();
            if (inOverworld && UnixTimestampHelper.CurrentTime() < this.overworldCallAvailableAt)
            {
                DalamudContext.PluginLog.Verbose("Overworld lookup not available, skipping.");
                return;
            }

            lock (this.locker)
            {
                if (this.isProcessing)
                {
                    DalamudContext.PluginLog.Verbose("Lodestone lookup already processing, skipping.");
                    return;
                }

                this.isProcessing = true;
            }

            // load requests and quit if nothing to process
            DalamudContext.PluginLog.Verbose("Loading lodestone requests.");
            var requestsToProcess = this.LoadRequests();
            if (!requestsToProcess)
            {
                DalamudContext.PluginLog.Verbose("No lodestone requests to process, skipping.");
                lock (this.locker)
                {
                    this.isProcessing = false;
                }

                return;
            }

            // batch requests and send (up to 10k at a time)
            var batchedRequests = this.BatchLodestoneRequests();
            this.SendBatchRequest(batchedRequests);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to process lodestone requests.");
        }
        finally
        {
            lock (this.locker)
            {
                this.isProcessing = false;
            }
        }
    }

    private void SendBatchRequest(List<LodestoneRequest> batchedRequests)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.SendBatchRequest(): {batchedRequests.Count}");
        var response = this.GetCharacterIdsAsync(batchedRequests).Result;
        if (response.StatusCode != HttpStatusCode.OK)
        {
            this.lodestoneSequentialFailureCount++;
            if (this.lodestoneSequentialFailureCount > 3)
            {
                this.lodestoneCallAvailableAt = UnixTimestampHelper.CurrentTime() + 7200000;
            }

            throw new HttpRequestException($"Failed response from lodestone request: {response.StatusCode}");
        }

        this.lodestoneSequentialFailureCount = 0;

        var responsePayload = response.Content.ReadAsStringAsync().Result;
        var responseObjects = JsonConvert.DeserializeObject<LodestoneResponse[]>(responsePayload);
        if (responseObjects == null)
        {
            throw new HttpRequestException("Failed to deserialize lodestone response.");
        }

        foreach (var request in batchedRequests)
        {
            var lodestoneResponse = responseObjects.FirstOrDefault(resp =>
                resp.PlayerName.Equals(request.PlayerName, StringComparison.OrdinalIgnoreCase) &&
                resp.WorldName.Equals(request.WorldName, StringComparison.OrdinalIgnoreCase));
            this.lodestoneLookups.TryGetValue(request.PlayerId, out var lookup);
            if (lookup == null)
            {
                throw new HttpRequestException($"Failed to find lodestone lookup for player id {request.PlayerId}.");
            }

            if (lodestoneResponse is not { StatusCode: 200 })
            {
                lookup.FlagAsFailed();
            }
            else
            {
                lookup.FlagAsSuccessful(lodestoneResponse.LodestoneId);
            }

            this.lodestoneLookups.TryRemove(request.PlayerId, out _);
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
            PlayerLodestoneService.UpdateLodestone(lookup);
            this.lodestoneCallAvailableAt = UnixTimestampHelper.CurrentTime() + 10000;
            this.overworldCallAvailableAt = ServiceContext.ConfigService.GetConfig().AccelerateLodestoneLookup
                ? UnixTimestampHelper.CurrentTime() + 30000
                : UnixTimestampHelper.CurrentTime() + 3600000;
        }
    }

    private async Task<HttpResponseMessage> GetCharacterIdsAsync(List<LodestoneRequest> requests)
    {
        var content = JsonConvert.SerializeObject(requests);
        var stringContent = new StringContent(content);
        return await this.httpClient.PostAsync(
            new Uri("https://api.kalilistic.io/v1/lodestone/players"),
            stringContent);
    }

    private bool LoadRequests()
    {
        try
        {
            if (!this.lodestoneQueue.IsEmpty)
            {
                return true;
            }

            this.LoadPendingRequests();
            this.LoadFailedRequests();
            return !this.lodestoneQueue.IsEmpty;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private List<LodestoneRequest> BatchLodestoneRequests()
    {
        var lodestoneRequests = new List<LodestoneRequest>();
        while (!this.lodestoneQueue.IsEmpty && lodestoneRequests.Count < 10000)
        {
            var isSuccessful = this.lodestoneQueue.TryDequeue(out var lodestoneRequest);
            if (!isSuccessful || lodestoneRequest == null)
            {
                continue;
            }

            lodestoneRequests.Add(lodestoneRequest);
        }

        return lodestoneRequests;
    }

    private void LoadPendingRequests()
    {
        var pendingLookups =
            RepositoryContext.LodestoneRepository.GetRequestsByStatus(LodestoneStatus.Unverified) ??
            new List<LodestoneLookup>();
        foreach (var lookup in pendingLookups)
        {
            var request = new LodestoneRequest(lookup.PlayerId, lookup.PlayerName, lookup.WorldName);
            this.lodestoneQueue.Enqueue(request);
            this.lodestoneLookups.TryAdd(lookup.PlayerId, lookup);
        }
    }

    private void LoadFailedRequests()
    {
        var failedLookups =
            RepositoryContext.LodestoneRepository.GetRequestsByStatus(LodestoneStatus.Failed) ??
            new List<LodestoneLookup>();
        var currentTime = UnixTimestampHelper.CurrentTime();
        foreach (var lookup in failedLookups)
        {
            if (currentTime <= lookup.Updated + 172800000) continue;
            var request = new LodestoneRequest(lookup.PlayerId, lookup.PlayerName, lookup.WorldName);
            this.lodestoneQueue.Enqueue(request);
            this.lodestoneLookups.TryAdd(lookup.PlayerId, lookup);
        }
    }
}
