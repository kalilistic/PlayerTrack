using System.Linq;
using System.Numerics;

using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// MigrationWindow for the plugin.
    /// </summary>
    public class MigrationWindow : PluginWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MigrationWindow"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public MigrationWindow(PlayerTrackPlugin plugin)
            : base(plugin, "PlayerTrack Migration")
        {
            this.Size = new Vector2(500f, 500f);
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            this.Size = null;
            ImGui.TextWrapped(Loc.Localize(
                                  "MigrationInfo",
                                  "Thank you for updating PlayerTrack! The new version needs to migrate your data to a new format. " +
                                  "This only runs once but may take awhile depending on how much data you have. " +
                                  "Please be patient and do not interrupt the migration or you risk data loss. " +
                                  "You can continue to play the game and this window will automatically close when complete."));
            ImGui.Spacing();
            var messages = Migrator.Messages.ToList();
            foreach (var message in messages)
            {
                ImGui.TextWrapped(message);
            }
        }
    }
}
