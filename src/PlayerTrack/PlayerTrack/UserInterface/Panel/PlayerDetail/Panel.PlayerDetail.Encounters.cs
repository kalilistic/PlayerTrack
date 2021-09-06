using System.Linq;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Detail Encounters View.
    /// </summary>
    public partial class Panel
    {
        private void PlayerEncounters()
        {
            if (this.SelectedPlayer == null) return;
            const float sameLineOffset1 = 60f;
            const float sameLineOffset2 = 130f;
            const float sameLineOffset3 = 170f;
            const float sameLineOffset4 = 200f;

            if (this.SelectedEncounters != null && this.SelectedEncounters.Any())
            {
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Time", "Time"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Duration", "Duration"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Job", "Job"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Lvl", "Lvl"));
                ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset4);
                ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Location", "Location"));

                foreach (var encounter in this.SelectedEncounters)
                {
                    ImGui.BeginGroup();
                    ImGui.Text(encounter.Created.ToTimeSpan());
                    ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset1);
                    ImGui.Text((encounter.Updated - encounter.Created).ToDuration());
                    ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset2);
                    ImGui.Text(encounter.JobCode);
                    ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset3);
                    ImGui.Text(encounter.JobLvl.ToString());
                    ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset4);
                    ImGui.Text(encounter.LocationName);
                    ImGui.EndGroup();
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup("###PlayerTrack_Encounter_Popup_" + encounter.Id);
                    }

                    if (ImGui.BeginPopup("###PlayerTrack_Encounter_Popup_" + encounter.Id))
                    {
                        ImGui.Text(Loc.Localize("Delete", "Delete"));
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            this.plugin.EncounterService.DeleteEncounter(encounter);
                            this.SelectedEncounters = this.plugin.EncounterService.GetEncountersByPlayer(this.SelectedPlayer.Key).OrderByDescending(enc => enc.Created).ToList();
                        }

                        ImGui.EndPopup();
                    }
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors2.ToadYellow, Loc.Localize("NoEncounters", "No encounters found for this player."));
                ImGui.TextWrapped(Loc.Localize(
                                      "NoEncountersExplanation",
                                      "This can happen for characters manually added or if all the encounters have been deleted."));
            }
        }
    }
}
