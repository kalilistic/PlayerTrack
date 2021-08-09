using System;
using System.Numerics;

using Dalamud.DrunkenToad;
using XivCommon.Functions.NamePlates;

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
            this.plugin.XivCommon.Functions.NamePlates.OnUpdate += this.OnNamePlateUpdate;
        }

        /// <summary>
        /// Dispose name plates manager.
        /// </summary>
        public void Dispose()
        {
            this.plugin.XivCommon.Functions.NamePlates.OnUpdate -= this.OnNamePlateUpdate;
        }

        /// <summary>
        /// Force existing nameplates to redraw.
        /// </summary>
        public void ForceRedraw()
        {
            this.plugin.XivCommon.Functions.NamePlates.ForceRedraw = true;
        }

        private void OnNamePlateUpdate(NamePlateUpdateEventArgs args)
        {
            try
            {
                // check if nameplates should be used
                var restrict =
                    ContentRestrictionType.GetContentRestrictionTypeByIndex(this.plugin.Configuration.ShowNamePlates);
                if (!(restrict == ContentRestrictionType.Always ||
                      (restrict == ContentRestrictionType.ContentOnly && this.plugin.PluginService.InContent()) ||
                      (restrict == ContentRestrictionType.HighEndDutyOnly && this.plugin.PluginService.InHighEndDuty())))
                {
                    return;
                }

                // check if any nameplate features enabled
                if (!this.plugin.Configuration.ChangeNamePlateTitle &&
                    !this.plugin.Configuration.UseNamePlateColors) return;

                // check if plugin started
                if (!this.plugin.IsDoneLoading) return;

                // check if valid
                if (args.Type != PlateType.Player || args.ActorId == 0) return;

                // get player
                var actor = this.plugin.ActorManager.GetPlayerCharacter(args.ActorId);
                if (actor == null) return;
                var player = this.plugin.PlayerService.GetPlayer(actor.Name, (ushort)actor.HomeWorld.Id);
                if (player == null) return;

                // set title
                if (this.plugin.Configuration.ChangeNamePlateTitle)
                {
                    // set title by player
                    if (player.SeTitle != null)
                    {
                        args.Title = player.SeTitle;
                    }

                    // set title by category
                    else
                    {
                        var category = this.plugin.CategoryService.GetCategory(player.CategoryId);
                        if (category.IsDefault == false && category.SeName != null)
                        {
                            args.Title = category.SeName;
                        }
                    }
                }

                // set color
                if (this.plugin.Configuration.UseNamePlateColors)
                {
                    var color = this.plugin.PlayerService.GetPlayerNamePlateColor(player);
                    if (color != null)
                    {
                        args.Colour = ((Vector4)color).ToByteColor();
                    }
                }

                // force consistent nameplate style
                if (this.plugin.Configuration.ForceNamePlateStyle)
                {
                    args.Type = PlateType.LowTitleNoFc;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update nameplate.");
            }
        }
    }
}
