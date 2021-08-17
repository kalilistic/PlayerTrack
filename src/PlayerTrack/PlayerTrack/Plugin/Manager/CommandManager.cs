using Dalamud.Game.Command;

namespace PlayerTrack
{
    /// <summary>
    /// Manage plugin commands.
    /// </summary>
    public class CommandManager
    {
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public CommandManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.plugin.PluginService.PluginInterface.CommandManager.AddHandler("/ptrack", new CommandInfo(this.TogglePlayerTrack)
            {
                HelpMessage = "Show/hide PlayerTrack.",
                ShowInHelp = true,
            });
            this.plugin.PluginService.PluginInterface.CommandManager.AddHandler("/ptrackconfig", new CommandInfo(this.TogglePlayerTrackConfig)
            {
                HelpMessage = "Open PlayerTrack settings.",
                ShowInHelp = true,
            });
            this.plugin.PluginService.PluginInterface.CommandManager.AddHandler("/ptrackintegrity", new CommandInfo(this.RunIntegrityCheck)
            {
                HelpMessage = "Clean-up and delete erroneous data from previous versions.",
                ShowInHelp = true,
            });
            this.plugin.PluginService.PluginInterface.CommandManager.AddHandler("/ptrackowenc", new CommandInfo(this.DeleteOverworldEncounters)
            {
                HelpMessage = "Delete overworld encounters from previous versions.",
                ShowInHelp = true,
            });
        }

        /// <summary>
        /// Dispose command manager.
        /// </summary>
        public void Dispose()
        {
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrack");
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrackconfig");
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrackintegrity");
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrackowenc");
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
