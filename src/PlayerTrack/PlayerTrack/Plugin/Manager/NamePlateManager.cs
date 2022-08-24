using System;
using System.Numerics;

using Dalamud.DrunkenToad;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace PlayerTrack
{
    /// <summary>
    /// Manage name plates for players.
    /// </summary>
    public class NamePlateManager
    {
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="NamePlateManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public NamePlateManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Dispose name plates manager.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Force existing nameplates to redraw.
        /// </summary>
        public void ForceRedraw()
        {
        }
    }
}
