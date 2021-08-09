using System.Collections.Generic;

using Dalamud.Interface;

namespace PlayerTrack
{
    /// <summary>
    /// PlayerTrack configuration.
    /// </summary>
    public abstract class PlayerTrackConfig
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
        public int LodestoneFailureDelay = 86400000;

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
        public int PlayerFilterType = 0;

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
        /// MainWi window width when expanded.
        /// </summary>
        public float MainWindowWidth = 142;

        /// <summary>
        /// Main window height.
        /// </summary>
        public float MainWindowHeight = 222;

        /// <summary>
        /// Plugin version to use for special processing on upgrades.
        /// </summary>
        public int PluginVersion = 0;

        /// <summary>
        /// Number of backups to keep before deleting the oldest.
        /// </summary>
        public int BackupRetention = 3;

        /// <summary>
        /// Use nameplate colors.
        /// </summary>
        public bool UseNamePlateColors = true;

        /// <summary>
        /// Change nameplate titles to their override or category name.
        /// </summary>
        public bool ChangeNamePlateTitle = true;

        /// <summary>
        /// Only create encounters when in content and skip other areas.
        /// </summary>
        public bool RestrictEncountersToContent = true;

        /// <summary>
        /// Show option to get/show player info via context menu.
        /// </summary>
        public bool ShowAddShowInfoContextMenu = true;

        /// <summary>
        /// Show option to open lodestone profile via context menu.
        /// </summary>
        public bool ShowOpenLodestoneContextMenu = true;

        /// <summary>
        /// Frequency for refreshing player list (ms).
        /// </summary>
        public long PlayerListRefreshFrequency = 1000;

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
        public int ShowNamePlates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the window is locked in size/position.
        /// </summary>
        public bool LockWindow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force LowTitleNoFc style.
        /// </summary>
        public bool ForceNamePlateStyle { get; set; }
    }
}
