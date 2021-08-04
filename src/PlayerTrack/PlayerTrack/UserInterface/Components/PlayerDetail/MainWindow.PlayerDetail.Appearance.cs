using System;
using System.Windows.Forms;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Detail Appearance View.
    /// </summary>
    public partial class MainWindow
    {
        private void PlayerCustomize()
        {
            if (this.SelectedPlayer == null) return;
            const float sameLineOffset = 70f;

            if (this.SelectedPlayer.Customize != null)
            {
                ImGui.Text(Loc.Localize("Gender", "Gender"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
                ImGui.Text(this.plugin.PluginService.GameData.GenderName(this.SelectedPlayer.CharaCustomizeData.Gender));

                ImGui.Text(Loc.Localize("Race", "Race"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
                ImGui.Text(this.plugin.PluginService.GameData.RaceName(this.SelectedPlayer.CharaCustomizeData.Race, this.SelectedPlayer.CharaCustomizeData.Gender));

                ImGui.Text(Loc.Localize("Tribe", "Tribe"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
                ImGui.Text(this.plugin.PluginService.GameData.TribeName(this.SelectedPlayer.CharaCustomizeData.Tribe, this.SelectedPlayer.CharaCustomizeData.Gender));

                ImGui.Text(Loc.Localize("Height", "Height"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
                ImGui.Text(string.Format(
                               Loc.Localize("PlayerHeightValue", "{0} in"),
                               CharHeightUtil.CalcInches(this.SelectedPlayer.CharaCustomizeData.Height, this.SelectedPlayer.CharaCustomizeData.Race, this.SelectedPlayer.CharaCustomizeData.Tribe, this.SelectedPlayer.CharaCustomizeData.Gender)));

                ImGuiHelpers.ScaledDummy(5f);
                if (this.SelectedPlayer.Customize is { Length: > 0 })
                {
                    if (ImGui.Button(Loc.Localize("Copy", "Copy") + "###PlayerTrack_PlayerAppearanceCopy_Button"))
                    {
                        Clipboard.SetText(BitConverter.ToString(this.SelectedPlayer.Customize));
                    }
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors2.ToadYellow, Loc.Localize("NoAppearance", "No appearance data found for this player."));
                ImGui.TextWrapped(Loc.Localize(
                                      "NoAppearanceExplanation",
                                      "This can happen for characters manually added or if the player was migrated from an earlier version of the plugin. " +
                                      "Next time you encounter this player, the appearance data will be refreshed."));
            }
        }
    }
}
