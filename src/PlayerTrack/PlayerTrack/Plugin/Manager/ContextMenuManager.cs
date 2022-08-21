using System.Linq;

using CheapLoc;
using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace PlayerTrack
{
    /// <summary>
    /// Manage custom context menu on players.
    /// </summary>
    public class ContextMenuManager
    {
        private readonly PlayerTrackPlugin plugin;
        private readonly GameObjectContextMenuItem addShowMenuItem;
        private readonly GameObjectContextMenuItem openLodestoneMenuItem;

        private Player? selectedPlayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuManager"/> class.
        /// </summary>
        /// <param name="plugin">plugin.</param>
        public ContextMenuManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.plugin.ContextMenu.OnOpenGameObjectContextMenu += this.OpenGameObjectContextMenu;
            this.addShowMenuItem = new GameObjectContextMenuItem(
                new SeString(new TextPayload(Loc.Localize("SubMenuShowPlayerItem", "Add/Show Info"))), this.OnOpenPlayerInfo);
            this.openLodestoneMenuItem = new GameObjectContextMenuItem(
                new SeString(new TextPayload(Loc.Localize("SubMenuOpenLodestoneItem", "Open Lodestone"))), this.OnOpenLodestoneProfile);
        }

        /// <summary>
        /// Dispose context menu manager.
        /// </summary>
        public void Dispose()
        {
            this.plugin.ContextMenu.OnOpenGameObjectContextMenu -= this.OpenGameObjectContextMenu;
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

        private void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
        {
            // check if plugin started
            if (!this.plugin.IsDoneLoading) return;

            // hide on own player
            if (args.ObjectId == PlayerTrackPlugin.ClientState.LocalPlayer!.ObjectId) return;

            // validate menu
            if (!IsMenuValid(args)) return;

            // set player if existing
            this.selectedPlayer = this.plugin.PlayerService.GetPlayer(args.Text!.ToString(), args.ObjectWorld);

            if (this.plugin.Configuration.ShowAddShowInfoContextMenu)
            {
                args.AddCustomItem(this.addShowMenuItem);
            }

            if (this.plugin.Configuration.ShowOpenLodestoneContextMenu && this.selectedPlayer?.LodestoneStatus == LodestoneStatus.Verified)
            {
                args.AddCustomItem(this.openLodestoneMenuItem);
            }
        }

        private void OnOpenLodestoneProfile(GameObjectContextMenuItemSelectedArgs args)
        {
            this.plugin.LodestoneService.OpenLodestoneProfile(this.selectedPlayer!.LodestoneId);
        }

        private void OnOpenPlayerInfo(GameObjectContextMenuItemSelectedArgs args)
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
    }
}
