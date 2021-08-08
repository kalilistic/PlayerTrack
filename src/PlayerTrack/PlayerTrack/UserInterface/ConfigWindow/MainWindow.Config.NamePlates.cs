using CheapLoc;
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
            var useNamePlateColors = this.Plugin.Configuration.UseNamePlateColors;
            if (ImGui.Checkbox(
                Loc.Localize($"UseNamePlateColors", "Use nameplate color"),
                ref useNamePlateColors))
            {
                this.Plugin.Configuration.UseNamePlateColors = useNamePlateColors;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "UseNamePlateColors_HelpMarker",
                                           "override normal nameplate color with category/player colors"));
            ImGui.Spacing();

            var changeNamePlateTitle = this.Plugin.Configuration.ChangeNamePlateTitle;
            if (ImGui.Checkbox(
                Loc.Localize($"ChangeNamePlateTitle", "Change nameplate title"),
                ref changeNamePlateTitle))
            {
                this.Plugin.Configuration.ChangeNamePlateTitle = changeNamePlateTitle;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ChangeNamePlateTitle_HelpMarker",
                                           "override normal nameplate to use title or category name"));
            ImGui.Spacing();
        }
    }
}
