using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// NamePlate Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void NamePlateConfig()
        {
            // warning about compatability
            ImGui.TextColored(ImGuiColors2.ToadYellow, Loc.Localize(
                                  "NamePlateCompatabilityWarning",
                                  "Warning: Not all of these settings will work if you are using other plugins that modify nameplates."));
            ImGui.Spacing();

            // restrict nameplate use
            ImGui.Text(Loc.Localize("ShowNamePlates", "Show Nameplates"));
            var showNamePlatesIndex = this.plugin.Configuration.ShowNamePlates;
            ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo(
                "###PlayerTrack_ShowNamePlates_Combo",
                ref showNamePlatesIndex,
                ContentRestrictionType.RestrictionTypeNames.ToArray(),
                ContentRestrictionType.RestrictionTypeNames.Count))
            {
                this.plugin.Configuration.ShowNamePlates = ContentRestrictionType.GetContentRestrictionTypeByIndex(showNamePlatesIndex).Index;
                this.plugin.SaveConfig();
                this.plugin.PlayerService.ResetViewPlayers();
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowNamePlates_HelpMarker",
                                           "when to show custom nameplates"));
            ImGui.Spacing();

            // restrict in combat
            var restrictNamePlatesInCombat = this.Plugin.Configuration.RestrictNamePlatesInCombat;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictNamePlatesInCombat", "Don't show nameplates in combat") +
                "###PlayerTrack_RestrictNamePlatesInCombat_Checkbox",
                ref restrictNamePlatesInCombat))
            {
                this.Plugin.Configuration.RestrictNamePlatesInCombat = restrictNamePlatesInCombat;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "RestrictNamePlatesInCombat_HelpMarker",
                                           "stop showing nameplates in combat regardless of other settings"));
            ImGui.Spacing();

            // use nameplate colors
            var useNamePlateColors = this.Plugin.Configuration.UseNamePlateColors;
            if (ImGui.Checkbox(
                Loc.Localize($"UseNamePlateColors", "Use nameplate color"),
                ref useNamePlateColors))
            {
                this.Plugin.Configuration.UseNamePlateColors = useNamePlateColors;
                this.Plugin.SaveConfig();
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "UseNamePlateColors_HelpMarker",
                                           "override normal nameplate color with category/player colors"));
            ImGui.Spacing();

            // disable name plate color change if player is dead
            var disableNamePlateColorIfDead = this.Plugin.Configuration.DisableNamePlateColorIfDead;
            if (ImGui.Checkbox(
                Loc.Localize("DisableNamePlateColorIfDead", "Disable nameplate color if player is dead") +
                "###PlayerTrack_DisableNamePlateColorIfDead_Checkbox",
                ref disableNamePlateColorIfDead))
            {
                this.Plugin.Configuration.DisableNamePlateColorIfDead = disableNamePlateColorIfDead;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "DisableNamePlateColorIfDead_HelpMarker",
                                           "don't update nameplate for dead players so easier to see they are dead"));
            ImGui.Spacing();

            // default the nameplate color to the list color unless changed
            var defaultNamePlateColorToListColor = this.Plugin.Configuration.DefaultNamePlateColorToListColor;
            if (ImGui.Checkbox(
                Loc.Localize("DefaultNamePlateColorToListColor", "Default nameplate color to list color") +
                "###PlayerTrack_DefaultNamePlateColorToListColor_Checkbox",
                ref defaultNamePlateColorToListColor))
            {
                this.Plugin.Configuration.DefaultNamePlateColorToListColor = defaultNamePlateColorToListColor;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "DefaultNamePlateColorToListColor_HelpMarker",
                                           "default the nameplate color to the list color unless changed"));
            ImGui.Spacing();

            // use nameplate titles
            var changeNamePlateTitle = this.Plugin.Configuration.ChangeNamePlateTitle;
            if (ImGui.Checkbox(
                Loc.Localize($"ChangeNamePlateTitle", "Change nameplate title"),
                ref changeNamePlateTitle))
            {
                this.Plugin.Configuration.ChangeNamePlateTitle = changeNamePlateTitle;
                this.Plugin.SaveConfig();
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ChangeNamePlateTitle_HelpMarker",
                                           "override normal nameplate to use title or category name"));
            ImGui.Spacing();

            // force nameplate style
            var forceNamePlateStyle = this.Plugin.Configuration.ForceNamePlateStyle;
            if (ImGui.Checkbox(
                Loc.Localize($"ForceNamePlateStyle", "Force consistent nameplate style"),
                ref forceNamePlateStyle))
            {
                this.Plugin.Configuration.ForceNamePlateStyle = forceNamePlateStyle;
                this.Plugin.SaveConfig();
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ForceNamePlateStyle_HelpMarker",
                                           "force LowTitleNoFc nameplate style which looks more " +
                                           "consistent \nbut hides FC and doesn't play nicely with other plugins"));
            ImGui.Spacing();
        }
    }
}
