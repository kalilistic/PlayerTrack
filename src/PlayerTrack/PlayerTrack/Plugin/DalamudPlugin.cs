using Dalamud.Plugin;

namespace PlayerTrack
{
    /// <summary>
    /// Base plugin to register with dalamud.
    /// </summary>
    public class DalamudPlugin : IDalamudPlugin
    {
        private PlayerTrackPlugin playerTrackPlugin = null!;

        /// <inheritdoc/>
        public string Name => "PlayerTrack";

        /// <inheritdoc/>
        public void Dispose()
        {
            this.playerTrackPlugin.Dispose();
        }

        /// <inheritdoc/>
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.playerTrackPlugin = new PlayerTrackPlugin(this.Name, pluginInterface);
        }
    }
}
