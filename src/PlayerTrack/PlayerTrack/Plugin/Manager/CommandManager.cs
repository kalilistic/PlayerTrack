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
                HelpMessage = "Show/hide PlayerTrack settings.",
                ShowInHelp = false,
            });
        }

        /// <summary>
        /// Dispose command manager.
        /// </summary>
        public void Dispose()
        {
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrack");
            this.plugin.PluginService.PluginInterface.CommandManager.RemoveHandler("/ptrackconfig");
        }

        private void TogglePlayerTrack(string command, string arguments)
        {
            this.plugin.WindowManager.MainWindow!.IsOpen = !this.plugin.WindowManager.MainWindow!.IsOpen;
        }

        private void TogglePlayerTrackConfig(string command, string arguments)
        {
            this.plugin.Configuration.CurrentView = View.Settings;
            this.plugin.SaveConfig();
            this.plugin.WindowManager.MainWindow!.IsOpen = true;
        }
    }
}
