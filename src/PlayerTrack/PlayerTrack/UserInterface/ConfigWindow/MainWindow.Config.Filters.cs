using CheapLoc;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Filter Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void FiltersConfig()
        {
            var restrictInCombat = this.Plugin.Configuration.RestrictInCombat;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictInCombat", "Don't process in combat") +
                "###PlayerTrack_RestrictInCombat_Checkbox",
                ref restrictInCombat))
            {
                this.Plugin.Configuration.RestrictInCombat = restrictInCombat;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictInCombat_HelpMarker",
                                           "stop processing data while in combat"));
            ImGui.Spacing();

            var restrictEncountersToContent = this.Plugin.Configuration.RestrictEncountersToContent;
            if (ImGui.Checkbox(
                Loc.Localize("OnlyCreateEncountersInContent", "Create Encounters in Content Only") +
                "###PlayerTrack_OnlyCreateEncountersInContent_Checkbox",
                ref restrictEncountersToContent))
            {
                this.Plugin.Configuration.RestrictEncountersToContent = restrictEncountersToContent;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "OnlyCreateEncountersInContent_HelpMarker",
                                           "only create new encounters in content rather than open world"));
            ImGui.Spacing();

            var restrictToContent = this.Plugin.Configuration.RestrictToContent;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictToContent", "Content Only") + "###PlayerTrack_RestrictToContent_Checkbox",
                ref restrictToContent))
            {
                this.Plugin.Configuration.RestrictToContent = restrictToContent;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictToContent_HelpMarker",
                                           "restrict to instanced content and exclude overworld encounters"));
            ImGui.Spacing();

            var restrictToHighEndDuty = this.Plugin.Configuration.RestrictToHighEndDuty;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictToHighEndDuty", "High-End Duty Only") +
                "###PlayerTrack_RestrictToHighEndDuty_Checkbox",
                ref restrictToHighEndDuty))
            {
                this.Plugin.Configuration.RestrictToHighEndDuty = restrictToHighEndDuty;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictToHighEndDuty_HelpMarker",
                                           "restrict to high-end duties only (e.g. savage)"));
            ImGui.Spacing();
        }
    }
}
