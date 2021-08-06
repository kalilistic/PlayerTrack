using System.Collections.Generic;
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
        /// Selected player to view.
        /// </summary>
        public Player? SelectedPlayer;

        /// <summary>
        /// Selected encounters for detailed view.
        /// </summary>
        public List<Encounter>? SelectedEncounters;

        private readonly PlayerTrackPlugin plugin;
        private Vector2? windowSize;
        private Vector2 minimizedSize;
        private Vector2 maximizedSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public MainWindow(PlayerTrackPlugin plugin)
            : base(plugin, "PlayerTrack")
        {
            this.plugin = plugin;

            // set world names for add player
            this.worldNames = this.plugin.PluginService.GameData.WorldNames().ToArray();

            // set to settings view on load if left on player detail
            if (this.plugin.Configuration.CurrentView == View.PlayerDetail)
            {
                this.plugin.Configuration.CurrentView = View.Settings;
                this.plugin.SaveConfig();
            }

            // set window sizes
            this.SetWindowSizes();
            this.Size = this.plugin.Configuration.CurrentView != View.None ?
                            this.maximizedSize : this.minimizedSize;

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

        /// <inheritdoc/>
        public override void Draw()
        {
            // set size depending on view
            this.SetWindowSizes();
            var vector2 = this.Size;
            this.windowSize = ImGui.GetWindowSize();
            if (vector2 != null)
            {
                if (this.plugin.Configuration.CurrentView == View.None)
                {
                    this.plugin.Configuration.MainWindowHeight = vector2.Value.Y;
                    this.Size = new Vector2(221, this.windowSize.Value.Y) / ImGuiHelpers.GlobalScale;
                }
                else
                {
                    this.plugin.Configuration.MainWindowHeight = vector2.Value.Y;
                    this.plugin.Configuration.MainWindowWidth = vector2.Value.X;
                    this.Size = new Vector2(this.windowSize.Value.X, this.windowSize.Value.Y) / ImGuiHelpers.GlobalScale;
                }
            }
            else
            {
                this.Size = this.windowSize;
            }

            // left panel
            ImGui.BeginChild(
                "###PlayerTrack_LeftPanel_Child",
                new Vector2(205 * ImGuiHelpers.GlobalScale, 0),
                false);
            {
                this.SearchBox();
                this.Menu();
                this.PlayerList();
                ImGui.EndChild();
            }

            // right panel
            ImGui.SameLine();
            ImGui.BeginChild("###PlayerTrack_RightPanel_Child");
            this.TabBar();
            ImGui.EndChild();
        }

        /// <summary>
        /// Show right panel.
        /// </summary>
        /// <param name="view">View to show.</param>
        public void ShowRightPanel(View view)
        {
            var vector2 = this.windowSize;
            if (vector2 != null)
            {
                this.Size = new Vector2(this.plugin.Configuration.MainWindowWidth, this.plugin.Configuration.MainWindowHeight);
            }

            this.plugin.Configuration.LastView = this.plugin.Configuration.CurrentView;
            this.plugin.Configuration.CurrentView = view;
            this.plugin.SaveConfig();
        }

        /// <summary>
        /// Hide right panel.
        /// </summary>
        public void HideRightPanel()
        {
            var vector2 = this.windowSize;
            if (vector2 != null)
            {
                this.Size = new Vector2(221 * ImGuiHelpers.GlobalScale, this.plugin.Configuration.MainWindowHeight);
            }

            this.plugin.Configuration.LastView = this.plugin.Configuration.CurrentView;
            this.plugin.Configuration.CurrentView = View.None;
            this.plugin.SaveConfig();
        }

        private static void SpacerWithTabs()
        {
            ImGuiHelpers.ScaledDummy(1f);
        }

        private static void SpacerNoTabs()
        {
            ImGuiHelpers.ScaledDummy(28f);
        }

        private void SetWindowSizes()
        {
            this.minimizedSize =
                new Vector2(220 * ImGuiHelpers.GlobalScale, this.plugin.Configuration.MainWindowHeight);
            this.maximizedSize =
                new Vector2(this.plugin.Configuration.MainWindowWidth, this.plugin.Configuration.MainWindowHeight);
        }

        private void ToggleRightPanel(View view)
        {
            if (view == this.plugin.Configuration.CurrentView)
            {
                this.HideRightPanel();
            }
            else
            {
                this.ShowRightPanel(view);
            }
        }
    }
}
