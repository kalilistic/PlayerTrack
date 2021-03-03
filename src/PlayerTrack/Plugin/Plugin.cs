// ReSharper disable UnusedMember.Global
// ReSharper disable DelegateSubtraction

using System;
using Dalamud.Plugin;

namespace PlayerTrack
{
    public class Plugin : IDalamudPlugin
    {
        private PlayerTrackPlugin _plugin;

        public string Name => "PlayerTrack";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _plugin = new PlayerTrackPlugin(Name, pluginInterface);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _plugin.Dispose();
        }
    }
}