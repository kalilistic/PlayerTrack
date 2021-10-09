using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Plugin window which extends window with PlayerTrack.
    /// </summary>
    public abstract class PluginWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        /// <param name="windowName">Name of the window.</param>
        /// <param name="flags">ImGui flags.</param>
        protected PluginWindow(PlayerTrackPlugin plugin, string windowName, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            : base(windowName, flags)
        {
            this.Plugin = plugin;
            this.RespectCloseHotkey = this.Plugin.Configuration.RespectCloseHotkey;
        }

        /// <summary>
        /// Gets PlayerTrack for window.
        /// </summary>
        protected PlayerTrackPlugin Plugin { get; }

        /// <inheritdoc/>
        public override void Draw()
        {
        }
    }
}
