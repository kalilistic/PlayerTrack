using System.Collections.Generic;
using System.Linq;

using CheapLoc;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using XivCommon.Functions.ContextMenu;

namespace PlayerTrack
{
    /// <summary>
    /// Manage custom context menu on players.
    /// </summary>
    public class ContextMenuManager
    {
        private readonly PlayerTrackPlugin plugin;
        private readonly NormalContextMenuItem addShowMenuItem;
        private readonly NormalContextMenuItem openLodestoneMenuItem;
        private readonly NormalContextSubMenuItem contextSubMenuItem;

        private Player? selectedPlayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public ContextMenuManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.plugin.XivCommon.Functions.ContextMenu.OpenContextMenu += this.OnOpenContextMenu;
            this.addShowMenuItem = new NormalContextMenuItem(
                new SeString(new Payload[]
                {
                    new TextPayload(Loc.Localize("SubMenuShowPlayerItem", "Add/Show Info")),
                }), this.OnOpenPlayerInfo);
            this.openLodestoneMenuItem = new NormalContextMenuItem(
                new SeString(new Payload[]
                {
                new TextPayload(Loc.Localize("SubMenuOpenLodestoneItem", "Open Lodestone")),
                }), this.OnOpenLodestoneProfile);
            this.contextSubMenuItem =
                new NormalContextSubMenuItem(Loc.Localize("SetCategoryContextMenu", "Set Category"), this.OnOpenCategoryMenu);
        }

        /// <summary>
        /// Dispose context menu manager.
        /// </summary>
        public void Dispose()
        {
            this.plugin.XivCommon.Functions.ContextMenu.OpenContextMenu -= this.OnOpenContextMenu;
        }

        private static bool IsMenuValid(BaseContextMenuArgs args)
        {
            switch (args.ParentAddonName)
            {
                case null:
                case "LookingForGroup":
                case "PartyMemberList":
                case "FriendList":
                case "FreeCompany":
                case "SocialList":
                case "ContactList":
                case "ChatLog":
                case "_PartyList":
                case "LinkShell":
                case "CrossWorldLinkshell":
                case "ContentMemberList":
                case "BlackList":
                    return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

                default:
                    return false;
            }
        }

        private static int? GetActionIndex(ICollection<byte> configActionIds, IReadOnlyList<byte> currentActionIds)
        {
            for (var i = 0; i < currentActionIds.Count; i++)
            {
                if (configActionIds.Contains(currentActionIds[i]))
                {
                    return i;
                }
            }

            return null;
        }

        private void OnOpenContextMenu(ContextMenuOpenArgs args)
        {
            // check if plugin started
            if (!this.plugin.IsDoneLoading) return;

            // hide on own player
            if (args.ObjectId == PlayerTrackPlugin.ClientState.LocalPlayer!.ObjectId) return;

            // validate menu
            if (!IsMenuValid(args)) return;

            // set player if existing
            this.selectedPlayer = this.plugin.PlayerService.GetPlayer(args.Text!.ToString(), args.ObjectWorld);

            // setup
            var index = 0;
            var actionIds = args.Items.Select(baseContextMenuItem => ((NativeContextMenuItem)baseContextMenuItem).InternalAction).ToList();

            // default
            if (args.ParentAddonName == "FriendList" || (this.plugin.Configuration.ShowContextAboveThis.Count == 0 &&
                this.plugin.Configuration.ShowContextBelowThis.Count == 0))
            {
                if (this.plugin.Configuration.ShowAddShowInfoContextMenu)
                {
                    args.Items.Add(this.addShowMenuItem);
                }

                if (this.plugin.Configuration.ShowOpenLodestoneContextMenu && this.selectedPlayer?.LodestoneStatus == LodestoneStatus.Verified)
                {
                    args.Items.Add(this.openLodestoneMenuItem);
                }

                if (this.plugin.Configuration.ShowSetCategoryContextMenu)
                {
                    args.Items.Add(this.contextSubMenuItem);
                }

                return;
            }

            // get show above index
            var relativeAboveIndex = GetActionIndex(this.plugin.Configuration.ShowContextAboveThis, actionIds);
            if (relativeAboveIndex != null)
            {
                index = (int)relativeAboveIndex;
                actionIds.RemoveRange(index, actionIds.Count - index);
            }

            // get show below index
            var relativeBelowIndex = GetActionIndex(this.plugin.Configuration.ShowContextBelowThis, actionIds);
            if (relativeBelowIndex != null)
            {
                index = (int)relativeBelowIndex + 1;
            }

            // default to bottom if nothing found
            if (relativeAboveIndex == null && relativeBelowIndex == null)
            {
                index = args.Items.Count;
            }

            // // insert menu options
            if (this.plugin.Configuration.ShowSetCategoryContextMenu)
            {
                args.Items.Insert(index, this.contextSubMenuItem);
            }

            if (this.plugin.Configuration.ShowOpenLodestoneContextMenu && this.selectedPlayer?.LodestoneStatus == LodestoneStatus.Verified)
            {
                args.Items.Insert(index, this.openLodestoneMenuItem);
            }

            if (this.plugin.Configuration.ShowAddShowInfoContextMenu)
            {
                args.Items.Insert(index, this.addShowMenuItem);
            }
        }

        private void OnOpenLodestoneProfile(ContextMenuItemSelectedArgs args)
        {
            this.plugin.LodestoneService.OpenLodestoneProfile(this.selectedPlayer!.LodestoneId);
        }

        private void OnOpenPlayerInfo(ContextMenuItemSelectedArgs args)
        {
            // get player or add if doesn't exist
            this.selectedPlayer ??= this.plugin.PlayerService.AddPlayer(args.Text!.ToString(), args.ObjectWorld);

            // open in detailed view
            this.plugin.WindowManager.Panel!.SelectedPlayer = null;
            this.plugin.WindowManager.Panel!.SelectedPlayer = this.selectedPlayer;
            this.plugin.WindowManager.Panel!.SelectedEncounters = this.plugin.EncounterService
                                                                           .GetEncountersByPlayer(this.selectedPlayer.Key)
                                                                           .OrderByDescending(enc => enc.Created).ToList();
            this.plugin.WindowManager.MainWindow!.IsOpen = true;
            this.plugin.WindowManager.Panel!.ShowPanel(View.PlayerDetail);
        }

        private void OnOpenCategoryMenu(ContextMenuOpenArgs args)
        {
            var categoryNames = this.plugin.CategoryService.GetCategoryNames().Reverse().ToArray();
            var categoryIds = this.plugin.CategoryService.GetCategoryIds().Reverse().ToArray();
            for (var i = 0; i < categoryIds.Length; i++)
            {
                var index = i;
                args.Items.Insert(i, new NormalContextMenuItem(
                  new SeString(new TextPayload(categoryNames[i])), _ =>
                  {
                      var player = this.selectedPlayer;
                      if (player != null)
                      {
                          player.CategoryId = categoryIds[index];
                          this.plugin.PlayerService.UpdatePlayerCategory(player);
                      }

                      this.plugin.NamePlateManager.ForceRedraw();
                  }));
            }
        }
    }
}
