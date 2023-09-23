using System;
using System.Collections.Generic;
using Dalamud.DrunkenToad.Core.Enums;
using Dalamud.DrunkenToad.Gui.Interfaces;
using Dalamud.Interface;
using Newtonsoft.Json;

namespace PlayerTrack.Models;

using Dalamud.DrunkenToad.Helpers;

public class PluginConfig : IPluginConfig
{
    public int FilterCategoryIndex { get; set; }

    public int FilterCategoryId { get; set; }

    public bool IsConfigOpen { get; set; } = true;

    public bool IsWindowCombined { get; set; } = true;

    public int PluginVersion { get; set; }

    public int LastVersionBackup { get; set; }

    public bool LodestoneEnableLookup { get; set; } = true;

    public LodestoneLocale LodestoneLocale { get; set; } = LodestoneLocale.NA;

    public float MainWindowHeight { get; set; } = 400f;

    public float MainWindowWidth { get; set; } = 700f;

    public PanelType PanelType { get; set; } = PanelType.None;

    [JsonIgnore] public PlayerConfig PlayerConfig { get; set; } = new(PlayerConfigType.Default);

    public PlayerListFilter PlayerListFilter { get; set; } = PlayerListFilter.AllPlayers;

    public SearchType SearchType { get; set; } = SearchType.Contains;

    public ConfigMenuOption SelectedConfigOption { get; set; } = ConfigMenuOption.Window;

    [JsonIgnore] public string SearchInput { get; set; } = string.Empty;

    public bool ShowOpenInPlayerTrack { get; set; } = true;

    public bool ShowOpenLodestone { get; set; } = true;

    public bool ShowPlayerFilter { get; set; } = true;

    public bool ShowSearchBox { get; set; } = true;

    public bool ShowCategorySeparator { get; set; } = true;

    public bool ShowContextMenuIndicator { get; set; } = true;

    public bool SyncWithVisibility { get; set; }

    public long MaintenanceLastRunOn { get; set; } = UnixTimestampHelper.CurrentTime();

    public List<FontAwesomeIcon> Icons { get; set; } = new()
    {
        FontAwesomeIcon.User,
        FontAwesomeIcon.GrinBeam,
        FontAwesomeIcon.GrinAlt,
        FontAwesomeIcon.Meh,
        FontAwesomeIcon.Frown,
        FontAwesomeIcon.Angry,
        FontAwesomeIcon.Flushed,
        FontAwesomeIcon.Surprise,
        FontAwesomeIcon.Tired,
    };

    public TrackingLocationConfig Overworld { get; set; } = new()
    {
        AddPlayers = true,
        AddEncounters = false,
    };

    public TrackingLocationConfig Content { get; set; } = new()
    {
        AddPlayers = true,
        AddEncounters = true,
    };

    public TrackingLocationConfig HighEndContent { get; set; } = new()
    {
        AddPlayers = true,
        AddEncounters = true,
    };

    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public bool IsPluginEnabled { get; set; }

    public bool IsWindowSizeLocked { get; set; }

    public bool IsWindowPositionLocked { get; set; }

    public bool OnlyShowWindowWhenLoggedIn { get; set; }

    public TrackingLocationConfig GetTrackingLocationConfig(ToadLocationType locType) => locType switch
    {
        ToadLocationType.Overworld => this.Overworld,
        ToadLocationType.Content => this.Content,
        ToadLocationType.HighEndContent => this.HighEndContent,
        ToadLocationType.None => throw new ArgumentException($"Unsupported location type: {locType}"),
        _ => throw new ArgumentException($"Unsupported location type: {locType}"),
    };

    public void ClearCategoryIds(int categoryId)
    {
        if (this.Overworld.DefaultCategoryId == categoryId)
        {
            this.Overworld.DefaultCategoryId = 0;
        }

        if (this.Content.DefaultCategoryId == categoryId)
        {
            this.Content.DefaultCategoryId = 0;
        }

        if (this.HighEndContent.DefaultCategoryId == categoryId)
        {
            this.HighEndContent.DefaultCategoryId = 0;
        }
    }
}
