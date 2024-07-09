using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Main.Presenters;

// ReSharper disable InconsistentNaming
namespace PlayerTrack.UserInterface.Main.Components;

using Dalamud.DrunkenToad.Helpers;
using Dalamud.Interface.Utility;

public class PlayerListComponent : ViewComponent
{
    private const int DebounceTime = 300;
    private readonly IMainPresenter presenter;
    private List<Category> categories = null!;
    private List<string> categoryNames = null!;
    private ImGuiListClipperPtr clipper;
    private long lastInputTime;
    private bool isSearchDirty;

    public PlayerListComponent(IMainPresenter presenter) => this.presenter = presenter;

    public delegate void PlayerListComponent_OpenConfigDelegate();

    public event PlayerListComponent_OpenConfigDelegate? PlayerListComponent_OpenConfig;

    public override void Draw()
    {
        this.categories = ServiceContext.CategoryService.GetCategories(false);
        this.categoryNames = ServiceContext.CategoryService.GetCategoryNames(false, false);
        ImGui.BeginChild("###LeftPanel", new Vector2(205 * ImGuiHelpers.GlobalScale, 0), false);
        this.DrawControls();
        ImGui.BeginChild("###PlayerList", new Vector2(205 * ImGuiHelpers.GlobalScale, 0), true);

        this.SetupClipper();
        var playersCount = this.presenter.GetPlayersCount();
        this.clipper.Begin(playersCount);
        var shouldShowSeparator = this.config.ShowCategorySeparator && string.IsNullOrEmpty(this.config.SearchInput);
        while (this.clipper.Step())
        {
            var players = this.presenter.GetPlayers(this.clipper.DisplayStart, this.clipper.DisplayEnd);
            for (var i = 0; i < players.Count; i++)
            {
                ImGui.BeginGroup();
                var player = players.ElementAtOrDefault(i);
                if (player != null)
                {
                    if (shouldShowSeparator && i > 0 && i < players.Count && player.PrimaryCategoryId != players[i - 1].PrimaryCategoryId)
                    {
                        ImGui.Separator();
                    }

                    this.DrawPlayer(player);
                }

                ImGui.EndGroup();
            }
        }

        this.clipper.End();
        ImGui.EndChild();
        ImGui.EndChild();
    }

