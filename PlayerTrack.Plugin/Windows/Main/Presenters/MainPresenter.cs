using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.Windows.Main.Components;
using PlayerTrack.Windows.Main.Views;
using PlayerTrack.Windows.ViewModels;
using PlayerTrack.Windows.ViewModels.Mappers;

namespace PlayerTrack.Windows.Main.Presenters;

public class MainPresenter : IMainPresenter
{
    private const int CacheChunkSize = 100;
    private const long CacheTtl = 600000;
    private const int OneSecond = 1000;

    private readonly PluginConfig Config;
    private readonly Dictionary<int, List<Player>> PlayerCache = new();
    private readonly Dictionary<string, int> PlayerCountCache = new();
    private PlayerView? SelectedPlayer;
    private bool IsPlayerStillLoading;
    private PlayerComponent PlayerComponent = null!;
    private AddPlayerComponent AddPlayerComponent = null!;
    private bool IsPlayerCacheStale;
    private bool IsPlayerCountCacheStale;
    private long PlayerCacheLastUpdated;
    private long PlayerCountCacheLastUpdated;
    private string LastSearchInput = string.Empty;

    private long LastCacheClear = Environment.TickCount64;
    private long LastDelayedCacheClear = Environment.TickCount64;
    private bool PlayerCacheDelayedClear;

    public MainPresenter()
    {
        Config = ServiceContext.ConfigService.GetConfig();
        BuildComponents();
        BuildViews();
    }

    public Action<ConfigMenuOption>? OnConfigMenuOptionSelected { get; set; }

    public Combined Combined { get; set; } = null!;

    public PanelView PanelView { get; set; } = null!;

    public PlayerList PlayerList { get; set; } = null!;

    public PlayerView? GetSelectedPlayer() => SelectedPlayer;

    public void ClosePlayer()
    {
        SelectedPlayer = null;
        IsPlayerStillLoading = false;
        Config.PanelType = PanelType.None;
    }

    public bool IsPlayerLoading() => IsPlayerStillLoading;

