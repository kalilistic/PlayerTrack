using System;
using Dalamud.Configuration;

namespace PlayerTrack
{
    [Serializable]
    public class PluginConfig : PlayerTrackConfig, IPluginConfiguration
    {
        public int Version { get; set; } = 0;
    }
}