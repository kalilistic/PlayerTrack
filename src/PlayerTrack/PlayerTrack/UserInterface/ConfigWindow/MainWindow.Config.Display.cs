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

            // lock overlay
            var lockWindow = this.plugin.Configuration.LockWindow;
            if (ImGui.Checkbox(
                Loc.Localize("LockWindow", "Lock window") + "###PlayerTrack_LockWindow_Checkbox",
                ref lockWindow))
            {
                this.plugin.Configuration.LockWindow = lockWindow;
                this.Plugin.SaveConfig();
                if (lockWindow)
                {
                    this.plugin.WindowManager.MainWindow?.LockWindow();
                    this.plugin.WindowManager.PlayerDetailWindow.LockWindow();
                }
                else
                {
                    this.plugin.WindowManager.MainWindow?.UnlockWindow();
                    this.plugin.WindowManager.PlayerDetailWindow.UnlockWindow();
                }
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "LockWindow_HelpMarker",
                                           "keep main window locked in size/position"));
            ImGui.Spacing();

            // combine player detail window
            var combinedPlayerDetailWindow = this.plugin.Configuration.CombinedPlayerDetailWindow;
            if (ImGui.Checkbox(
                Loc.Localize("CombinedPlayerDetailWindow", "Combine windows") + "###PlayerTrack_CombinedPlayerDetailWindow_Checkbox",
                ref combinedPlayerDetailWindow))
            {
                this.plugin.Configuration.CombinedPlayerDetailWindow = combinedPlayerDetailWindow;
                this.plugin.WindowManager.ShouldTogglePlayerDetail = true;
                if (combinedPlayerDetailWindow)
                {
                    this.plugin.WindowManager.Panel!.ShowPanel(View.AddPlayer);
                }

                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "CombinedPlayerDetailWindow_HelpMarker",
                                           "show combined or separate windows like original version"));
            ImGui.Spacing();

            // show player tags
            var showPlayerTags = this.plugin.Configuration.ShowPlayerTags;
            if (ImGui.Checkbox(
                Loc.Localize("ShowTags", "Show player tags") + "###PlayerTrack_ShowPlayerTags_Checkbox",
                ref showPlayerTags))
            {
                this.plugin.Configuration.ShowPlayerTags = showPlayerTags;
                this.plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowPlayerTags_HelpMarker",
                                           "show player tags on detail screen"));
            ImGui.Spacing();

            // show player tags
            var searchTags = this.plugin.Configuration.SearchTags;
            if (ImGui.Checkbox(
                Loc.Localize("SearchTags", "Check tags on search") + "###PlayerTrack_SearchTags_Checkbox",
                ref searchTags))
            {
                this.plugin.Configuration.SearchTags = searchTags;
                this.plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SearchTags_HelpMarker",
                                           "search player tags with search"));
            ImGui.Spacing();

            // player list offset
            var playerListOffset = this.plugin.Configuration.PlayerListOffset;
            if (ImGui.Checkbox(
                Loc.Localize("PlayerListOffset", "Add offset to player list") + "###PlayerTrack_PlayerListOffset_Checkbox",
                ref playerListOffset))
            {
                this.plugin.Configuration.PlayerListOffset = playerListOffset;
                this.plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "PlayerListOffset_HelpMarker",
                                           "toggle offset with player list to work nicer with dalamud ui customizations like material ui"));
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
