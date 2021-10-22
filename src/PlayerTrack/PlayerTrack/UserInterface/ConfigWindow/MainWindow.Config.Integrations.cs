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
        }
    }
}
