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
    private const int batch_size = 30;
    private const int hourly_refresh_limit = 15;
    private const int processInterval = 60000;
    private const int sequentialFailureLimit = 2;
    private const int serviceCooldownMs = 900000; // 15 minutes
    private const long retryInterval = 172800000; // 48 hours
    private const string api_uri = "https://api.kal-xiv.net/v2/lodestone";
    private readonly ConcurrentDictionary<int, LodestoneLookup> lodestoneBatchLookups = new();
    private readonly ConcurrentQueue<LodestoneBatchRequest> lodestoneBatchQueue = new();
    private readonly ConcurrentQueue<LodestoneRefreshRequest> lodestoneRefreshQueue = new();
    private readonly ConcurrentDictionary<int, LodestoneLookup> lodestoneRefreshLookups = new();
    private readonly List<long> lodestoneRefreshHistory = new();
    private readonly object locker = new();
    private HttpClient httpClient = null!;
    private Timer processTimer = null!;
    private bool isProcessTimerStarted;
    private bool isProcessing;
    private long serviceCallAvailableAt;
    private int serviceSequentialFailureCount;

    public LodestoneService()
    {
        this.SetupHttpClient();
        this.SetupTimer();
        this.LoadRequests();
    }

    public LodestoneServiceStatus GetServiceStatus()
    {
        var config = ServiceContext.ConfigService.GetConfig();
        if (!config.LodestoneEnableLookup || !this.isProcessTimerStarted)
        {
            return LodestoneServiceStatus.ServiceDisabled;
        }

        return UnixTimestampHelper.CurrentTime() > this.serviceCallAvailableAt - 60000 
            ? LodestoneServiceStatus.ServiceAvailable 
            : LodestoneServiceStatus.ServiceUnavailable;
    }
    
    public void Start(bool shouldStart)
    {
        if (shouldStart)
        {
            this.processTimer.Start();
            this.isProcessTimerStarted = true;
        }
    }

    public void Stop()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.Stop()");
        this.processTimer.Elapsed -= this.ProcessRequests;
        this.processTimer.Stop();
        this.isProcessTimerStarted = false;
    }

    public void Dispose()
    {
        try
        {
            DalamudContext.PluginLog.Verbose("Entering LodestoneService.Dispose()");
            this.lodestoneBatchQueue.Clear();
            this.lodestoneRefreshQueue.Clear();
            this.httpClient.Dispose();
            GC.SuppressFinalize(this);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to dispose lodestone service.");
        }
    }

    public static long GetMillisecondsUntilRetry(long updatedTime)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
        var retryTime = updatedTime + retryInterval;
    
        if (currentTime >= retryTime)
        {
            return 0;
        }
    
        return retryTime - currentTime;
    }

    private void SetupHttpClient()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.SetupHttpClient()");
        var httpClientHandler = new HttpClientHandler();
        this.httpClient = new HttpClient(httpClientHandler, true)
        {
            Timeout = TimeSpan.FromMilliseconds(60000),
        };
        
        var pluginVersion = ServiceContext.ConfigService.GetConfig().PluginVersion.ToString();
        this.httpClient.DefaultRequestHeaders.Add("Application-Name", "PlayerTrack");
        this.httpClient.DefaultRequestHeaders.Add("Application-Version", pluginVersion);
        this.httpClient.DefaultRequestHeaders.Add("User-Agent", $"PlayerTrack/{pluginVersion} (Dalamud)");
    }

    private void SetupTimer()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.SetupTimer()");
        this.processTimer = new Timer { Interval = processInterval, Enabled = true };
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

            if (UnixTimestampHelper.CurrentTime() < this.serviceCallAvailableAt)
            {
                DalamudContext.PluginLog.Verbose("Lodestone lookup not available, skipping.");
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

            this.SendRefreshRequests();
            this.SendBatchRequest();

            this.serviceCallAvailableAt = UnixTimestampHelper.CurrentTime() + 10000;
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

    private void HandleRequestFailure(HttpResponseMessage? response, bool forceCooldown)
    {
        this.serviceSequentialFailureCount++;
        if (forceCooldown || this.serviceSequentialFailureCount > sequentialFailureLimit)
        {
            this.serviceCallAvailableAt = UnixTimestampHelper.CurrentTime() + serviceCooldownMs;
            DalamudContext.PluginLog.Warning($"Lodestone service is on cooldown until {this.serviceCallAvailableAt}");
        }

        throw new HttpRequestException($"Failed response from lodestone request: {response?.StatusCode}");
    }

    private void SendRefreshRequests()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneService.SendRefreshRequests()");
        var requests = this.DequeueRefreshRequests();
        if (requests.Count == 0)
        {
            DalamudContext.PluginLog.Verbose("No refresh requests to process, skipping.");
            return;
        }
        foreach (var request in requests)
        {
            DalamudContext.PluginLog.Verbose($"Sending refresh request for player {request.LodestoneId}");
            var response = this.GetRefreshedCharacterAsync(request.LodestoneId).Result;
            var responsePayload = response.Content.ReadAsStringAsync().Result;
            DalamudContext.PluginLog.Debug($"Lodestone refresh response payload: {responsePayload}");
            if (response.StatusCode != HttpStatusCode.OK)
            {
                DalamudContext.PluginLog.Error($"Failed to get lodestone refresh response: {response.StatusCode}");
                var forceCoolDown = response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.TooManyRequests;
                HandleRequestFailure(response, forceCoolDown);
            }

            this.serviceSequentialFailureCount = 0;

            var lodestoneResponse = JsonConvert.DeserializeObject<LodestoneResponse>(responsePayload);
            if (lodestoneResponse == null)
            {
                throw new HttpRequestException("Failed to deserialize lodestone refresh response.");
            }

            this.lodestoneRefreshLookups.TryGetValue(request.PlayerId, out var lookup);
            if (lookup == null)
            {
                throw new HttpRequestException($"Failed to find lodestone refresh lookup for player id {request.PlayerId}.");
            }
            
            HandleLodestoneResponse(lookup, lodestoneResponse);
            this.lodestoneRefreshLookups.TryRemove(request.PlayerId, out _);
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
            PlayerLodestoneService.UpdatePlayerFromLodestone(lookup);
        }
    }

    private static void HandleLodestoneResponse(LodestoneLookup lookup, LodestoneResponse lodestoneResponse)
    {
        switch (lodestoneResponse)
        {
            case { StatusCode: 410 }:
                lookup.UpdateLookupAsUnavailable();
                break;
            case { StatusCode: 400 }:
                lookup.UpdateLookupAsInvalid();
                break;
            default:
            {
                if (lodestoneResponse is not { StatusCode: 200 })
                {
                    lookup.UpdateLookupAsFailed(lodestoneResponse.StatusCode != 400);
                }
                else
                {
                    lookup.UpdateLookupAsSuccess(lodestoneResponse, LodestoneStatus.Verified);
                }

                break;
            }
        }
    }
    
    private void SendBatchRequest()
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.SendBatchRequest()");
        var batchedRequests = this.DequeueBatchRequests();
        if (batchedRequests.Count == 0)
        {
            DalamudContext.PluginLog.Verbose("No batch requests to process, skipping.");
            return;
        }
        var response = this.GetBatchCharacterIdsAsync(batchedRequests).Result;
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            DalamudContext.PluginLog.Error($"Failed to get lodestone response: {response.StatusCode}");
            var forceCoolDown = response.StatusCode is HttpStatusCode.BadGateway or HttpStatusCode.TooManyRequests;
            HandleRequestFailure(response, forceCoolDown);
        }

        this.serviceSequentialFailureCount = 0;

        var responsePayload = response.Content.ReadAsStringAsync().Result;
        DalamudContext.PluginLog.Debug($"Lodestone batch response payload: {responsePayload}");
        var responseObjects = JsonConvert.DeserializeObject<LodestoneResponse[]>(responsePayload);
        if (responseObjects == null)
        {
            throw new HttpRequestException("Failed to deserialize lodestone response.");
        }

        foreach (var request in batchedRequests)
        {
            var lodestoneResponse = responseObjects.FirstOrDefault(resp =>
                resp.PlayerName.Equals(request.PlayerName, StringComparison.OrdinalIgnoreCase) &&
                resp.WorldId == request.WorldId);
            if (lodestoneResponse == null)
            {
                throw new HttpRequestException("Failed to deserialize lodestone batch response.");
            }
            this.lodestoneBatchLookups.TryGetValue(request.PlayerId, out var lookup);
            if (lookup == null)
            {
                throw new HttpRequestException($"Failed to find lodestone lookup for player id {request.PlayerId}.");
            }
            
            HandleLodestoneResponse(lookup, lodestoneResponse);
            this.lodestoneBatchLookups.TryRemove(request.PlayerId, out _);
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
            PlayerLodestoneService.UpdatePlayerFromLodestone(lookup);
        }
    }

    private async Task<HttpResponseMessage> GetBatchCharacterIdsAsync(List<LodestoneBatchRequest> requests)
    {
        var content = JsonConvert.SerializeObject(requests);
        var stringContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
        var url = new Uri($"{api_uri}/players");
        return await this.httpClient.PostAsync(url, stringContent);
    }
    
    private async Task<HttpResponseMessage> GetRefreshedCharacterAsync(uint lodestoneId)
    {
        var url = new Uri($"{api_uri}/player/{lodestoneId}");
        return await this.httpClient.GetAsync(url);
    }

    private bool LoadRequests()
    {
        try
        {
            if (!this.lodestoneBatchQueue.IsEmpty || !this.lodestoneRefreshQueue.IsEmpty)
            {
                return true;
            }

            this.LoadPendingRequests();
            this.LoadFailedRequests();
            return !this.lodestoneBatchQueue.IsEmpty || !this.lodestoneRefreshQueue.IsEmpty;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private List<LodestoneBatchRequest> DequeueBatchRequests()
    {
        var lodestoneRequests = new List<LodestoneBatchRequest>();
        while (!this.lodestoneBatchQueue.IsEmpty && lodestoneRequests.Count < batch_size)
        {
            var isSuccessful = this.lodestoneBatchQueue.TryDequeue(out var lodestoneRequest);
            if (!isSuccessful || lodestoneRequest == null)
            {
                continue;
            }

            lodestoneRequests.Add(lodestoneRequest);
        }

        return lodestoneRequests;
    }

    private List<LodestoneRefreshRequest> DequeueRefreshRequests()
    {
        var lodestoneRequests = new List<LodestoneRefreshRequest>();
        var currentTime = UnixTimestampHelper.CurrentTime();
        lodestoneRefreshHistory.RemoveAll(timestamp => currentTime - timestamp > 3600000);
        while (!this.lodestoneRefreshQueue.IsEmpty && lodestoneRequests.Count < hourly_refresh_limit)
        {
            var isSuccessful = this.lodestoneRefreshQueue.TryDequeue(out var lodestoneRequest);
            if (!isSuccessful || lodestoneRequest == null)
            {
                continue;
            }

            lodestoneRequests.Add(lodestoneRequest);
            lodestoneRefreshHistory.Add(currentTime);
            lodestoneRefreshHistory.RemoveAll(timestamp => currentTime - timestamp > 3600000);
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
            AddRequest(lookup);
        }
    }

    private void LoadFailedRequests()
    {
        var failedLookups =
            RepositoryContext.LodestoneRepository.GetRequestsByStatus(LodestoneStatus.Failed) ??
            new List<LodestoneLookup>();
        foreach (var lookup in failedLookups)
        {
            if (GetMillisecondsUntilRetry(lookup.Updated) > 0) continue;
            AddRequest(lookup);
        }
    }

    private void AddRequest(LodestoneLookup lookup)
    {
        if (string.IsNullOrEmpty(lookup.PlayerName) || lookup.WorldId == 0)
        {
            DalamudContext.PluginLog.Warning($"Invalid lodestone lookup for player {lookup.PlayerName}@{lookup.WorldId}");
            lookup.UpdateLookupAsInvalid();
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
            PlayerLodestoneService.UpdatePlayerFromLodestone(lookup);
        }
        else if (lookup.LodestoneLookupType == LodestoneLookupType.Batch)
        {
            var request = new LodestoneBatchRequest(lookup.PlayerId, lookup.PlayerName, lookup.WorldId);
            var isAdded = this.lodestoneBatchLookups.TryAdd(lookup.PlayerId, lookup);
            if (isAdded) this.lodestoneBatchQueue.Enqueue(request);
        }
        else
        {
            var request = new LodestoneRefreshRequest(lookup.PlayerId, lookup.LodestoneId);
            var isAdded = this.lodestoneRefreshLookups.TryAdd(lookup.PlayerId, lookup);
            if (isAdded) this.lodestoneRefreshQueue.Enqueue(request);
        }
    }
}