    private unsafe void SetupClipper() => this.clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());

    private void DrawControls()
    {
        ImGui.BeginGroup();

        // Player Filter
        if (this.config.ShowPlayerFilter)
        {
            var playerListFilter = this.config.PlayerListFilter;
            if (ToadGui.Combo("###PlayerList_Filter", ref playerListFilter, -1, false))
            {
                this.config.PlayerListFilter = playerListFilter;
                ServiceContext.ConfigService.SaveConfig(this.config);
                ServiceContext.PlayerCacheService.Resort();
                this.presenter.ClearCache();
            }
        }

        // Category Filter
        if (this.config.PlayerListFilter == PlayerListFilter.PlayersByCategory)
        {
            var categoryFilters = ServiceContext.CategoryService.GetCategoryFilters();

            if (categoryFilters.TotalFilters == 0)
            {
                ImGui.BeginDisabled();
            }

            var filterCategoryIndex = this.config.FilterCategoryIndex;
            if (ToadGui.Combo("###PlayerList_CategoryFilter", ref filterCategoryIndex, categoryFilters.FilterNames, -1, false))
            {
                this.config.FilterCategoryIndex = filterCategoryIndex;
                this.config.FilterCategoryId = categoryFilters.FilterIds[this.config.FilterCategoryIndex];
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.presenter.ClearCache();
            }

            if (categoryFilters.TotalFilters == 0)
            {
                ImGui.EndDisabled();
            }
        }

        // Tag Filter
        if (this.config.PlayerListFilter == PlayerListFilter.PlayersByTag)
        {
            var tagFilters = ServiceContext.TagService.GetTagFilters();

            if (tagFilters.TotalFilters == 0)
            {
                ImGui.BeginDisabled();
            }

            var filterTagIndex = this.config.FilterTagIndex;
            if (ToadGui.Combo("###PlayerList_TagFilter", ref filterTagIndex, tagFilters.FilterNames, -1, false))
            {
                this.config.FilterTagIndex = filterTagIndex;
                this.config.FilterTagId = tagFilters.FilterIds[this.config.FilterTagIndex];
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.presenter.ClearCache();
            }

            if (tagFilters.TotalFilters == 0)
            {
                ImGui.EndDisabled();
            }
        }

        // Search Box
        if (this.config.ShowSearchBox)
        {
            ImGui.SetNextItemWidth(-1);
            var searchInput = this.config.SearchInput;

            if (LocGui.InputTextWithHint("###PlayerList_Search", "SearchPlayersHint", ref searchInput, 30))
            {
                this.config.SearchInput = searchInput;
                this.lastInputTime = UnixTimestampHelper.CurrentTime();
                this.isSearchDirty = true;
            }

            if (this.isSearchDirty && UnixTimestampHelper.CurrentTime() - this.lastInputTime > DebounceTime)
            {
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.presenter.ClearCache();
                this.isSearchDirty = false;
            }
        }

        // Dummy spacing if nothing available so can still open menu
        if (this.config is { ShowSearchBox: false, ShowPlayerFilter: false })
        {
            ImGui.Dummy(new Vector2(this.config.MainWindowWidth, 5f));
        }

        ImGui.EndGroup();

        // Open config popup on right click
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("###PlayerTrack_Menu_Popup");
        }

        // Config menu popup
        if (ImGui.BeginPopup("###PlayerTrack_Menu_Popup", ImGuiWindowFlags.Popup))
        {
            if (LocGui.MenuItem("AddPlayer"))
            {
                this.presenter.TogglePanel(PanelType.AddPlayer);
            }

            if (LocGui.MenuItem("OpenSettings"))
            {
                this.PlayerListComponent_OpenConfig?.Invoke();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawPlayer(Player player)
    {
        ImGui.BeginGroup();
        if (ImGui.Selectable("###PlayerSelectable" + player.Id, this.presenter.GetSelectedPlayer()?.Id == player.Id))
        {
            // hide right subWindow if clicking same user while already open
            if (this.presenter.GetSelectedPlayer()?.Id == player.Id && this.config.PanelType == PanelType.Player)
            {
                this.presenter.ClosePlayer();
                this.presenter.HidePanel();
            }

            // open selectedPlayer in right subWindow
            else
            {
                this.presenter.SelectPlayer(player);
                this.presenter.ShowPanel(PanelType.Player);
            }
        }

        ImGuiHelpers.ScaledRelativeSameLine(-10f / ImGuiHelpers.GlobalScale);

        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(player.PlayerListNameColor, player.PlayerListIconString);
        ImGui.PopFont();
        ImGui.SameLine();
        LocGui.TextColored(player.Name, player.PlayerListNameColor);

        ImGui.EndGroup();

        // open menu options for selected player
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            ImGui.OpenPopup("###PlayerList_Popup_" + player.Id);
        }

        // pop up for selected player
        if (ImGui.BeginPopup("###PlayerList_Popup_" + player.Id))
        {
            if (this.presenter.GetSelectedPlayer()?.Id == player.Id)
            {
                if (LocGui.MenuItem("ClosePlayer"))
                {
                    this.presenter.ClosePlayer();
                    this.presenter.HidePanel();
                }
            }
            else
            {
                if (LocGui.MenuItem("OpenPlayer"))
                {
                    this.presenter.SelectPlayer(player);
                    this.presenter.ShowPanel(PanelType.Player);
                }
            }

            if (player.IsCurrent)
            {
                if (LocGui.MenuItem("TargetPlayer"))
                {
                    DalamudContext.TargetManager.SetTarget(player.EntityId);
                }

                if (LocGui.MenuItem("FocusTargetPlayer"))
                {
                    DalamudContext.TargetManager.SetFocusTarget(player.EntityId);
                }

                if (LocGui.MenuItem("OpenPlayerPlate"))
                {
                    DalamudContext.TargetManager.OpenPlateWindow(player.EntityId);
                }
            }

            if (LocGui.MenuItem("OpenLodestone"))
            {
                ServiceContext.LodestoneService.OpenLodestoneProfile(player.Name, player.WorldId);
            }

            // sub menu for selecting category
            if (this.categoryNames.Count > 0)
            {
                if (LocGui.BeginMenu("AssignPlayerCategory"))
                {
                    var playerCategoryIds = player.AssignedCategories.Select(cat => cat.Id).ToArray();
                    for (var j = 0; j < this.categoryNames.Count; j++)
                    {
                        var isSelected = playerCategoryIds.Contains(this.categories[j].Id);
                        if (ImGui.MenuItem(
                                this.categoryNames[j],
                                string.Empty,
                                isSelected,
                                true))
                        {
                            if (isSelected)
                            {
                                PlayerCategoryService.UnassignCategoryFromPlayer(player.Id, this.categories[j].Id);
                            }
                            else
                            {
                                PlayerCategoryService.AssignCategoryToPlayer(player.Id, this.categories[j].Id);
                            }
                        }
                    }

                    ImGui.EndMenu();
                }
            }

            ImGui.EndPopup();
        }
    }
}
