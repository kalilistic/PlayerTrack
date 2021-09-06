using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Add Player View.
    /// </summary>
    public partial class Panel
    {
        private readonly string[] worldNames;
        private int selectedWorld;
        private string addPlayerInput = string.Empty;
        private bool showInvalidNameError;
        private bool showDuplicatePlayerError;

        private void AddPlayer()
        {
            WindowManager.SpacerNoTabs();
            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("AddPlayerModalContent", "Add player manually to your list."));
            ImGui.Spacing();
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            ImGui.Combo(
                "###PlayerTrack_PlayerAdd_Combo",
                ref this.selectedWorld,
                this.worldNames,
                this.worldNames.Length);
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            ImGui.InputTextWithHint(
                "###PlayerTrack_PlayerNameAdd_Input",
                Loc.Localize("PlayerNameAddHint", "player name"),
                ref this.addPlayerInput,
                30);
            ImGui.Spacing();
            if (ImGui.Button(Loc.Localize("AddPlayerModalButton", "Add") + "###PlayerTrack_AddPlayerModalOK_Button"))
            {
                this.showInvalidNameError = false;
                this.showDuplicatePlayerError = false;
                if (this.addPlayerInput.IsValidCharacterName())
                {
                    var existingPlayer =
                        this.plugin.PlayerService.GetPlayer(this.addPlayerInput, this.worldNames[this.selectedWorld]);
                    if (existingPlayer != null)
                    {
                        this.showDuplicatePlayerError = true;
                    }
                    else
                    {
                        var player = this.plugin.PlayerService.AddPlayer(this.addPlayerInput, this.worldNames[this.selectedWorld]);
                        this.addPlayerInput = string.Empty;
                        this.SelectedPlayer = player;
                        this.plugin.Configuration.CurrentView = View.PlayerDetail;
                    }
                }
                else
                {
                    this.showInvalidNameError = true;
                }
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("Cancel", "Cancel") +
                             "###PlayerTrack_AddPlayerModalCancel_Button"))
            {
                this.addPlayerInput = string.Empty;
                this.showInvalidNameError = false;
                this.showDuplicatePlayerError = false;

                // this.HideRightPanel(); TODO
            }

            ImGui.Spacing();
            if (this.showInvalidNameError)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, Loc.Localize("InvalidPlayerName", "Not a valid player name - try again."));
            }
            else if (this.showDuplicatePlayerError)
            {
                ImGui.TextColored(ImGuiColors.DPSRed, Loc.Localize("DuplicatePlayer", "This player already exists in your list!"));
            }
        }
    }
}
