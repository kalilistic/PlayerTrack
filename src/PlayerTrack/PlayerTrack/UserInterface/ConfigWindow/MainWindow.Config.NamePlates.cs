using CheapLoc;
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
                this.plugin.PlayerService.UpdateViewPlayers();
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowNamePlates_HelpMarker",
                                           "when to show custom nameplates"));
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
