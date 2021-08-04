using CheapLoc;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// NamePlate Settings.
    /// </summary>
    public partial class MainWindow
    {
        private void NamePlateSettings()
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

            var changeNamePlateTitleToCategory = this.Plugin.Configuration.ChangeNamePlateTitleToCategory;
            if (ImGui.Checkbox(
                Loc.Localize($"ChangeNamePlateTitleToCategory", "Change nameplate title to category"),
                ref changeNamePlateTitleToCategory))
            {
                this.Plugin.Configuration.ChangeNamePlateTitleToCategory = changeNamePlateTitleToCategory;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ChangeNamePlateTitleToCategory_HelpMarker",
                                           "override normal nameplate title to use category name (if not default)"));
            ImGui.Spacing();
        }
    }
}
