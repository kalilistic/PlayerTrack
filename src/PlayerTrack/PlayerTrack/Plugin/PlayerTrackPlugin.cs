using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Plugin;
using XivCommon;

namespace PlayerTrack
{
    /// <summary>
    /// PlayerTrack.
    /// </summary>
    public class PlayerTrackPlugin
    {
        /// <summary>
        /// Plugin service.
        /// </summary>
        public PluginService PluginService = null!;

        /// <summary>
        /// XivCommon library instance.
        /// </summary>
        public XivCommonBase XivCommon = null!;

        private Timer backupTimer = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerTrackPlugin"/> class.
        /// </summary>
        /// <param name="pluginName">Plugin name.</param>
        /// <param name="pluginInterface">Plugin interface.</param>
        public PlayerTrackPlugin(string pluginName, DalamudPluginInterface pluginInterface)
        {
            Task.Run(() =>
            {
                try
                {
                    // setup common libs
                    this.PluginService = new PluginService(pluginName, pluginInterface);
                    const Hooks hooks = Hooks.NamePlates | Hooks.ContextMenu;
                    this.XivCommon = new XivCommonBase(pluginInterface, hooks);

                    // load config
                    try
                    {
                        this.Configuration = this.PluginService.LoadConfig() as PluginConfig ?? new PluginConfig();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Failed to load config so creating new one.", ex);
                        this.Configuration = new PluginConfig();
                        this.SaveConfig();
                    }

                    // setup services
                    this.BaseRepository = new BaseRepository(this.PluginService);
                    this.LodestoneService = new LodestoneService(this);
                    this.ActorManager = new ActorManager(this);
                    this.CategoryService = new CategoryService(this);
                    this.EncounterService = new EncounterService(this);
                    this.PlayerService = new PlayerService(this);
                    this.WindowManager = new WindowManager(this);
                    this.CommandManager = new CommandManager(this);
                    this.ContextMenuManager = new ContextMenuManager(this);
                    this.NamePlateManager = new NamePlateManager(this);

                    // run backup
                    this.backupTimer = new Timer { Interval = this.Configuration.BackupFrequency, Enabled = false };
                    this.backupTimer.Elapsed += this.BackupTimerOnElapsed;
                    var pluginVersion = this.PluginService.PluginVersionNumber();
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
        /// Gets or sets a value indicating whether plugin is done loading.
        /// </summary>
        public bool IsDoneLoading { get; set; }

        /// <summary>
        /// Gets or sets plugin configuration.
        /// </summary>
        public PlayerTrackConfig Configuration { get; set; } = null!;

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
        public CommandManager CommandManager { get; set; } = null!;

        /// <summary>
        /// Save plugin configuration.
        /// </summary>
        public void SaveConfig()
        {
            this.PluginService.SaveConfig(this.Configuration);
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
                this.CommandManager.Dispose();
                this.NamePlateManager.Dispose();
                this.ContextMenuManager.Dispose();
                this.XivCommon.Dispose();
                this.LodestoneService.Dispose();
                this.PluginService.Dispose();
                this.ActorManager.Dispose();
                this.WindowManager.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to dispose plugin properly.");
            }

            this.PluginService.PluginInterface.Dispose();
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
            var helpMessages = new[]
            {
                Loc.Localize(
                    "HelpMessage1",
                    "PlayerTrack helps you keep a record of who you meet and the content you played together. " +
                    "By default, this is instanced content only - but you can expand or restrict this in settings. " +
                    "You can see all the details on a player by clicking on their name in the overlay. " +
                    "Here you can also record notes and set a personalized icon/color."),
                Loc.Localize(
                    "HelpMessage2",
                    "PlayerTrack uses Lodestone to keep the data updated (e.g. world transfers). " +
                    "If this happens, you'll see an indicator next to their home world and " +
                    "can mouse-over to see their previous residence."),
                Loc.Localize(
                    "HelpMessage3",
                    "If you need help, reach out on discord or open an issue on GitHub. If you want to " +
                    "help add translations, you can submit updates on Crowdin."),
            };
            this.PluginService.Chat.PrintNotice(helpMessages);
        }

        /// <summary>
        /// Open examine window for actor.
        /// </summary>
        /// <param name="actorId">actor id.</param>
        public void OpenExamineWindow(int actorId)
        {
            var player = this.PlayerService.GetPlayer((uint)actorId);
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
                this.PluginService.BackupManager.CreateBackup();
                this.PluginService.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
            }
        }

        private void RunUpgradeBackup()
        {
            this.Configuration.LastBackup = DateUtil.CurrentTime();
            this.PluginService.BackupManager.CreateBackup("upgrade/v" + this.Configuration.PluginVersion + "_");
            this.PluginService.BackupManager.DeleteBackups(this.Configuration.BackupRetention);
        }

        private void HandleFreshInstall()
        {
            if (!this.Configuration.FreshInstall)
            {
                return;
            }

            this.BaseRepository.SetVersion(3);
            this.SetDefaultIcons();
            this.PluginService.Chat.PrintNotice(Loc.Localize("InstallThankYou", "Thank you for installing PlayerTrack!"));
            this.PrintHelpMessage();
            this.Configuration.FreshInstall = false;
            this.SaveConfig();
        }
    }
}
