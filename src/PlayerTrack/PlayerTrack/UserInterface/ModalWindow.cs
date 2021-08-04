using System;
using System.Numerics;

using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Delete modal for the plugin.
    /// </summary>
    public class ModalWindow : PluginWindow
    {
        /// <summary>
        /// Current player.
        /// </summary>
        public Player? Player;

        private const ImGuiWindowFlags ModalFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse;
        private readonly PlayerTrackPlugin plugin;
        private ModalType currentModalType = ModalType.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModalWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public ModalWindow(PlayerTrackPlugin plugin)
            : base(plugin, "ModalWindow", ModalFlags)
        {
            this.plugin = plugin;
            this.Position = new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f);
            this.PositionCondition = ImGuiCond.Appearing;
        }

        /// <summary>
        /// Modal Types.
        /// </summary>
        public enum ModalType
        {
            /// <summary>
            /// No Modal.
            /// </summary>
            None,

            /// <summary>
            /// Confirmation to delete player.
            /// </summary>
            ConfirmDelete,
        }

        /// <summary>
        /// Open modal.
        /// </summary>
        /// <param name="modalType">type of modal to open.</param>
        public void Open(ModalType modalType)
        {
            this.currentModalType = modalType;
            this.IsOpen = true;
        }

        /// <summary>
        /// Open modal with player.
        /// </summary>
        /// <param name="modalType">type of modal to open.</param>
        /// <param name="player">player to act upon.</param>
        public void Open(ModalType modalType, Player player)
        {
            this.currentModalType = modalType;
            this.Player = player;
            this.IsOpen = true;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            switch (this.currentModalType)
            {
                case ModalType.None:
                    break;
                case ModalType.ConfirmDelete:
                    this.WindowName = Loc.Localize("DeleteConfirmationModalTitle", "Delete") +
                                      "###PlayerTrack_DeleteConfirmationModal_Window";
                    ImGui.Text(Loc.Localize("DeleteConfirmationModalContent", "Are you sure you want to delete?"));
                    ImGui.Spacing();
                    if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_DeleteConfirmationModalOK_Button"))
                    {
                        this.IsOpen = false;

                        this.plugin.WindowManager.MainWindow!.HideRightPanel();
                        this.plugin.PlayerService.DeletePlayer(this.Player!);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Localize("Cancel", "Cancel") +
                                     "###PlayerTrack_DeleteConfirmationModalCancel_Button"))
                    {
                        this.IsOpen = false;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
