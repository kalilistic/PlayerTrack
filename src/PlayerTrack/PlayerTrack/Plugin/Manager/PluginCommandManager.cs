using Dalamud.Game.Command;

namespace PlayerTrack
{
    /// <summary>
    /// Manage plugin commands.
    /// </summary>
    public class PluginCommandManager
    {
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginCommandManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public PluginCommandManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            PlayerTrackPlugin.CommandManager.AddHandler("/ptrack", new CommandInfo(this.TogglePlayerTrack)
            {
                HelpMessage = "Show/hide PlayerTrack.",
                ShowInHelp = true,
            });
            PlayerTrackPlugin.CommandManager.AddHandler("/ptrackconfig", new CommandInfo(this.TogglePlayerTrackConfig)
            {
                HelpMessage = "Open PlayerTrack settings.",
                ShowInHelp = true,
            });
            PlayerTrackPlugin.CommandManager.AddHandler("/ptrackintegrity", new CommandInfo(this.RunIntegrityCheck)
            {
                HelpMessage = "Clean-up and delete erroneous data from previous versions.",
                ShowInHelp = true,
            });
            PlayerTrackPlugin.CommandManager.AddHandler("/ptrackowenc", new CommandInfo(this.DeleteOverworldEncounters)
            {
                HelpMessage = "Delete overworld encounters from previous versions or settings.",
                ShowInHelp = true,
            });
        }

        /// <summary>
        /// Dispose command manager.
        /// </summary>
        public void Dispose()
        {
            PlayerTrackPlugin.CommandManager.RemoveHandler("/ptrack");
            PlayerTrackPlugin.CommandManager.RemoveHandler("/ptrackconfig");
            PlayerTrackPlugin.CommandManager.RemoveHandler("/ptrackintegrity");
            PlayerTrackPlugin.CommandManager.RemoveHandler("/ptrackowenc");
        }

        private void TogglePlayerTrack(string command, string arguments)
        {
            this.plugin.WindowManager.MainWindow!.IsOpen = !this.plugin.WindowManager.MainWindow!.IsOpen;
            if (this.plugin.WindowManager.MainWindow!.IsOpen)
            {
                this.plugin.PlayerService.ResetViewPlayers();
            }
        }

        private void TogglePlayerTrackConfig(string command, string arguments)
        {
            this.plugin.WindowManager.ConfigWindow!.IsOpen ^= true;
        }

        private void RunIntegrityCheck(string command, string arguments)
        {
            this.plugin.RunIntegrityCheck();
        }

        private void DeleteOverworldEncounters(string command, string arguments)
        {
            this.plugin.EncounterService.DeleteOverworldEncounters();
        }
    }
}
