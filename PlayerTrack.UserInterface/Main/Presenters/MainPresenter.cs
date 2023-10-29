using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Views;
using PlayerTrack.UserInterface.ViewModels;
using PlayerTrack.UserInterface.ViewModels.Mappers;

namespace PlayerTrack.UserInterface.Main.Presenters;

using System.Collections;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;

public class MainPresenter : IMainPresenter
{
    private const int CacheChunkSize = 60;
    private const long CacheTtl = 10000;
    private readonly PluginConfig config;
    private readonly Dictionary<int, List<Player>> playerCache = new();
    private readonly Dictionary<string, int> playerCountCache = new();
    private PlayerView? selectedPlayer;
    private bool isPlayerLoading;
    private PlayerComponent playerComponent = null!;
    private AddPlayerComponent addPlayerComponent = null!;
    private bool isPlayerCacheStale;
    private bool isPlayerCountCacheStale;
    private long playerCacheLastUpdated;
    private long playerCountCacheLastUpdated;
    private string lastSearchInput = string.Empty;

    public MainPresenter()
    {
        this.config = ServiceContext.ConfigService.GetConfig();
        this.BuildComponents();
        this.BuildViews();
    }

    public Action<ConfigMenuOption>? OnConfigMenuOptionSelected { get; set; }

    public Combined Combined { get; set; } = null!;

    public PanelView PanelView { get; set; } = null!;

    public PlayerList PlayerList { get; set; } = null!;

    public PlayerView? GetSelectedPlayer() => this.selectedPlayer;

    public void ClosePlayer()
    {
        this.selectedPlayer = null;
        this.isPlayerLoading = false;
        this.config.PanelType = PanelType.None;
    }

    public bool IsPlayerLoading() => this.isPlayerLoading;

