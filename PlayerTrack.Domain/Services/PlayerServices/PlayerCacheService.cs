using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Interface;
using PlayerTrack.Domain.Caches;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using PlayerTrack.Models.Comparers;
using Timer = System.Timers.Timer;

namespace PlayerTrack.Domain;

public class PlayerCacheService
{
    private readonly ReaderWriterLockSlim setLock = new(LockRecursionPolicy.NoRecursion);
    private readonly PlayerCache playerCache = new();
    private readonly PlayerCurrentCache playerCurrentCache = new();
    private readonly PlayerRecentCache playerRecentCache = new();
    private readonly PlayerCategoryCache playerCategoryCache = new();
    private readonly PlayerTagCache playerTagCache = new();
    private readonly Timer recentPlayerTimer;
    private IComparer<Player> comparer = null!;
    private List<Player> dbPlayers = null!;
    private Dictionary<int, int> dbCategoryRanks = null!;
    private const long NinetyDaysInMilliseconds = 7776000000;

    public event Action? CacheUpdated;

    public PlayerCacheService()
    {
        this.recentPlayerTimer = new Timer(30000);
        this.recentPlayerTimer.Elapsed += this.OnRecentPlayerTimerOnElapsed;
        this.recentPlayerTimer.AutoReset = true;
        this.recentPlayerTimer.Start();
    }

    public void Dispose()
    {
        this.recentPlayerTimer.Elapsed -= this.OnRecentPlayerTimerOnElapsed;
        this.recentPlayerTimer.Stop();
    }
    
    private void OnRecentPlayerTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
        var threshold = ServiceContext.ConfigService.GetConfig().RecentPlayersThreshold;
        var expiry = this.playerRecentCache.GetExpiry();

