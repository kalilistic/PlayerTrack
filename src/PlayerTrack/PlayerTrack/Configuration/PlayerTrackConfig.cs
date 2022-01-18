using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Interface;

namespace PlayerTrack
{
    /// <summary>
    /// PlayerTrack configuration.
    /// </summary>
    public class PlayerTrackConfig : IPluginConfiguration
    {
        /// <summary>
        /// Frequency to show alerts to avoid spamming.
        /// </summary>
        public int AlertFrequency = 14400000;

        /// <summary>
        /// Show icons for players in list view.
        /// </summary>
        public List<FontAwesomeIcon> EnabledIcons = new ();

        /// <summary>
        /// Delay period for lodestone lookups after max failures reached.
        /// </summary>
        public long LodestoneCooldownDuration = 3600000;

        /// <summary>
        /// Delay period for lodestone lookup for a particular character after failure.
        /// </summary>
        public int LodestoneFailureDelay = 259200000;

        /// <summary>
        /// Delay period for reprocessing players for lodestone requests.
        /// </summary>
        public int LodestoneReprocessDelay = 3600000;

        /// <summary>
        /// Lodestone locale to open player profile with.
        /// </summary>
        public LodestoneLocale LodestoneLocale = LodestoneLocale.na;

        /// <summary>
        /// Search type for name search.
        /// </summary>
        public PlayerSearchType SearchType = PlayerSearchType.contains;

        /// <summary>
        /// List mode for what players to display.
        /// </summary>
        public int PlayerFilterType = 2;

        /// <summary>
        /// Category to filter list by.
        /// </summary>
        public int CategoryFilterId = 0;

        /// <summary>
        /// Max number of times to try lodestone lookup before cooldown.
        /// </summary>
        public int LodestoneMaxFailure = 3;

        /// <summary>
        /// Max number of times to retry a particular character automatically.
        /// </summary>
        public int LodestoneMaxRetry = 2;

        /// <summary>
        /// Max number of failures before running health check to see if lodestone is available.
        /// </summary>
        public int LodestoneMaxSubsequentFailures = 3;

        /// <summary>
        /// Amount of time to wait for response for lodestone call.
        /// </summary>
        public int LodestoneTimeout = 60000;

        /// <summary>
        /// Amount of time to cycle through all events in queue.
        /// </summary>
        public int LodestoneQueueFrequency = 15000;

        /// <summary>
        /// Toggle to disable tracking in combat.
        /// </summary>
        public bool RestrictInCombat = true;

        /// <summary>
        /// Toggle to disable nameplates in combat.
        /// </summary>
        public bool RestrictNamePlatesInCombat = false;

        /// <summary>
        /// Toggle to restrict tracking to content only.
        /// </summary>
        public bool RestrictToContent = true;

        /// <summary>
        /// Toggle to restrict tracking to high-end duty only.
        /// </summary>
        public bool RestrictToHighEndDuty = false;

        /// <summary>
        /// Toggle to disable lodestone lookups.
        /// </summary>
        public bool SyncToLodestone = true;

        /// <summary>
        /// Toggle to disable sync with visibility.
        /// </summary>
        public bool SyncWithVisibility = false;

        /// <summary>
        /// Toggle to disable sync with FCNameColor.
        /// </summary>
        public bool SyncWithFCNameColor = false;

        /// <summary>
        /// Create dynamic FC categories with FCNameColor.
        /// </summary>
        public bool CreateDynamicFCCategories = false;

        /// <summary>
        /// Move players from existing categories and not just default.
        /// </summary>
        public bool ReassignPlayersFromExistingCategory = false;

        /// <summary>
        /// Toggle to hide players voided in visibility in list.
        /// </summary>
        public bool ShowVoidedPlayersInList = false;

        /// <summary>
        /// Visibility sync frequency in ms.
        /// </summary>
        public long SyncWithVisibilityFrequency = 30000;

        /// <summary>
        /// FCNameColor sync frequency in ms.
        /// </summary>
        public long SyncWithFCNameColorFrequency = 30000;

