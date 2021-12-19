using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

using CheapLoc;
using Dalamud.Data;
using Dalamud.DrunkenToad;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using XivCommon;

namespace PlayerTrack
{
    /// <summary>
    /// PlayerTrack.
    /// </summary>
    public class PlayerTrackPlugin : IDalamudPlugin
    {
        /// <summary>
        /// XivCommon library instance.
        /// </summary>
        public XivCommonBase XivCommon = null!;

        /// <summary>
        /// Backup manager.
        /// </summary>
        public BackupManager BackupManager = null!;

        private Timer backupTimer = null!;
        private Localization localization = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerTrackPlugin"/> class.
        /// </summary>
        public PlayerTrackPlugin()
        {
            Task.Run(() =>
            {
                try
                {
                    // setup common libs
                    this.localization = new Localization(PluginInterface, CommandManager);
                    this.BackupManager = new BackupManager(PluginInterface.GetPluginConfigDirectory());
                    this.XivCommon = new XivCommonBase(Hooks.NamePlates | Hooks.ContextMenu);

                    // load config
                    try
                    {
                        this.Configuration = PluginInterface.GetPluginConfig() as PlayerTrackConfig ??
                                             new PlayerTrackConfig();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to load config so creating new one.", ex);
                        this.Configuration = new PlayerTrackConfig();
                        this.SaveConfig();
                    }

                    // setup services
                    this.BaseRepository = new BaseRepository(GetPluginFolder());
                    this.LodestoneService = new LodestoneService(this);
                    this.ActorManager = new ActorManager(this);
                    this.CategoryService = new CategoryService(this);
                    this.EncounterService = new EncounterService(this);
                    this.PlayerService = new PlayerService(this);
                    this.VisibilityService = new VisibilityService(this);
                    this.FCNameColorService = new FCNameColorService(this);
                    this.PlayerTrackProvider = new PlayerTrackProvider(PluginInterface, new PlayerTrackAPI(this));
                    this.WindowManager = new WindowManager(this);
                    this.PluginCommandManager = new PluginCommandManager(this);
                    this.ContextMenuManager = new ContextMenuManager(this);
                    this.NamePlateManager = new NamePlateManager(this);

                    // run backup
                    this.backupTimer = new Timer { Interval = this.Configuration.BackupFrequency, Enabled = false };
                    this.backupTimer.Elapsed += this.BackupTimerOnElapsed;
                    var pluginVersion = Assembly.GetExecutingAssembly().VersionNumber();
                    if (this.Configuration.PluginVersion < pluginVersion)
                    {
                        Logger.LogInfo("Running backup since new version detected.");
                        this.RunUpgradeBackup();
                        this.Configuration.PluginVersion = pluginVersion;
                        this.SaveConfig();
                    }
                    else
                    {
                        this.BackupTimerOnElapsed(this, null);
                    }

                    // migrate if needed
                    var success = Migrator.Migrate(this);
                    if (success)
                    {
                        // special handling for fresh install
                        this.HandleFreshInstall();

                        // reset categories if failed to load
                        if (this.CategoryService.GetCategories().Length == 0)
                        {
                            this.CategoryService.ResetCategories();
                        }

                        // start plugin
                        this.IsDoneLoading = true;
                        this.backupTimer.Enabled = true;
                        this.VisibilityService.Start();
                        this.FCNameColorService.Start();
                        this.ActorManager.Start();
                        this.WindowManager.AddWindows();
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to initialize plugin.");
                }
            });
        }

        /// <summary>
        /// Gets pluginInterface.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static DalamudPluginInterface PluginInterface { get; private set; } = null!;

        /// <summary>
        /// Gets command manager.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static CommandManager CommandManager { get; private set; } = null!;

        /// <summary>
        /// Gets chat gui.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static ChatGui Chat { get; private set; } = null!;

        /// <summary>
        /// Gets client state.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static ClientState ClientState { get; private set; } = null!;

        /// <summary>
        /// Gets framework.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static Framework Framework { get; private set; } = null!;

        /// <summary>
        /// Gets condition.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static Condition Condition { get; private set; } = null!;

        /// <summary>
        /// Gets data manager.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static DataManager DataManager { get; private set; } = null!;

        /// <summary>
        /// Gets object table.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static ObjectTable ObjectTable { get; private set; } = null!;

        /// <summary>
        /// Gets target manager.
        /// </summary>
        [PluginService]
        [RequiredVersion("1.0")]
        public static TargetManager TargetManager { get; private set; } = null!;

        /// <inheritdoc/>
        public string Name => "PlayerTrack";

        /// <summary>
        /// Gets or sets playerTrack API for other plugins.
        /// </summary>
        public PlayerTrackProvider PlayerTrackProvider { get; set; } = null!;

        /// <summary>
        /// Gets or sets visibility service to get data from visibility plugin.
        /// </summary>
        public VisibilityService VisibilityService { get; set; } = null!;

        /// <summary>
        /// Gets or sets FCNameColor service to get data from FCNameColor plugin.
        /// </summary>
        public FCNameColorService FCNameColorService { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether plugin is done loading.
        /// </summary>
        public bool IsDoneLoading { get; set; }

        /// <summary>
        /// Gets or sets plugin configuration.
        /// </summary>
        public PlayerTrackConfig Configuration { get; set; } = new ();

        /// <summary>
        /// Gets or sets base repository.
        /// </summary>
        public BaseRepository BaseRepository { get; set; } = null!;

        /// <summary>
        /// Gets or sets player service.
        /// </summary>
        public PlayerService PlayerService { get; set; } = null!;

        /// <summary>
        /// Gets or sets encounter service.
        /// </summary>
        public EncounterService EncounterService { get; set; } = null!;

        /// <summary>
        /// Gets or sets category service.
        /// </summary>
        public CategoryService CategoryService { get; set; } = null!;

        /// <summary>
        /// Gets or sets lodestone service.
        /// </summary>
        public LodestoneService LodestoneService { get; set; } = null!;

        /// <summary>
        /// Gets or sets Actor manager.
        /// </summary>
        public ActorManager ActorManager { get; set; } = null!;

        /// <summary>
        /// Gets or sets window manager.
        /// </summary>
        public WindowManager WindowManager { get; set; } = null!;

        /// <summary>
        /// Gets or sets context Menu manager to handle player context menu.
        /// </summary>
        public ContextMenuManager ContextMenuManager { get; set; } = null!;

        /// <summary>
        /// Gets or sets name plate manager to handle custom nameplates.
        /// </summary>
        public NamePlateManager NamePlateManager { get; set; } = null!;

        /// <summary>
        /// Gets or sets command manager to handle user commands.
        /// </summary>
        public PluginCommandManager PluginCommandManager { get; set; } = null!;

        /// <summary>
        /// Get plugin folder.
        /// </summary>
        /// <returns>plugin folder name.</returns>
        public static string GetPluginFolder()
        {
            return PluginInterface.GetPluginConfigDirectory();
        }

        /// <summary>
        /// Save plugin configuration.
        /// </summary>
        public void SaveConfig()
        {
            PluginInterface.SavePluginConfig(this.Configuration);
        }

        /// <summary>
        /// Dispose player track.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.backupTimer.Elapsed -= this.BackupTimerOnElapsed;
                this.backupTimer.Dispose();
                this.PluginCommandManager.Dispose();
                this.NamePlateManager.Dispose();
                this.ContextMenuManager.Dispose();
                this.XivCommon.Dispose();
                this.PlayerTrackProvider.Dispose();
                this.VisibilityService.Dispose();
                this.FCNameColorService.Dispose();
                this.LodestoneService.Dispose();
                this.ActorManager.Dispose();
                this.WindowManager.Dispose();
                this.localization.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispose plugin properly.");
            }

            PluginInterface.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get list of icon names.
        /// </summary>
        /// <returns>array of icon names.</returns>
        public string[] IconListNames()
        {
            var namesList = new List<string> { Loc.Localize("None", "None") };
            namesList.AddRange(this.Configuration.EnabledIcons.ToList()
                                   .Select(icon => icon.ToString()));
            return namesList.ToArray();
        }

        /// <summary>
        /// Get list of icon codes.
        /// </summary>
        /// <returns>array of icon codes.</returns>
        public int[] IconListCodes()
        {
            var codesList = new List<int> { 0 };
            codesList.AddRange(this.Configuration.EnabledIcons.ToList().Select(icon => (int)icon));
            return codesList.ToArray();
        }

        /// <summary>
        /// Get icon index by code.
        /// </summary>
        /// <param name="code">icon code.</param>
        /// <returns>icon index.</returns>
        public int IconListIndex(int code)
        {
            try
            {
                return Array.IndexOf(this.IconListCodes(), code);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Print help message to chat how to use plugin.
        /// </summary>
        public void PrintHelpMessage()
        {
            Chat.PluginPrintNotice(Loc.Localize(
                                       "HelpMessage1",
                                       "PlayerTrack helps you keep a record of who you meet and the content you played together. " +
                                       "By default, this is instanced content only - but you can expand or restrict this in settings. " +
                                       "You can see all the details on a player by clicking on their name in the overlay. " +
                                       "Here you can also record notes and set a personalized icon/color."));
            Chat.PluginPrintNotice(Loc.Localize(
                                       "HelpMessage2",
                                       "PlayerTrack uses Lodestone to keep the data updated (e.g. world transfers). " +
                                       "If this happens, you'll see an indicator next to their home world and " +
                                       "can mouse-over to see their previous residence."));
            Chat.PluginPrintNotice(Loc.Localize(
                                       "HelpMessage3",
                                       "If you need help, reach out on discord or open an issue on GitHub. If you want to " +
                                       "help add translations, you can submit updates on Crowdin."));
        }

        /// <summary>
        /// Open examine window for actor.
        /// </summary>
        /// <param name="actorId">actor id.</param>
        public void OpenExamineWindow(uint actorId)
        {
            var player = this.PlayerService.GetPlayer(actorId);
            if (player is not { IsCurrent: true }) return;
            try
            {
                this.XivCommon.Functions.Examine.OpenExamineWindow(actorId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to open examine window");
            }
        }

        /// <summary>
        /// Fix or delete records based on known issues from previous versions.
        /// This should be used with caution since it's destructive and irreversible.
        /// </summary>
        public void RunIntegrityCheck()
        {
            Logger.LogInfo("Starting integrity check.");

            // fix category ranks
            var categories = this.CategoryService.GetCategories().OrderBy(pair => pair.Value.Rank);

            var count = 0;
            foreach (var category in categories)
            {
                category.Value.Rank = count;
                count += 1;
            }

            // delete invalid players
            var players = this.PlayerService.GetPlayers();
            if (players != null)
            {
                foreach (var player in players)
                {
                    if (player.Value.HomeWorlds.First().Key == 0 ||
                        !player.Value.Names.First().IsValidCharacterName())
                    {
                        Logger.LogInfo($"Deleting Player: {player.Value.Key}");
                        this.PlayerService.DeletePlayer(player.Value);
                    }
                }
            }

            // delete orphan encounters
            var playerKeys = this.PlayerService.GetPlayers()?.Select(pair => pair.Key).ToList() !;
            var encounters = this.EncounterService.GetEncounters().ToList();
            foreach (var encounter in encounters)
            {
                if (!playerKeys.Contains(encounter.PlayerKey))
                {
                    Logger.LogInfo($"Deleting Encounter: {encounter.PlayerKey} {encounter.LocationName}");
                    this.EncounterService.DeleteEncounter(encounter);
                }
            }

            // run rebuild
            this.BaseRepository.RebuildDatabase();

            Logger.LogInfo("Finished running integrity check.");
        }

        /// <summary>
        /// Set default icons for fresh install or on reset.
        /// </summary>
        public void SetDefaultIcons()
        {
            this.Configuration.EnabledIcons = new List<FontAwesomeIcon>
            {
                FontAwesomeIcon.GrinBeam,
                FontAwesomeIcon.GrinAlt,
                FontAwesomeIcon.Meh,
                FontAwesomeIcon.Frown,
                FontAwesomeIcon.Angry,
                FontAwesomeIcon.Flushed,
                FontAwesomeIcon.Surprise,
                FontAwesomeIcon.Tired,
            };
            this.SaveConfig();
        }

        private void BackupTimerOnElapsed(object sender, ElapsedEventArgs? e)
        {
            if (DateUtil.CurrentTime() > this.Configuration.LastBackup + this.Configuration.BackupFrequency)
            {
                Logger.LogInfo("Running backup due to frequency timer.");
                this.Configuration.LastBackup = DateUtil.CurrentTime();
                this.BackupManager.CreateBackup();
                this.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
            }
        }

        private void RunUpgradeBackup()
        {
            this.Configuration.LastBackup = DateUtil.CurrentTime();
            this.BackupManager.CreateBackup("upgrade/v" + this.Configuration.PluginVersion + "_");
            this.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
        }

        private void HandleFreshInstall()
        {
            if (!this.Configuration.FreshInstall)
            {
                if (this.Configuration.LodestoneFailureDelay < 259200000)
                {
                    this.Configuration.LodestoneFailureDelay = 259200000;
                    this.SaveConfig();
                }

                return;
            }

            this.BaseRepository.SetVersion(3);
            this.SetDefaultIcons();
            Chat.PluginPrintNotice(Loc.Localize("InstallThankYou", "Thank you for installing PlayerTrack!"));
            this.PrintHelpMessage();
            this.Configuration.FreshInstall = false;
            this.Configuration.DataFixVersion = 3;
            this.SaveConfig();
        }
    }
}
