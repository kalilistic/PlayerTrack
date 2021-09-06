using System;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
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

        private const ImGuiWindowFlags ModalFlags = ImGuiWindowFlags.NoCollapse;
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

            /// <summary>
            /// Icon glossary to view what's available.
            /// </summary>
            IconGlossary,
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

                        this.plugin.WindowManager.Panel!.HidePanel();
                        this.plugin.PlayerService.DeletePlayer(this.Player!);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Localize("Cancel", "Cancel") +
                                     "###PlayerTrack_DeleteConfirmationModalCancel_Button"))
                    {
                        this.IsOpen = false;
                    }

                    break;
                case ModalType.IconGlossary:
                    this.WindowName = Loc.Localize("IconGlossaryModalTitle", "Icon Glossary") +
                                      "###PlayerTrack_IconGlossaryModal_Window";
                    var icons = IconHelper.Icons;
                    var iconNames = IconHelper.IconNames;
                    for (var i = 0; i < icons.Length; i++)
                    {
                        ImGui.BeginGroup();
                        if (this.Plugin.Configuration.EnabledIcons.Contains(icons[i]))
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors2.ToadViolet);
                        }

                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.Text(icons[i].ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.Text(iconNames[i]);
                        if (this.Plugin.Configuration.EnabledIcons.Contains(icons[i]))
                        {
                            ImGui.PopStyleColor();
                        }

                        ImGui.EndGroup();
                        if (ImGui.IsItemClicked())
                        {
                            if (this.Plugin.Configuration.EnabledIcons.Contains(icons[i]))
                            {
                                this.Plugin.Configuration.EnabledIcons.Remove(icons[i]);
                                this.Plugin.SaveConfig();
                            }
                            else
                            {
                                this.Plugin.Configuration.EnabledIcons.Add(IconHelper.Icons[i]);
                                this.Plugin.SaveConfig();
                            }
                        }
                    }

                    ImGui.Spacing();
                    if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_IconGlossaryModalOK_Button"))
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
