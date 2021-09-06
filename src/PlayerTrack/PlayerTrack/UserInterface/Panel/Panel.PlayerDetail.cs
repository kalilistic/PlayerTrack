using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Detail View.
    /// </summary>
    public partial class Panel
    {
        private void PlayerDetail()
        {
            if (this.plugin.Configuration.CurrentView == View.PlayerDetail)
            {
                if (this.SelectedPlayer == null) return;
                if (ImGui.BeginTabBar("###PlayerTrack_PlayerDetail_TabBar", ImGuiTabBarFlags.None))
                {
                    if (ImGui.BeginTabItem(Loc.Localize("Summary", "Summary")))
                    {
                        WindowManager.SpacerWithTabs();
                        this.PlayerSummary();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Loc.Localize("Encounters", "Encounters")))
                    {
                        WindowManager.SpacerWithTabs();
                        this.PlayerEncounters();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Loc.Localize("Appearance", "Appearance")))
                    {
                        WindowManager.SpacerWithTabs();
                        this.PlayerCustomize();
                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem(Loc.Localize("Display", "Display")))
                    {
                        WindowManager.SpacerWithTabs();
                        this.PlayerDisplay();
                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
        }
    }
}
