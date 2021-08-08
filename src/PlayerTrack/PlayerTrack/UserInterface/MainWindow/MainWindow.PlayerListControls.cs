using System;
using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Search.
    /// </summary>
    public partial class MainWindow
    {
        private string searchInput = string.Empty;

        private void PlayerListControls()
        {
            var playerFilterTypeIndex = this.plugin.Configuration.PlayerFilterType;

            ImGui.BeginGroup();

            // filter type
            if (this.Plugin.Configuration.ShowFilterType)
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo(
                    "###PlayerTrack_PlayerFilterType_Combo",
                    ref playerFilterTypeIndex,
                    PlayerFilterType.FilterTypeNames.ToArray(),
                    PlayerFilterType.FilterTypeNames.Count))
                {
                    this.plugin.Configuration.PlayerFilterType = PlayerFilterType.GetPlayerFilterTypeByIndex(playerFilterTypeIndex).Index;
                    this.plugin.SaveConfig();
                    this.plugin.PlayerService.UpdateViewPlayers();
                }
            }

            // category filter
            if (PlayerFilterType.GetPlayerFilterTypeByIndex(playerFilterTypeIndex) ==
                PlayerFilterType.PlayersByCategory)
            {
                var categoryNames = this.plugin.CategoryService.GetCategoryNames().ToArray();
                var categoryIds = this.plugin.CategoryService.GetCategoryIds().ToArray();
                var categoryIndex = Array.IndexOf(categoryIds, this.plugin.Configuration.CategoryFilterId);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Combo(
                    "###PlayerTrack_CategoryFilter_Combo",
                    ref categoryIndex,
                    categoryNames,
                    categoryNames.Length))
                {
                    this.plugin.Configuration.CategoryFilterId = categoryIds[categoryIndex];
                    this.plugin.SaveConfig();
                    this.plugin.PlayerService.UpdateViewPlayers();
                }
            }

            // search box
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (this.Plugin.Configuration.ShowSearchBox)
            {
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputTextWithHint(
                    "###PlayerTrack_SearchBox_Input",
                    Loc.Localize("SearchHint", "search"),
                    ref this.searchInput,
                    30))
                {
                    this.lastPlayerListRefresh = DateUtil.CurrentTime();
                }
            }

            // add dummy spacing if nothing available so can still open menu
            if (!this.Plugin.Configuration.ShowSearchBox && !this.Plugin.Configuration.ShowFilterType)
            {
                var vector2 = this.windowSize;
                if (vector2 != null) ImGui.Dummy(new Vector2(vector2.Value.X, 5f));
            }

            ImGui.EndGroup();

            // open config popup on right click
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("###PlayerTrack_Menu_Popup");
            }

            // config menu popup
            if (ImGui.BeginPopup("###PlayerTrack_Menu_Popup"))
            {
                if (ImGui.MenuItem(
                    Loc.Localize("AddPlayer", "Add Player")))
                {
                    this.ToggleRightPanel(View.AddPlayer);
                }

                if (ImGui.MenuItem(
                    Loc.Localize("OpenLodestoneService", "Open Lodestone")))
                {
                    this.ToggleRightPanel(View.Lodestone);
                }

                if (ImGui.MenuItem(
                    Loc.Localize("OpenSettings", "Open Settings")))
                {
                    this.Plugin.WindowManager.ConfigWindow!.IsOpen ^= true;
                }

                ImGui.EndPopup();
            }
        }
    }
}
