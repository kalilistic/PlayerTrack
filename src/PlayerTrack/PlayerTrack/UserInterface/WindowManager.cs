using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace PlayerTrack
{
    /// <summary>
    /// Window manager to hold plugin windows and window system.
    /// </summary>
    public class WindowManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowManager"/> class.
        /// </summary>
        /// <param name="playerTrackPlugin">PlayerTrack plugin.</param>
        public WindowManager(PlayerTrackPlugin playerTrackPlugin)
        {
            this.Plugin = playerTrackPlugin;

            // create windows
            this.MainWindow = new MainWindow(this.Plugin);
            this.ConfigWindow = new ConfigWindow(this.Plugin);
            this.ModalWindow = new ModalWindow(this.Plugin);
            this.MigrationWindow = new MigrationWindow(this.Plugin);

            // setup window system
            this.WindowSystem = new WindowSystem("PlayerTrackWindowSystem");
            this.WindowSystem.AddWindow(this.MigrationWindow);

            // add event listeners
            PlayerTrackPlugin.PluginInterface.UiBuilder.Draw += this.Draw;
            PlayerTrackPlugin.PluginInterface.UiBuilder.OpenConfigUi += this.OpenConfigUi;
        }

        /// <summary>
        /// Gets main PlayerTrack window.
        /// </summary>
        public MainWindow? MainWindow { get; }

        /// <summary>
        /// Gets config PlayerTrack window.
        /// </summary>
        public ConfigWindow? ConfigWindow { get; }

        /// <summary>
        /// Gets delete player confirmation window.
        /// </summary>
        public ModalWindow ModalWindow { get; }

        /// <summary>
        /// Gets migration window.
        /// </summary>
        public MigrationWindow MigrationWindow { get; }

        private WindowSystem WindowSystem { get; }

        private PlayerTrackPlugin Plugin { get; }

        /// <summary>
        /// Create a dummy scaled for use with tabs.
        /// </summary>
        public static void SpacerWithTabs()
        {
            ImGuiHelpers.ScaledDummy(1f);
        }

        /// <summary>
        /// Create a dummy scaled for use without tabs.
        /// </summary>
        public static void SpacerNoTabs()
        {
            ImGuiHelpers.ScaledDummy(28f);
        }

        /// <summary>
        /// Add windows after plugin start.
        /// </summary>
        public void AddWindows()
        {
            this.WindowSystem.AddWindow(this.MainWindow!);
            this.WindowSystem.AddWindow(this.ModalWindow);
            this.WindowSystem.AddWindow(this.ConfigWindow!);
        }

        /// <summary>
        /// Dispose plugin windows and commands.
        /// </summary>
        public void Dispose()
        {
            PlayerTrackPlugin.PluginInterface.UiBuilder.Draw -= this.Draw;
            PlayerTrackPlugin.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenConfigUi;
            this.WindowSystem.RemoveAllWindows();
        }

        private void Draw()
        {
            // only show when logged in
            if (!PlayerTrackPlugin.ClientState.IsLoggedIn) return;

            this.WindowSystem.Draw();
        }

        private void OpenConfigUi()
        {
            this.ConfigWindow!.IsOpen ^= true;
        }
    }
}
