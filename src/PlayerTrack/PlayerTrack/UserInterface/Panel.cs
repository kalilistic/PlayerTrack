using System.Collections.Generic;
using System.Numerics;

using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <summary>
    /// Panel Tab.
    /// </summary>
    public partial class Panel
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Panel"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public Panel(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;

            // set world names for add player
            this.worldNames = PlayerTrackPlugin.DataManager.WorldNames();
        }

        /// <summary>
        /// Show right panel.
        /// </summary>
        /// <param name="view">View to show.</param>
        public void ShowPanel(View view)
        {
            if (this.plugin.Configuration.CombinedPlayerDetailWindow)
            {
                this.plugin.WindowManager.MainWindow!.Size = new Vector2(this.plugin.Configuration.MainWindowWidth, this.plugin.Configuration.MainWindowHeight);
            }

            this.plugin.Configuration.LastView = this.plugin.Configuration.CurrentView;
            this.plugin.Configuration.CurrentView = view;
            this.plugin.SaveConfig();
        }

        /// <summary>
        /// Hide right panel.
        /// </summary>
        public void HidePanel()
        {
            if (this.plugin.Configuration.CombinedPlayerDetailWindow)
            {
                var vector2 = this.plugin.WindowManager.MainWindow!.WindowSize;
                if (vector2 != null)
                {
                    this.plugin.WindowManager.MainWindow!.Size = new Vector2(this.plugin.WindowManager.MainWindow!.MinimizedWidth, this.plugin.Configuration.MainWindowHeight);
                }
            }

            this.plugin.Configuration.LastView = this.plugin.Configuration.CurrentView;
            this.plugin.Configuration.CurrentView = View.None;
            this.plugin.SaveConfig();
        }

        /// <summary>
        /// Toggle panel view.
        /// </summary>
        /// <param name="view">view.</param>
        public void TogglePanel(View view)
        {
            if (view == this.plugin.Configuration.CurrentView)
            {
                this.HidePanel();
            }
            else
            {
                this.ShowPanel(view);
            }
        }
    }
}
