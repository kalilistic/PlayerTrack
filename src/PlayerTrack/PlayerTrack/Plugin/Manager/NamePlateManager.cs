using System;
using System.Numerics;

using Dalamud.DrunkenToad;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
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
                    PlayerTrackPlugin.Condition.InCombat())
                {
                    return;
                }

                var restrict =
                    ContentRestrictionType.GetContentRestrictionTypeByIndex(this.plugin.Configuration.ShowNamePlates);
                if (restrict == ContentRestrictionType.Never ||
                    !(restrict == ContentRestrictionType.Always ||
                    (restrict == ContentRestrictionType.ContentOnly && PlayerTrackPlugin.DataManager.InContent(PlayerTrackPlugin.ClientState.TerritoryType)) ||
                    (restrict == ContentRestrictionType.HighEndDutyOnly && PlayerTrackPlugin.DataManager.InHighEndDuty(PlayerTrackPlugin.ClientState.TerritoryType))))
                {
                    return;
                }

                // check if any nameplate features enabled
                if (!this.plugin.Configuration.ChangeNamePlateTitle &&
                    !this.plugin.Configuration.UseNamePlateColors) return;

                // check if plugin started
                if (!this.plugin.IsDoneLoading) return;

                // check if valid
                if (args.Type != PlateType.Player || args.ObjectId == 0) return;

                // get actor
                var actorGameObject = PlayerTrackPlugin.ObjectTable.SearchById(args.ObjectId);
                if (actorGameObject == null || actorGameObject is not PlayerCharacter actorPlayer) return;

                var player = this.plugin.PlayerService.GetPlayer(actorPlayer.Name.ToString(), (ushort)actorPlayer.HomeWorld.Id);
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
                if (this.plugin.Configuration.DisableNamePlateColorIfDead && actorPlayer.IsDead()) return;
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
