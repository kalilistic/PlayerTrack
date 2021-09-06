using System.Collections.Generic;

using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// PlayerDetail for when displayed separately.
    /// </summary>
    public class PlayerDetailWindow : PluginWindow
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
        /// Initializes a new instance of the <see cref="PlayerDetailWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public PlayerDetailWindow(PlayerTrackPlugin plugin)
            : base(plugin, "PlayerTrack###PlayerDetail")
        {
            this.plugin = plugin;

            // set to none on load if left on player detail
            if (this.plugin.Configuration.CurrentView == View.PlayerDetail)
            {
                this.plugin.Configuration.CurrentView = View.None;
                this.plugin.SaveConfig();
            }

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
            this.IsOpen = true;
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
            if (this.plugin.Configuration.CurrentView != View.None)
            {
                this.plugin.WindowManager.Panel!.Draw();
            }
            else
            {
                ImGui.Text(Loc.Localize("NoPlayerSelected", "No player selected."));
            }
        }
    }
}
