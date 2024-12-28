using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

[SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
public class PlayerListComponent(IMainPresenter presenter) : ViewComponent
{
    private const int DebounceTime = 300;
    private List<Category> categories = null!;
    private List<string> categoryNames = null!;
    private ImGuiListClipperPtr clipper;
    private bool pendingFilterUpdate;
    private long lastInputTime;
    private bool isSearchDirty;

    public delegate void PlayerListComponent_OpenConfigDelegate();

    public event PlayerListComponent_OpenConfigDelegate? PlayerListComponent_OpenConfig;

    public override void Draw()
    {
        if (this.pendingFilterUpdate)
        {
            this.pendingFilterUpdate = false;
            ServiceContext.ConfigService.SaveConfig(this.config);
            ServiceContext.PlayerCacheService.Resort();
            presenter.ClearCache();
        }

        this.categories = ServiceContext.CategoryService.GetCategories(false);
        this.categoryNames = ServiceContext.CategoryService.GetCategoryNames(false, false);
        var playersCount = presenter.GetPlayersCount();

        ImGui.BeginChild("###LeftPanel", new Vector2(205 * ImGuiHelpers.GlobalScale, 0), false);
        this.DrawControls(playersCount);
        ImGui.BeginChild("###PlayerList", new Vector2(205 * ImGuiHelpers.GlobalScale, 0), true);

        if (playersCount == 0)
        {
            ImGui.EndChild();
            ImGui.EndChild();
            return;
        }

        this.SetupClipper();
        this.clipper.Begin(playersCount);
        var shouldShowSeparator = this.config.ShowCategorySeparator && string.IsNullOrEmpty(this.config.SearchInput);

        while (this.clipper.Step())
        {
            var players = presenter.GetPlayers(this.clipper.DisplayStart, this.clipper.DisplayEnd);
            for (var i = 0; i < players.Count; i++)
            {
                ImGui.BeginGroup();

                var player = players[i];
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (player != null)
                {
                    if (shouldShowSeparator &&
                        i > 0 && i < players.Count &&
                        player.PrimaryCategoryId != players[i - 1].PrimaryCategoryId)
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

    private unsafe void SetupClipper()
    {
        this.clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
    }

    private void onPlayerFilterSelect(PlayerListFilter playerListFilter)
    {
        this.config.PlayerListFilter = playerListFilter;
        this.pendingFilterUpdate = true;
    }
    
    private void DrawControls(int playersCount)
    {
        ImGui.BeginGroup();

        // Player Filter
        if (this.config.ShowPlayerFilter)
        {
            var playerListFilter = this.config.PlayerListFilter;
            if (this.config.ShowPlayerCountInFilter)
            {
                var playerCountString =
                    $"({playersCount.ToString("N0", System.Globalization.CultureInfo.CurrentCulture)})";
                if (ToadGui.Combo("###PlayerList_Filter", ref playerListFilter, playerCountString))
                {
                    onPlayerFilterSelect(playerListFilter);
                }
            }
            else
            {
                if (ToadGui.Combo("###PlayerList_Filter", ref playerListFilter, -1, false))
                {
                    onPlayerFilterSelect(playerListFilter);
                }
            }
        }

        // Category Filter
        if (this.config.PlayerListFilter == PlayerListFilter.PlayersByCategory)
        {
            var categoryFilters = ServiceContext.CategoryService.GetCategoryFilters();
            if (categoryFilters.TotalFilters == 0) ImGui.BeginDisabled();

            var filterCategoryIndex = this.config.FilterCategoryIndex;
            if (ToadGui.Combo("###PlayerList_CategoryFilter",
                    ref filterCategoryIndex,
                    categoryFilters.FilterNames,
                    -1,
                    false))
            {
                this.config.FilterCategoryIndex = filterCategoryIndex;
                this.config.FilterCategoryId = categoryFilters.FilterIds[this.config.FilterCategoryIndex];
                this.pendingFilterUpdate = true;
            }

            if (categoryFilters.TotalFilters == 0) ImGui.EndDisabled();
        }

        // Tag Filter
        if (this.config.PlayerListFilter == PlayerListFilter.PlayersByTag)
        {
            var tagFilters = ServiceContext.TagService.GetTagFilters();
            if (tagFilters.TotalFilters == 0) ImGui.BeginDisabled();

            var filterTagIndex = this.config.FilterTagIndex;
            if (ToadGui.Combo("###PlayerList_TagFilter",
                    ref filterTagIndex,
                    tagFilters.FilterNames,
                    -1,
                    false))
            {
                this.config.FilterTagIndex = filterTagIndex;
                this.config.FilterTagId = tagFilters.FilterIds[this.config.FilterTagIndex];
                this.pendingFilterUpdate = true;
            }

            if (tagFilters.TotalFilters == 0) ImGui.EndDisabled();
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

            if (this.isSearchDirty &&
                UnixTimestampHelper.CurrentTime() - this.lastInputTime > DebounceTime)
            {
                this.isSearchDirty = false;
                this.pendingFilterUpdate = true;
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
                presenter.TogglePanel(PanelType.AddPlayer);
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

        var isSelected = (presenter.GetSelectedPlayer()?.Id == player.Id);
        if (ImGui.Selectable("###PlayerSelectable" + player.Id, isSelected))
        {
            // hide right subWindow if clicking same user while already open
            if (isSelected && this.config.PanelType == PanelType.Player)
            {
                presenter.ClosePlayer();
                presenter.HidePanel();
            }

            // open selectedPlayer in right subWindow
            else
            {
                presenter.SelectPlayer(player);
                presenter.ShowPanel(PanelType.Player);
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
            if (isSelected)
            {
                if (LocGui.MenuItem("ClosePlayer"))
                {
                    presenter.ClosePlayer();
                    presenter.HidePanel();
                }
            }
            else
            {
                if (LocGui.MenuItem("OpenPlayer"))
                {
                    presenter.SelectPlayer(player);
                    presenter.ShowPanel(PanelType.Player);
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
                        var isCatSelected = playerCategoryIds.Contains(this.categories[j].Id);
                        if (ImGui.MenuItem(this.categoryNames[j], string.Empty, isCatSelected, true))
                        {
                            if (isCatSelected)
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