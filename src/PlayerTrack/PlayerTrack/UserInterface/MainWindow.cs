using System.Numerics;

using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Main window for the plugin.
    /// </summary>
    public partial class MainWindow : PluginWindow
    {
        /// <summary>
        /// Window size.
        /// </summary>
        public Vector2? WindowSize;

        /// <summary>
        /// Minimized size.
        /// </summary>
        public Vector2 MinimizedSize;

        /// <summary>
        /// Maximized size.
        /// </summary>
        public Vector2 MaximizedSize;

        /// <summary>
        /// Minimized width.
        /// </summary>
        public float MinimizedWidth;
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public MainWindow(PlayerTrackPlugin plugin)
            : base(plugin, "PlayerTrack")
        {
            this.plugin = plugin;

            // set to none on load if left on player detail
            if (this.plugin.Configuration.CurrentView == View.PlayerDetail)
            {
                this.plugin.Configuration.CurrentView = View.None;
                this.plugin.SaveConfig();
            }

            // set window sizes
            this.SetWindowSizes();
            this.Size = this.plugin.Configuration.CurrentView != View.None ?
                            this.MaximizedSize : this.MinimizedSize;

            // set lock state
            if (this.plugin.Configuration.LockWindow)
            {
                this.LockWindow();
            }
            else
            {
                this.UnlockWindow();
            }

            // open window
            this.IsOpen = this.plugin.Configuration.ShowWindow;
        }

        /// <inheritdoc />
        public override void OnOpen()
        {
            this.plugin.Configuration.ShowWindow = true;
            this.plugin.SaveConfig();
        }

        /// <inheritdoc />
        public override void OnClose()
        {
            this.plugin.Configuration.ShowWindow = false;
            this.plugin.SaveConfig();
        }

        /// <summary>
        /// Lock window size/position.
        /// </summary>
        public void LockWindow()
        {
            this.Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        }

        /// <summary>
        /// Unlock window size/position.
        /// </summary>
        public void UnlockWindow()
        {
            this.Flags = ImGuiWindowFlags.None;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            // get sizes
            this.MinimizedWidth = 221 * ImGuiHelpers.GlobalScale;
            this.SetWindowSizes();
            var vector2 = this.Size;
            this.WindowSize = ImGui.GetWindowSize();

            // check if combined or separate panel
            if (this.plugin.Configuration.CombinedPlayerDetailWindow)
            {
                // set size depending on view
                if (vector2 != null)
                {
                    if (this.plugin.Configuration.CurrentView == View.None)
                    {
                        this.plugin.Configuration.MainWindowHeight = vector2.Value.Y;
                        this.Size = new Vector2(this.MinimizedWidth, this.WindowSize.Value.Y) / ImGuiHelpers.GlobalScale;
                    }
                    else
                    {
                        this.plugin.Configuration.MainWindowHeight = vector2.Value.Y;
                        this.plugin.Configuration.MainWindowWidth = vector2.Value.X;
                        this.Size = new Vector2(this.WindowSize.Value.X, this.WindowSize.Value.Y) / ImGuiHelpers.GlobalScale;
                    }
                }
                else
                {
                    this.Size = this.WindowSize;
                }

                // left panel
                ImGui.BeginChild(
                    "###PlayerTrack_LeftPanel_Child",
                    new Vector2(205 * ImGuiHelpers.GlobalScale, 0),
                    false);
                {
                    this.PlayerListControls();
                    this.PlayerList();
                    ImGui.EndChild();
                }

                // right panel
                ImGui.SameLine();
                ImGui.BeginChild("###PlayerTrack_RightPanel_Child");
                this.plugin.WindowManager.Panel?.Draw();
                ImGui.EndChild();
            }
            else
            {
                // set to minimized size if not set
                if (vector2 != null)
                {
                    this.plugin.Configuration.MainWindowHeight = vector2.Value.Y;
                    this.Size = new Vector2(this.MinimizedWidth, this.WindowSize.Value.Y) / ImGuiHelpers.GlobalScale;
                }
                else
                {
                    this.Size = this.WindowSize;
                }

                // left panel
                ImGui.BeginChild(
                    "###PlayerTrack_LeftPanel_Child",
                    new Vector2(205 * ImGuiHelpers.GlobalScale, 0),
                    false);
                {
                    this.PlayerListControls();
                    this.PlayerList();
                    ImGui.EndChild();
                }
            }
        }

        private void SetWindowSizes()
        {
            this.MinimizedSize =
                new Vector2(220 * ImGuiHelpers.GlobalScale, this.plugin.Configuration.MainWindowHeight);
            this.MaximizedSize =
                new Vector2(this.plugin.Configuration.MainWindowWidth, this.plugin.Configuration.MainWindowHeight);
        }
    }
}