    public int GetPlayersCount()
    {
        InvalidateCacheIfStale(this.playerCountCache, ref this.playerCountCacheLastUpdated, ref this.isPlayerCountCacheStale);
        this.InvalidateCacheIfSearchChanged();

        var filter = this.config.PlayerListFilter;
        var cacheKey = string.IsNullOrEmpty(this.config.SearchInput) ?
            filter.ToString() :
            $"{filter}_{this.config.SearchInput}_{this.config.SearchType}";

        if (this.playerCountCache.TryGetValue(cacheKey, out var cachedCount))
        {
            return cachedCount;
        }

        int count;
        if (string.IsNullOrEmpty(this.config.SearchInput))
        {
            count = this.config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayersCount(),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayersCount(),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayersCount(),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayersCount(this.config.FilterCategoryId),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayersCount(this.config.FilterTagId),
                _ => 0,
            };
        }
        else
        {
            count = this.config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayersCount(this.config.SearchInput, this.config.SearchType),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayersCount(this.config.SearchInput, this.config.SearchType),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayersCount(this.config.SearchInput, this.config.SearchType),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayersCount(this.config.FilterCategoryId, this.config.SearchInput, this.config.SearchType),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayersCount(this.config.FilterTagId, this.config.SearchInput, this.config.SearchType),
                _ => 0,
            };
        }

        this.playerCountCache[cacheKey] = count;
        return count;
    }

    public List<Player> GetPlayersList(int start, int count)
    {
        if (string.IsNullOrEmpty(this.config.SearchInput))
        {
            return this.config.PlayerListFilter switch
            {
                PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayers(start, count),
                PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayers(start, count),
                PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayers(start, count),
                PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayers(this.config.FilterCategoryId, start, count),
                PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayers(this.config.FilterTagId, start, count),
                _ => new List<Player>(),
            };
        }

        return this.config.PlayerListFilter switch
        {
            PlayerListFilter.AllPlayers => ServiceContext.PlayerCacheService.GetAllPlayers(start, count, this.config.SearchInput, this.config.SearchType),
            PlayerListFilter.CurrentPlayers => ServiceContext.PlayerCacheService.GetCurrentPlayers(start, count, this.config.SearchInput, this.config.SearchType),
            PlayerListFilter.RecentPlayers => ServiceContext.PlayerCacheService.GetRecentPlayers(start, count, this.config.SearchInput, this.config.SearchType),
            PlayerListFilter.PlayersByCategory => ServiceContext.PlayerCacheService.GetCategoryPlayers(this.config.FilterCategoryId, start, count, this.config.SearchInput, this.config.SearchType),
            PlayerListFilter.PlayersByTag => ServiceContext.PlayerCacheService.GetTagPlayers(this.config.FilterTagId, start, count, this.config.SearchInput, this.config.SearchType),
            _ => new List<Player>(),
        };
    }

    public List<Player> GetPlayers(int displayStart, int displayEnd)
    {
        InvalidateCacheIfStale(this.playerCache, ref this.playerCacheLastUpdated, ref this.isPlayerCacheStale);
        this.InvalidateCacheIfSearchChanged();

        var currentTime = UnixTimestampHelper.CurrentTime();

        if (this.isPlayerCacheStale && currentTime - this.playerCacheLastUpdated > CacheTtl)
        {
            this.playerCache.Clear();
            this.isPlayerCacheStale = false;
            this.playerCacheLastUpdated = currentTime;
        }

        var playersCount = displayEnd - displayStart;
        var players = new List<Player>(playersCount);

        var startChunkIndex = displayStart / CacheChunkSize;
        var endChunkIndex = (displayEnd - 1) / CacheChunkSize;

        for (var chunkIndex = startChunkIndex; chunkIndex <= endChunkIndex; chunkIndex++)
        {
            if (!this.playerCache.TryGetValue(chunkIndex, out var cachedChunk))
            {
                var chunkStart = chunkIndex * CacheChunkSize;
                var chunkEnd = Math.Min(chunkStart + CacheChunkSize, this.GetPlayersCount());
                cachedChunk = this.GetPlayersList(chunkStart, chunkEnd);
                this.playerCache[chunkIndex] = cachedChunk;
            }

            var relativeStart = Math.Max(displayStart - chunkIndex * CacheChunkSize, 0);
            var relativeEnd = Math.Min(displayEnd - chunkIndex * CacheChunkSize, cachedChunk.Count);

            for (var i = relativeStart; i < relativeEnd; i++)
            {
                players.Add(cachedChunk[i]);
            }
        }

        return players;
    }

    public void TogglePanel(PanelType panelType) => this.CurrentPanelView().TogglePanel(panelType);

    public void HidePanel() => this.CurrentPanelView().HidePanel();

    public void ShowPanel(PanelType panelType) => this.CurrentPanelView().ShowPanel(panelType);

    public void ClearCache()
    {
        this.isPlayerCountCacheStale = true;
        this.isPlayerCacheStale = true;
        this.playerCache.Clear();
        this.playerCountCache.Clear();
    }

    public void SelectPlayer(Player player)
    {
        this.isPlayerLoading = true;
        Task.Run(() => this.LoadPlayer(player));
    }

    public void OpenConfig(ConfigMenuOption configMenuOption) => this.OnConfigMenuOptionSelected?.Invoke(configMenuOption);

    public void ReloadPlayer()
    {
        DalamudContext.PluginLog.Verbose("Entering MainPresenter.ReloadPlayer()");
        this.ClearCache();
        if (this.selectedPlayer == null)
        {
            return;
        }

        var player = ServiceContext.PlayerDataService.GetPlayer(this.selectedPlayer.Id);
        if (player == null)
        {
            return;
        }

        this.LoadPlayer(player);
    }

    private static void InvalidateCacheIfStale(IDictionary cache, ref long lastUpdated, ref bool isStale)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
        if (isStale && currentTime - lastUpdated > CacheTtl)
        {
            cache.Clear();
            lastUpdated = currentTime;
            isStale = false;
        }
    }

    private void InvalidateCacheIfSearchChanged()
    {
        if (this.lastSearchInput != this.config.SearchInput)
        {
            this.ClearCache();
            this.lastSearchInput = this.config.SearchInput;
        }
    }

    private IViewWithPanel CurrentPanelView() => this.config.IsWindowCombined ? this.Combined : this.PlayerList;

    private void LoadPlayer(Player player)
    {
        this.selectedPlayer = PlayerViewMapper.MapPlayer(player);
        this.isPlayerLoading = false;
    }

    private void BuildComponents()
    {
        this.playerComponent = new PlayerComponent(this);
        this.addPlayerComponent = new AddPlayerComponent();
    }

    private void BuildViews()
    {
        this.Combined = new Combined($"PlayerTrack##Combined", this.config, this.playerComponent, this.addPlayerComponent, this)
        {
            IsOpen = this.config is { IsWindowCombined: true, PreserveMainWindowState: true },
        };
        this.PanelView = new PanelView($"PlayerTrack##PanelView", this.config, this.playerComponent, this.addPlayerComponent, this)
        {
            IsOpen = this.config is { IsWindowCombined: false, PreserveMainWindowState: true },
        };
        this.PlayerList = new PlayerList($"PlayerTrack##PlayerList", this.config, this)
        {
            IsOpen = this.config is { IsWindowCombined: false, PreserveMainWindowState: true },
        };
        this.PlayerList.OpenPanelView += () => this.PanelView.IsOpen = true;
    }
}
