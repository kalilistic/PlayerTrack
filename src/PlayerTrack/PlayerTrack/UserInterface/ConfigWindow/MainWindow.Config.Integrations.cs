using System;

using CheapLoc;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Lodestone Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void IntegrationsConfig()
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Lodestone", "Lodestone"));
            var syncToLodestone = this.Plugin.Configuration.SyncToLodestone;
            if (ImGui.Checkbox(
                Loc.Localize($"SyncToLodestone", "Sync to Lodestone"),
                ref syncToLodestone))
            {
                this.Plugin.Configuration.SyncToLodestone = syncToLodestone;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SyncToLodestone_HelpMarker",
                                           "pull player data from lodestone to track name/world changes"));
            ImGui.Spacing();

            ImGui.Text(Loc.Localize("LodestoneLocale", "Lodestone Locale"));
            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "LodestoneLocale_HelpMarker",
                                           "set locale for lodestone profile link"));
            ImGui.Spacing();
            var lodestoneLocale = (int)this.Plugin.Configuration.LodestoneLocale;
            if (ImGui.Combo(
                "###PlayerTrack_LodestoneLocale_Combo",
                ref lodestoneLocale,
                Enum.GetNames(typeof(LodestoneLocale)),
                Enum.GetNames(typeof(LodestoneLocale)).Length))
            {
                this.Plugin.Configuration.LodestoneLocale = (LodestoneLocale)lodestoneLocale;
                this.Plugin.SaveConfig();
            }

            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("Visibility", "Visibility"));
            var syncWithVisibility = this.Plugin.Configuration.SyncWithVisibility;
            if (ImGui.Checkbox(
                Loc.Localize($"SyncWithVisibility", "Sync with Visibility"),
                ref syncWithVisibility))
            {
                this.Plugin.Configuration.SyncWithVisibility = syncWithVisibility;
                this.Plugin.SaveConfig();
                if (syncWithVisibility)
                {
                    this.plugin.VisibilityService.SyncWithVisibility();
                }
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SyncWithVisibility_HelpMarker",
                                           "synchronize with visibility plugin"));
            ImGui.Spacing();

            var showHiddenPlayersInList = this.Plugin.Configuration.ShowVoidedPlayersInList;
            if (ImGui.Checkbox(
                Loc.Localize($"ShowHiddenPlayersInList", "Show hidden players in list"),
                ref showHiddenPlayersInList))
            {
                this.Plugin.Configuration.ShowVoidedPlayersInList = showHiddenPlayersInList;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowHiddenPlayersInList_HelpMarker",
                                           "toggle showing players hidden with visibility in list"));
            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors.DalamudViolet, Loc.Localize("FCNameColor", "FCNameColor"));
            var syncWithFCNameColor = this.Plugin.Configuration.SyncWithFCNameColor;
            if (ImGui.Checkbox(
                Loc.Localize($"SyncWithFCNameColor", "Sync with FCNameColor"),
                ref syncWithFCNameColor))
            {
                this.Plugin.Configuration.SyncWithFCNameColor = syncWithFCNameColor;
                this.Plugin.SaveConfig();
                if (syncWithFCNameColor)
                {
                    this.plugin.FCNameColorService.SyncWithFCNameColor();
                }
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SyncWithFCNameColor_HelpMarker",
                                           "synchronize with FCNameColor plugin"));
            ImGui.Spacing();

            var createDynamicFCCategories = this.Plugin.Configuration.CreateDynamicFCCategories;
            if (ImGui.Checkbox(
                Loc.Localize($"CreateDynamicFCCategories", "Create dynamic FC categories"),
                ref createDynamicFCCategories))
            {
                this.Plugin.Configuration.CreateDynamicFCCategories = createDynamicFCCategories;
                this.Plugin.SaveConfig();
                if (syncWithFCNameColor && createDynamicFCCategories)
                {
                    this.plugin.FCNameColorService.SyncWithFCNameColor();
                }
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "CreateDynamicFCCategories_HelpMarker",
                                           "create categories that automatically add/remove players based on current FC roster"));

            ImGui.Spacing();

            var reassignPlayersFromExistingCategory = this.Plugin.Configuration.ReassignPlayersFromExistingCategory;
            if (ImGui.Checkbox(
                Loc.Localize($"ReassignPlayersFromExistingCategory", "Reassign players from existing categories"),
                ref reassignPlayersFromExistingCategory))
            {
                this.Plugin.Configuration.ReassignPlayersFromExistingCategory = reassignPlayersFromExistingCategory;
                this.Plugin.SaveConfig();
                if (syncWithFCNameColor && createDynamicFCCategories && reassignPlayersFromExistingCategory)
                {
                    this.plugin.FCNameColorService.SyncWithFCNameColor();
                }
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ReassignPlayersFromExistingCategory_HelpMarker",
                                           "reassign players from existing category assignments into fc category and not just default category"));
        }
    }
}