        /// <summary>
        /// Indicator if fresh install to perform one-time setup actions.
        /// </summary>
        public bool FreshInstall = true;

        /// <summary>
        /// Last view.
        /// </summary>
        public View LastView = View.None;

        /// <summary>
        /// Current view.
        /// </summary>
        public View CurrentView = View.None;

        /// <summary>
        /// Main window width when expanded.
        /// </summary>
        public float MainWindowWidth = 700;

        /// <summary>
        /// Main window height.
        /// </summary>
        public float MainWindowHeight = 400;

        /// <summary>
        /// Plugin version to use for special processing on upgrades.
        /// </summary>
        public int PluginVersion = 0;

        /// <summary>
        /// Number of backups to keep before deleting the oldest.
        /// </summary>
        public int BackupRetention = 7;

        /// <summary>
        /// Backup frequency in ms.
        /// </summary>
        public long BackupFrequency = 86400000;

        /// <summary>
        /// Last backup in ms.
        /// </summary>
        public long LastBackup;

        /// <summary>
        /// Use nameplate colors.
        /// </summary>
        public bool UseNamePlateColors = true;

        /// <summary>
        /// Change nameplate titles to their override or category name.
        /// </summary>
        public bool ChangeNamePlateTitle = true;

        /// <summary>
        /// Don't update nameplate if player is dead.
        /// </summary>
        public bool DisableNamePlateColorIfDead = true;

        /// <summary>
        /// Default the nameplate color to the list color unless changed.
        /// </summary>
        public bool DefaultNamePlateColorToListColor = true;

        /// <summary>
        /// Show option to get/show player info via context menu.
        /// </summary>
        public bool ShowAddShowInfoContextMenu = true;

        /// <summary>
        /// Show option to open lodestone profile via context menu.
        /// </summary>
        public bool ShowOpenLodestoneContextMenu = true;

        /// <summary>
        /// Show option to set category via context menu.
        /// </summary>
        public bool ShowSetCategoryContextMenu = true;

        /// <summary>
        /// Frequency for refreshing player list (ms).
        /// </summary>
        public long PlayerListRefreshFrequency = 1000;

        /// <summary>
        /// Threshold in ms before creating new encounter within same territoryType.
        /// </summary>
        public int CreateNewEncounterThreshold = 600000;

        /// <summary>
        /// Gets or sets list of items to show above.
        /// </summary>
        public List<byte> ShowContextAboveThis { get; set; } = new ();

        /// <summary>
        /// Gets or sets list of items to show below.
        /// </summary>
        public List<byte> ShowContextBelowThis { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether show main window.
        /// </summary>
        public bool ShowWindow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show filter type on player list.
        /// </summary>
        public bool ShowFilterType { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show search box on player list.
        /// </summary>
        public bool ShowSearchBox { get; set; } = true;

        /// <summary>
        /// Gets or sets add players restriction.
        /// </summary>
        public int RestrictAddUpdatePlayers { get; set; } = 1;

        /// <summary>
        /// Gets or sets add encounters restriction.
        /// </summary>
        public int RestrictAddEncounters { get; set; } = 1;

        /// <summary>
        /// Gets or sets show nameplates restriction.
        /// </summary>
        public int ShowNamePlates { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether the window is locked in size/position.
        /// </summary>
        public bool LockWindow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force LowTitleNoFc style.
        /// </summary>
        public bool ForceNamePlateStyle { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to open player details combined or separate.
        /// </summary>
        public bool CombinedPlayerDetailWindow { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show tags.
        /// </summary>
        public bool ShowPlayerTags { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to respect close key.
        /// </summary>
        public bool RespectCloseHotkey { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to search tags.
        /// </summary>
        public bool SearchTags { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating data fix version.
        /// </summary>
        public uint DataFixVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use offset in player list.
        /// </summary>
        public bool PlayerListOffset { get; set; } = true;

        /// <inheritdoc />
        public int Version { get; set; }
    }
}