    public int GetPlayersCount()
    {
        CheckDelayedCacheClear();
        InvalidateCacheIfStale(PlayerCountCache, ref PlayerCountCacheLastUpdated, ref IsPlayerCountCacheStale);
        InvalidateCacheIfSearchChanged();

        var filter = Config.PlayerListFilter;
        var cacheKey = string.IsNullOrEmpty(Config.SearchInput) ?
            filter.ToString() :
            $"{filter}_{Config.SearchInput}_{Config.SearchType}";

        if (PlayerCountCache.TryGetValue(cacheKey, out var cachedCount))
            return cachedCount;

        int count;
        if (string.IsNullOrEmpty(Config.SearchInput))
        {
            count = Config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayersCount(),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayersCount(),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayersCount(),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayersCount(Config.FilterCategoryId),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayersCount(Config.FilterTagId),
                _ => 0,
            };
        }
        else
        {
            count = Config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayersCount(Config.SearchInput, Config.SearchType),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayersCount(Config.SearchInput, Config.SearchType),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayersCount(Config.SearchInput, Config.SearchType),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayersCount(Config.FilterCategoryId, Config.SearchInput, Config.SearchType),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayersCount(Config.FilterTagId, Config.SearchInput, Config.SearchType),
                _ => 0,
            };
        }

        PlayerCountCache[cacheKey] = count;
        return count;
    }

    public List<Player> GetPlayersList(int start, int count)
    {
        if (string.IsNullOrEmpty(Config.SearchInput))
        {
            return Config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayers(start, count),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayers(start, count),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayers(start, count),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayers(Config.FilterCategoryId, start, count),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayers(Config.FilterTagId, start, count),
                _ => [],
            };
        }

        return Config.PlayerListFilter switch
        {
            PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayers(start, count, Config.SearchInput, Config.SearchType),
            PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayers(start, count, Config.SearchInput, Config.SearchType),
            PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayers(start, count, Config.SearchInput, Config.SearchType),
            PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayers(Config.FilterCategoryId, start, count, Config.SearchInput, Config.SearchType),
            PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayers(Config.FilterTagId, start, count, Config.SearchInput, Config.SearchType),
            _ => [],
        };
    }

    public List<Player> GetPlayers(int displayStart, int displayEnd)
    {
        if (!PlayerSearchService.IsValidSearch(Config.SearchInput))
            return []; // Return empty if the search input is invalid

        InvalidateCacheIfStale(PlayerCache, ref PlayerCacheLastUpdated, ref IsPlayerCacheStale);
        InvalidateCacheIfSearchChanged();

        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (IsPlayerCacheStale && currentTime - PlayerCacheLastUpdated > CacheTtl)
        {
            PlayerCache.Clear();
            IsPlayerCacheStale = false;
            PlayerCacheLastUpdated = currentTime;
        }

        var playersCount = displayEnd - displayStart;
        var players = new List<Player>(playersCount);

        var startChunkIndex = displayStart / CacheChunkSize;
        var endChunkIndex = (displayEnd - 1) / CacheChunkSize;

        for (var chunkIndex = startChunkIndex; chunkIndex <= endChunkIndex; chunkIndex++)
        {
            if (!PlayerCache.TryGetValue(chunkIndex, out var cachedChunk))
            {
                var chunkStart = chunkIndex * CacheChunkSize;
                var chunkEnd = Math.Min(chunkStart + CacheChunkSize, GetPlayersCount());
                cachedChunk = GetPlayersList(chunkStart, chunkEnd);
                PlayerCache[chunkIndex] = cachedChunk;
            }

            var relativeStart = Math.Max(displayStart - chunkIndex * CacheChunkSize, 0);
            var relativeEnd = Math.Min(displayEnd - chunkIndex * CacheChunkSize, cachedChunk.Count);

            for (var i = relativeStart; i < relativeEnd; i++)
                players.Add(cachedChunk[i]);
        }

        return players;
    }

    public void TogglePanel(PanelType panelType) => CurrentPanelView().TogglePanel(panelType);

    public void HidePanel() => CurrentPanelView().HidePanel();

    public void ShowPanel(PanelType panelType) => CurrentPanelView().ShowPanel(panelType);

    /// <summary>
    /// Clears the cache and triggers a rebuild.
    /// </summary>
    /// <remarks>
    /// Only allowed once every 1 second.
    /// If the cooldown is active, any new clear request will be
    /// pushed into a delayed pool which gets processed after 1 second
    /// </remarks>
    public void ClearCache()
    {
        if (Environment.TickCount64 < LastCacheClear)
        {
            PlayerCacheDelayedClear = true;
            return;
        }

        LastCacheClear = Environment.TickCount64 + OneSecond;
        LastDelayedCacheClear = Environment.TickCount64 + OneSecond;

        IsPlayerCountCacheStale = true;
        IsPlayerCacheStale = true;
        PlayerCache.Clear();
        PlayerCountCache.Clear();
    }

    public void SelectPlayer(Player player)
    {
        IsPlayerStillLoading = true;
        Task.Run(() => LoadPlayer(player));
    }

    public void OpenConfig(ConfigMenuOption configMenuOption) => OnConfigMenuOptionSelected?.Invoke(configMenuOption);

    public void ReloadPlayer()
    {
        Plugin.PluginLog.Verbose("Entering MainPresenter.ReloadPlayer()");
        ClearCache();
        if (SelectedPlayer == null)
            return;

        var player = ServiceContext.PlayerDataService.GetPlayer(SelectedPlayer.Id);
        if (player == null)
            return;

        LoadPlayer(player);
    }

    private static void InvalidateCacheIfStale(IDictionary cache, ref long lastUpdated, ref bool isStale)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (isStale && currentTime - lastUpdated > CacheTtl)
        {
            cache.Clear();
            lastUpdated = currentTime;
            isStale = false;
        }
    }

    private void InvalidateCacheIfSearchChanged()
    {
        if (LastSearchInput != Config.SearchInput)
        {
            ClearCache();
            LastSearchInput = Config.SearchInput;
        }
    }

    private IViewWithPanel CurrentPanelView() => Config.IsWindowCombined ? Combined : PlayerList;

    private void LoadPlayer(Player player)
    {
        SelectedPlayer = PlayerViewMapper.MapPlayer(player);
        IsPlayerStillLoading = false;
    }

    private void BuildComponents()
    {
        PlayerComponent = new PlayerComponent(this);
        AddPlayerComponent = new AddPlayerComponent();
    }

    private void BuildViews()
    {
        Combined = new Combined("PlayerTrack##Combined", Config, PlayerComponent, AddPlayerComponent, this)
        {
            IsOpen = Config is { IsWindowCombined: true, PreserveMainWindowState: true },
        };
        PanelView = new PanelView("PlayerTrack##PanelView", Config, PlayerComponent, AddPlayerComponent, this)
        {
            IsOpen = Config is { IsWindowCombined: false, PreserveMainWindowState: true },
        };
        PlayerList = new PlayerList("PlayerTrack##PlayerList", Config, this)
        {
            IsOpen = Config is { IsWindowCombined: false, PreserveMainWindowState: true },
        };

        PlayerList.OnOpenPanelView += () => PanelView.IsOpen = true;
    }

    /// <summary>
    /// Clears the cache after 1 second of delay if there was multiple clear requests before
    /// </summary>
    private void CheckDelayedCacheClear()
    {
        if (!PlayerCacheDelayedClear)
            return;

        if (Environment.TickCount64 < LastDelayedCacheClear)
            return;

        PlayerCacheDelayedClear = false;

        IsPlayerCountCacheStale = true;
        IsPlayerCacheStale = true;
        PlayerCache.Clear();
        PlayerCountCache.Clear();
    }
}
