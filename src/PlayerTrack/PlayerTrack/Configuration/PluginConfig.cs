using System;

using Dalamud.Configuration;

namespace PlayerTrack
{
    /// <summary>
    /// Plugin configuration class used for dalamud.
    /// </summary>
    [Serializable]
    public class PluginConfig : PlayerTrackConfig, IPluginConfiguration
    {
        /// <inheritdoc/>
        public int Version { get; set; } = 0;
    }
}
