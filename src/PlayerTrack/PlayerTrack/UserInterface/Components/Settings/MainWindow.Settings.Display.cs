using System;
using System.Linq;

using CheapLoc;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Display Settings.
    /// </summary>
    public partial class MainWindow
    {
        private void DisplaySettings()
        {
            ImGui.Text(Loc.Localize("ListMode", "List Mode"));
            var listMode = (int)this.plugin.Configuration.ListMode;
            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
            if (ImGui.Combo(
                "###PlayerTrack_ListMode_Combo",
                ref listMode,
                Enum.GetNames(typeof(PlayerListMode)),
                Enum.GetNames(typeof(PlayerListMode)).Length))
            {
                this.plugin.Configuration.ListMode = (PlayerListMode)listMode;
                this.plugin.SaveConfig();
            }

            ImGui.Spacing();
            ImGui.Text(Loc.Localize("CategoryFilter", "Category Filter"));
            var categoryNames = new[] { Loc.Localize("None", "None") };
            categoryNames = categoryNames.Concat(this.plugin.CategoryService.GetCategoryNames()).ToArray();
            var categoryFilterId = this.plugin.Configuration.CategoryFilterId;
            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
            if (ImGui.Combo(
                "###PlayerTrack_CategoryFilter_Combo",
                ref categoryFilterId,
                categoryNames,
                categoryNames.Length))
            {
                this.plugin.Configuration.CategoryFilterId = categoryFilterId;
                this.plugin.SaveConfig();
            }

            ImGui.Spacing();
            ImGui.Text(Loc.Localize("SearchType", "Search Type"));
            var searchType = (int)this.plugin.Configuration.SearchType;
            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
            if (ImGui.Combo(
                "###PlayerTrack_SearchType_Combo",
                ref searchType,
                Enum.GetNames(typeof(PlayerSearchType)),
                Enum.GetNames(typeof(PlayerSearchType)).Length))
            {
                this.plugin.Configuration.SearchType = (PlayerSearchType)searchType;
                this.plugin.SaveConfig();
            }
        }
    }
}
