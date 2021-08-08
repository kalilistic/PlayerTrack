using System;

using CheapLoc;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Filter Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void DisplayConfig()
        {
            // show filter type
            var showFilterType = this.Plugin.Configuration.ShowFilterType;
            if (ImGui.Checkbox(
                Loc.Localize("ShowFilterType", "Show filter type") +
                "###PlayerTrack_ShowFilterType_Checkbox",
                ref showFilterType))
            {
                this.Plugin.Configuration.ShowFilterType = showFilterType;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowFilterType_HelpMarker",
                                           "toggle to show filter type on player list"));
            ImGui.Spacing();

            // show search box
            var showSearchBox = this.Plugin.Configuration.ShowSearchBox;
            if (ImGui.Checkbox(
                Loc.Localize("ShowSearchBox", "Show search box") +
                "###PlayerTrack_ShowSearchBox_Checkbox",
                ref showSearchBox))
            {
                this.Plugin.Configuration.ShowSearchBox = showSearchBox;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowSearchBox_HelpMarker",
                                           "toggle to show search box on player list"));
            ImGui.Spacing();

            // search type
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

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SearchType_HelpMarker",
                                           "type of search to perform"));
        }
    }
}