        foreach (var entry in expiry)
        {
            var expiryTime = entry.Value + threshold;
            if (expiryTime > currentTime) continue;
            if (!this.playerRecentCache.RemoveExpiry(entry.Key)) continue;
            var player = this.playerCache.Get(entry.Key);
            if (player == null) continue;
            player.IsRecent = false;
            ServiceContext.PlayerDataService.UpdatePlayer(player);
        }
    }

    public void LoadPlayers()
    {
        this.setLock.EnterWriteLock();
        try
        {
            Initialize();
            CopyUnsavedProperties();
            FetchDataFromDatabase();
            PopulateCaches();
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }
    
    public void AddPlayer(Player playerToAdd)
    {
        this.setLock.EnterWriteLock();
        try
        {
            AddPlayerToCaches(playerToAdd);
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }

    public void RemovePlayer(Player playerToRemove)
    {
        this.setLock.EnterWriteLock();
        try
        {
            RemovePlayerFromAllCaches(playerToRemove);
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }
    
    public void RemovePlayer(int playerIdToRemove)
    {
        this.setLock.EnterWriteLock();
        try
        {
            var player = this.playerCache.Get(playerIdToRemove);
            if (player != null)
            {
                RemovePlayerFromAllCaches(player);
            }
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }

    public void UpdatePlayer(Player playerToUpdate)
    {
        this.setLock.EnterWriteLock();
        try
        {
            var existingPlayer = this.playerCache.Get(playerToUpdate.Id);
            if (existingPlayer != null)
            {
                RemovePlayerFromAllCaches(existingPlayer);
                PopulateDerivedFields(playerToUpdate);
                AddPlayerToCaches(playerToUpdate);
            }
            else
            {
                AddPlayerToCaches(playerToUpdate);
            }
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }
    
    public Player? GetPlayer(int playerId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.Get(playerId);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public Player? GetPlayer(string name, uint worldId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.FindFirst(p => p.Name == name && p.WorldId == worldId);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public Player? GetPlayer(uint objectId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.FindFirst(p => p.ObjectId == objectId);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public Player? GetPlayer(string key)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.FindFirst(p => p.Key == key);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public List<Player> GetPlayers()
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.GetAll();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetPlayers(Func<Player, bool> filter)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.GetAll().Where(filter).ToList();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public void AddCategory(int categoryIdToAdd)
    {
        this.setLock.EnterWriteLock();
        try
        {
            this.playerCategoryCache.AddGroup(categoryIdToAdd);
        }
        finally
        {
            this.setLock.ExitWriteLock();
        }
    }

    public void AddTag(int tagIdToAdd)
    {
        this.setLock.EnterWriteLock();
        try
        {
            this.playerTagCache.AddGroup(tagIdToAdd);
        }
        finally
        {
            this.setLock.ExitWriteLock();
        }
    }
        
    public void RemoveCategory(int categoryIdToRemove)
    {
        var updatedPlayers = new List<Player>();
        this.setLock.EnterWriteLock();
        try
        {
            var categoryCache = this.playerCategoryCache.GetGroup(categoryIdToRemove);
            if (categoryCache != null)
            {
                updatedPlayers.AddRange(categoryCache.Values);
                foreach (var player in updatedPlayers)
                {
                    player.AssignedCategories.RemoveAll(c => c.Id == categoryIdToRemove);
                    PopulateDerivedFields(player);
                }
                
                this.playerCategoryCache.RemoveGroup(categoryIdToRemove);

                foreach (var player in updatedPlayers)
                {
                    RemovePlayerFromAllCaches(player);
                    AddPlayerToCaches(player);
                }

                InitializeComparer();
                ResortCache();
            }
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }

    public void Resort()
    {
        this.setLock.EnterWriteLock();
        try
        {
            InitializeComparer();
            ResortCache();
        }
        finally
        {
            this.setLock.ExitWriteLock();
            this.CacheUpdated?.Invoke();
        }
    }
    
    public List<Player> GetAllPlayers(int start, int count)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.Get(start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetAllPlayers(int start, int count, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.Get(GetSearchFilter(name, searchType), start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetAllPlayersCount()
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.Count();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetAllPlayersCount(string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCache.Count(GetSearchFilter(name, searchType));
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetCurrentPlayers()
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCurrentCache.GetAll();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public List<Player> GetCurrentPlayers(int start, int count)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCurrentCache.Get(start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetCurrentPlayers(int start, int count, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCurrentCache.Get(GetSearchFilter(name, searchType), start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetCurrentPlayersCount()
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCurrentCache.Count();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetCurrentPlayersCount(string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCurrentCache.Count(GetSearchFilter(name, searchType));
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetRecentPlayers(int start, int count)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerRecentCache.Get(start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetRecentPlayers(int start, int count, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerRecentCache.Get(GetSearchFilter(name, searchType), start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetRecentPlayersCount()
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerRecentCache.Count();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetRecentPlayersCount(string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerRecentCache.Count(GetSearchFilter(name, searchType));
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public List<Player> GetCategoryPlayers(int categoryId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCategoryCache.GetGroup(categoryId)?.Values.ToList() ?? new List<Player>();
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public List<Player> GetCategoryPlayers(int categoryId, int start, int count)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCategoryCache.Get(categoryId, start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetCategoryPlayers(int categoryId, int start, int count, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCategoryCache.Get(categoryId, GetSearchFilter(name, searchType), start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetCategoryPlayersCount(int categoryId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCategoryCache.Count(categoryId);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetCategoryPlayersCount(int categoryId, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerCategoryCache.Count(categoryId, GetSearchFilter(name, searchType));
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public List<Player> GetTagPlayers(int tagId, int start, int count)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerTagCache.Get(tagId, start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public List<Player> GetTagPlayers(int tagId, int start, int count, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerTagCache.Get(tagId, GetSearchFilter(name, searchType), start, count);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetTagPlayersCount(int tagId)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerTagCache.Count(tagId);
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }

    public int GetTagPlayersCount(int tagId, string name, SearchType searchType)
    {
        this.setLock.EnterReadLock();
        try
        {
            return this.playerTagCache.Count(tagId, GetSearchFilter(name, searchType));
        }
        finally
        {
            this.setLock.ExitReadLock();
        }
    }
    
    public int GetPlayerConfigCount()
    {
        return this.playerCache.Get(p => p.PlayerConfig.Id != 0).Count;
    }

    public int GetPlayersForDeletionCount()
    {
        return this.GetPlayersForDeletion().Count;
    }

    public int GetPlayerConfigsForDeletionCount()
    {
        return this.GetPlayerConfigsForDeletion().Count;
    }

    public void PopulateDerivedFields(Player player)
    {
        PlayerCategoryService.SetPrimaryCategoryId(player, this.dbCategoryRanks);
        var colorId = PlayerConfigService.GetNameColor(player);
        var color = DalamudContext.DataManager.UIColors.TryGetValue(colorId, out var uiColor) ? uiColor : new ToadUIColor();
        player.PlayerListNameColor = color.Foreground.ToVector4();
        player.PlayerListIconString = ((FontAwesomeIcon)PlayerConfigService.GetIcon(player)).ToIconString();
    }
    
    private void ResortCache()
    {
        var filter = ServiceContext.ConfigService.GetConfig().PlayerListFilter;
        switch (filter)
        {
            case PlayerListFilter.CurrentPlayers:
                this.playerCurrentCache.Resort(this.comparer);
                break;
            case PlayerListFilter.RecentPlayers:
                this.playerRecentCache.Resort(this.comparer);
                break;
            case PlayerListFilter.AllPlayers:
                this.playerCache.Resort(this.comparer);
                break;
            case PlayerListFilter.PlayersByCategory:
                this.playerCategoryCache.Resort(this.comparer);
                break;
            case PlayerListFilter.PlayersByTag:
                this.playerTagCache.Resort(this.comparer);
                break;
            default:
                DalamudContext.PluginLog.Warning($"Invalid player list filter: {filter}");
                break;
        }
    }
     
    private void RemovePlayerFromAllCaches(Player playerToRemove)
    {
        this.playerCache.Remove(playerToRemove);
        this.playerCurrentCache.Remove(playerToRemove);
        this.playerRecentCache.Remove(playerToRemove);
        this.playerCategoryCache.Remove(playerToRemove);
        this.playerTagCache.Remove(playerToRemove);
    }

    private void PopulateCaches()
    {
        foreach (var player in this.dbPlayers)
        {
            PopulateDerivedFields(player);
            AddPlayerToCaches(player);
        }
    }
    
    private void CopyUnsavedProperties()
    {
        this.playerCurrentCache.SaveIds();
        this.playerRecentCache.SaveIds();
    }

    private void FetchDataFromDatabase()
    {
        this.dbPlayers = RepositoryContext.PlayerRepository.GetAllPlayersWithRelations().ToList();
        this.dbCategoryRanks = ServiceContext.CategoryService.GetCategoryRanks();
    }

    private void Initialize()
    {
        InitializeComparer();
        InitializeUnsavedProperties();
        this.playerCache.Initialize(this.comparer);
        this.playerCurrentCache.Initialize(this.comparer);
        this.playerRecentCache.Initialize(this.comparer);
        this.playerCategoryCache.Initialize(this.comparer);
        this.playerTagCache.Initialize(this.comparer);
    }

    private void InitializeUnsavedProperties()
    {
        this.playerCurrentCache.ClearIds();
        this.playerRecentCache.ClearIds();
    }
    
    private void InitializeComparer()
    {
        this.comparer = new PlayerComparer(ServiceContext.CategoryService.GetCategoryRanks());
    }
    
    private void AddPlayerToCaches(Player player)
    {
        playerCache.Add(player);
        playerCurrentCache.Add(player);
        playerRecentCache.Add(player);
        playerCategoryCache.Add(player);
        playerTagCache.Add(player);
    }
    
    private static Func<Player, bool> GetSearchFilter(string name, SearchType searchType)
    {
        return Filter;

        bool Filter(Player player)
        {
            return searchType switch
            {
                SearchType.Contains => player.Name.Contains(name, StringComparison.OrdinalIgnoreCase),
                SearchType.StartsWith => player.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase),
                SearchType.Exact => player.Name.Equals(name, StringComparison.OrdinalIgnoreCase),
                _ => throw new ArgumentException($"Invalid search type: {searchType}"),
            };
        }
    }
    
    private List<Player> GetPlayersForDeletion()
    {
        var playersWithEncounters = RepositoryContext.PlayerEncounterRepository.GetPlayersWithEncounters();
        var currentTimeUnix = UnixTimestampHelper.CurrentTime();
        var options = ServiceContext.ConfigService.GetConfig().PlayerDataActionOptions;
        return this.playerCache.Get(p =>
            (!options.KeepPlayersWithNotes || string.IsNullOrEmpty(p.Notes)) &&
            (!options.KeepPlayersWithCategories || !p.AssignedCategories.Any()) &&
            (!options.KeepPlayersWithAnySettings || p.PlayerConfig.Id == 0) &&
            (!options.KeepPlayersWithEncounters || !playersWithEncounters.Contains(p.Id)) &&
            (!options.KeepPlayersSeenInLast90Days || currentTimeUnix - p.LastSeen > NinetyDaysInMilliseconds) &&
            (!options.KeepPlayersVerifiedOnLodestone || p.LodestoneStatus != LodestoneStatus.Verified));
    }
    
    private List<PlayerConfig> GetPlayerConfigsForDeletion()
    {
        var playersWithEncounters = RepositoryContext.PlayerEncounterRepository.GetPlayersWithEncounters();
        var currentTimeUnix = UnixTimestampHelper.CurrentTime();
        var options = ServiceContext.ConfigService.GetConfig().PlayerSettingsDataActionOptions;
        return this.playerCache.Get(p =>
            p.PlayerConfig.Id != 0 &&
            (!options.KeepSettingsForPlayersWithNotes || string.IsNullOrEmpty(p.Notes)) &&
            (!options.KeepSettingsForPlayersWithCategories || !p.AssignedCategories.Any()) &&
            (!options.KeepSettingsForPlayersWithAnySettings || p.PlayerConfig.Id == 0) &&
            (!options.KeepSettingsForPlayersWithEncounters || !playersWithEncounters.Contains(p.Id)) &&
            (!options.KeepSettingsForPlayersSeenInLast90Days || currentTimeUnix - p.LastSeen > NinetyDaysInMilliseconds) &&
            (!options.KeepSettingsForPlayersVerifiedOnLodestone || p.LodestoneStatus != LodestoneStatus.Verified)).Select(p => p.PlayerConfig).ToList();
    }
}