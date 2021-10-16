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
        }
    }
}
