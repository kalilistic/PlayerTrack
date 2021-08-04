using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Settings View.
    /// </summary>
    public partial class MainWindow
    {
        private void ShowSettings()
        {
            if (ImGui.BeginTabBar("###PlayerTrack_Settings_TabBar", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem(Loc.Localize("DisplaySettings", "Display")))
                {
                    SpacerWithTabs();
                    this.DisplaySettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("RestrictionSettings", "Restrictions")))
                {
                    SpacerWithTabs();
                    this.RestrictionSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("IconSettings", "Icons")))
                {
                    SpacerWithTabs();
                    this.IconSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("NamePlateSettings", "NamePlates")))
                {
                    SpacerWithTabs();
                    this.NamePlateSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("ContextMenuSettings", "ContextMenu")))
                {
                    SpacerWithTabs();
                    this.ContextMenuSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("LodestoneSettings", "Lodestone")))
                {
                    SpacerWithTabs();
                    this.LodestoneSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("CategorySettings", "Categories")))
                {
                    SpacerWithTabs();
                    this.CategorySettings();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Spacing();
        }
    }
}
