using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Processing Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void ProcessingConfig()
        {
            // restrict in combat
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

            // add / update players
            ImGui.Text(Loc.Localize("RestrictAddUpdatePlayers", "Add / Update Players"));
            var restrictAddUpdatePlayersIndex = this.plugin.Configuration.RestrictAddUpdatePlayers;
            ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo(
                "###PlayerTrack_RestrictAddUpdatePlayers_Combo",
                ref restrictAddUpdatePlayersIndex,
                ContentRestrictionType.RestrictionTypeNames.ToArray(),
                ContentRestrictionType.RestrictionTypeNames.Count))
            {
                this.plugin.Configuration.RestrictAddUpdatePlayers = ContentRestrictionType.GetContentRestrictionTypeByIndex(restrictAddUpdatePlayersIndex).Index;
                this.plugin.SaveConfig();
                this.plugin.PlayerService.ResetViewPlayers();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictAddUpdatePlayers_HelpMarker",
                                           "when to process players"));
            ImGui.Spacing();

            // add encounters
            ImGui.Text(Loc.Localize("RestrictAddEncounters", "Add New Encounters"));
            var restrictAddEncountersIndex = this.plugin.Configuration.RestrictAddEncounters;
            ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo(
                "###PlayerTrack_RestrictAddEncounters_Combo",
                ref restrictAddEncountersIndex,
                ContentRestrictionType.RestrictionTypeNames.ToArray(),
                ContentRestrictionType.RestrictionTypeNames.Count))
            {
                this.plugin.Configuration.RestrictAddEncounters = ContentRestrictionType.GetContentRestrictionTypeByIndex(restrictAddEncountersIndex).Index;
                this.plugin.SaveConfig();
                this.plugin.PlayerService.ResetViewPlayers();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictAddEncounters_HelpMarker",
                                           "when to process new encounters (don't use always)"));
            ImGui.Spacing();
        }
    }
}
