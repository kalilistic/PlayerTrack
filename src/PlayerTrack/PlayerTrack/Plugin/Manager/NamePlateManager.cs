using System;
using System.Linq;
using System.Numerics;

using Dalamud.DrunkenToad;
using Dalamud.Game.ClientState.Actors.Types;
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
                if (this.plugin.Configuration.RestrictNamePlatesInCombat &&
                    this.plugin.PluginService.ClientState.Condition.InCombat())
                {
                    return;
                }

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

                // get actor
                if (this.plugin.PluginService.PluginInterface.ClientState.Actors.FirstOrDefault(act => act.ActorId == args.ActorId) is not PlayerCharacter actor) return;

                var player = this.plugin.PlayerService.GetPlayer(actor.Name, (ushort)actor.HomeWorld.Id);
                if (player == null) return;

                // set category
                var category = this.plugin.CategoryService.GetCategory(player.CategoryId);

                // force consistent nameplate style
                if (this.plugin.Configuration.ForceNamePlateStyle)
                {
                    args.Type = PlateType.LowTitleNoFc;
                }

                // set title
                if (this.plugin.Configuration.ChangeNamePlateTitle)
                {
                    // set title by player
                    if (player.SeTitle != null)
                    {
                        args.Title = player.SeTitle;
                    }

                    // set title by category
                    else if (category.IsNamePlateTitleEnabled)
                    {
                        if (category.IsNamePlateTitleEnabled && category.SeName != null)
                        {
                            args.Title = category.SeName;
                        }
                    }
                }

                // set color
                if (this.plugin.Configuration.DisableNamePlateColorIfDead && actor.IsDead()) return;
                if (this.plugin.Configuration.UseNamePlateColors)
                {
                    // set color by player
                    if (player.NamePlateColor != null)
                    {
                        args.Colour = ((Vector4)player.NamePlateColor).ToByteColor();
                    }

                    // set color by category
                    else if (category.IsNamePlateColorEnabled && category.NamePlateColor != null)
                    {
                        args.Colour = ((Vector4)category.NamePlateColor).ToByteColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update nameplate.");
            }
        }
    }
}
