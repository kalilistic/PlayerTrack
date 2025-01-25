using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Enums;
using PlayerTrack.Models;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class SocialListComponent : ConfigViewComponent
{
    private const int MaxLinkShells = 8;
    private LocalPlayer? Player;
    private List<LocalPlayer> Players = [];
    private List<string> PlayerNames = [];
    private int SelectedPlayerIndex;
    private List<string> CategoryNames = [];
    private List<string> DataCenterNames = [];
    private List<SocialList> SocialLists = [];
    private int SelectedDataCenterIndex;
    private uint SelectedDataCenterId;
    private int LastPlayerCount;
    private Tuple<ActionRequest, SocialList>? SocialListToUnsync;

    public override void Draw()
    {
        DrawWarning();
        DrawSelection();
        if (Players.Count == 0 || Player == null)
            return;

        Initialize();
        ImGuiHelpers.ScaledDummy(3f);

        using var tabBar = ImRaii.TabBar("SocialList_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        DrawSocialListTab(Language.FL_BL_FC, SocialListType.FriendList, SocialListType.BlackList, SocialListType.FreeCompany);
        DrawSocialListTab(Language.LS, SocialListType.LinkShell);
        DrawSocialListTab(Language.CWLS, SocialListType.CrossWorldLinkShell);
    }

    private void Initialize()
    {
        SocialLists = SocialListService.GetSocialLists(Player!.ContentId);
        CategoryNames = ServiceContext.CategoryService.GetCategoryNames();
        DataCenterNames = Sheets.DataCenters.Select(x => x.Value.Name).OrderBy(x => x).ToList();
        if (SelectedDataCenterId == 0)
        {
            SelectedDataCenterId = LocalPlayerService.GetLocalPlayerDataCenter();
            if (SelectedDataCenterId == 0) return;
            SelectedDataCenterIndex = DataCenterNames.IndexOf(Sheets.DataCenters[SelectedDataCenterId].Name);
        }
    }

    private void DrawWarning()
    {
        if (Players.Count == 0)
            return;

        Helper.TextWrapped(Language.SocialListWarning);
        ImGuiHelpers.ScaledDummy(1f);
    }

    private void DrawSelection()
    {
        Players = LocalPlayerService.GetLocalPlayers();
        PlayerNames = LocalPlayerService.GetLocalPlayerNames();
        if (Players.Count == 0)
        {
            Helper.TextColored(ImGuiColors.DalamudYellow, Language.NoLocalPlayers);
            return;
        }

        if (LastPlayerCount != Players.Count)
        {
            SelectedPlayerIndex = 0;
            LastPlayerCount = Players.Count;
        }

        Helper.Combo("##SelectPlayer", ref SelectedPlayerIndex, PlayerNames, 250, false, false);
        Player = Players[SelectedPlayerIndex];
    }

    private void DrawSocialListTab(string tabKey, params SocialListType[] socialListTypes)
    {
        if (Player == null || Players.Count == 0)
            return;

        using var tabItem = ImRaii.TabItem(tabKey);
        if (!tabItem.Success)
            return;

        using var child = ImRaii.Child($"###{tabKey}_Child");
        if (!child.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);
        DrawDataCenterSelection(socialListTypes);

        if (socialListTypes.Any(x => x is SocialListType.LinkShell or SocialListType.CrossWorldLinkShell))
            DrawLinkShells(socialListTypes);
        else
            DrawOtherSocialLists(socialListTypes);
    }

    private void DrawLinkShells(SocialListType[] socialListTypes)
    {
        for (var i = 1; i <= MaxLinkShells; i++)
        {
            foreach (var type in socialListTypes.Where(t => t is SocialListType.LinkShell or SocialListType.CrossWorldLinkShell))
            {
                var socialList = GetSocialList(type, i);
                if (socialList == null)
                    continue;

                DrawSocialList(socialList);
            }
        }
    }

    private void DrawOtherSocialLists(IEnumerable<SocialListType> socialListTypes)
    {
        foreach (var type in socialListTypes)
        {
            var socialList = GetSocialList(type);
            if (socialList == null)
                continue;

            DrawSocialList(socialList);
        }
    }

    private SocialList? GetSocialList(SocialListType type, int listNumber = -1)
    {
        if (Player == null)
            return null;

        var match = SocialLists.FirstOrDefault(x => x.ListType == type && x.ContentId == Player.ContentId &&
                                                    (listNumber == -1 || x.ListNumber == listNumber) &&
                                                    (type != SocialListType.CrossWorldLinkShell || x.DataCenterId == SelectedDataCenterId));
        if (match != null)
            return match;

        var newSocialList = new SocialList
        {
            ContentId = Player.ContentId,
            ListType = type,
            ListNumber = listNumber != -1 ? (ushort)listNumber : (ushort)0,
            DataCenterId = type == SocialListType.CrossWorldLinkShell ? SelectedDataCenterId : 0
        };

        if (newSocialList.Id == 0)
            SocialLists.Add(newSocialList);

        return newSocialList;
    }

    private void DrawDataCenterSelection(IEnumerable<SocialListType> socialListTypes)
    {
        if (socialListTypes.Contains(SocialListType.CrossWorldLinkShell))
        {
            if (Helper.Combo("##DataCenter", ref SelectedDataCenterIndex, DataCenterNames, 140, false, false))
            {
                var dataCenterName = DataCenterNames.ElementAt(SelectedDataCenterIndex);
                SelectedDataCenterId = Sheets.DataCenters.FirstOrDefault(x => x.Value.Name == dataCenterName).Key;
            }
            ImGuiHelpers.ScaledDummy(1f);
        }
    }

    private void DrawSocialList(SocialList socialList)
    {
        var listName = SocialListService.GetSocialListName(socialList.ListType, socialList.ListNumber);
        Helper.TextColored(ImGuiColors.DalamudViolet, listName);
        var addPlayers = socialList.AddPlayers;
        if (Helper.Checkbox(Language.AddPlayers, listName, ref addPlayers))
        {
            socialList.AddPlayers = addPlayers;
            socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
        }

        ImGuiComponents.HelpMarker(Language.SocialListAddPlayersHelpText);

        var syncWithCategory = socialList.SyncWithCategory;
        if (Helper.Checkbox(Language.SyncWithCategory, listName, ref syncWithCategory))
        {
            if (syncWithCategory)
            {
                socialList.SyncWithCategory = syncWithCategory;
                socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
            }
            else
            {
                SocialListToUnsync = new Tuple<ActionRequest, SocialList>(ActionRequest.Pending, socialList);
            }
        }

        ImGuiComponents.HelpMarker(Language.SocialListSyncCategoryHelpText);

        ImGui.SameLine();

        DrawSyncConfirmation(socialList);

        if (socialList.SyncWithCategory && SocialListToUnsync == null )
        {
            var syncedCategoryName = ServiceContext.CategoryService.GetSyncedCategory(socialList.Id)?.Name;
            if (!string.IsNullOrEmpty(syncedCategoryName))
            {
                ImGui.SameLine();
                ImGui.TextDisabled(syncedCategoryName);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5.0f * ImGuiHelpers.GlobalScale);
            }
        }

        var disableCategoryBox = CategoryNames.Count == 1;
        using (ImRaii.Disabled(disableCategoryBox))
        {
            var selectedCategoryIndex = 0;
            var categoryName = ServiceContext.CategoryService.GetCategory(socialList.DefaultCategoryId)?.Name;
            if (!string.IsNullOrEmpty(categoryName))
            {
                selectedCategoryIndex = CategoryNames.ToList().IndexOf(categoryName);
            }

            if (Helper.Combo(Language.DefaultCategory, listName, ref selectedCategoryIndex, CategoryNames, 140))
            {
                var category = ServiceContext.CategoryService.GetCategoryByName(CategoryNames.ElementAt(selectedCategoryIndex));
                if (category?.Id != null)
                {
                    socialList.DefaultCategoryId = category.Id;
                    socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
                }
                else if (selectedCategoryIndex == 0)
                {
                    socialList.DefaultCategoryId = 0;
                    socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
                }
            }

            ImGuiComponents.HelpMarker(Language.SocialListDefaultCategoryHelpText);
        }

        ImGuiHelpers.ScaledDummy(3f);
    }

    private void DrawSyncConfirmation(SocialList socialList)
    {
        Helper.Confirm(socialList, Language.ConfirmUnsync, ref SocialListToUnsync);
        if (SocialListToUnsync?.Item1 == ActionRequest.Confirmed)
        {
            socialList.SyncWithCategory = false;
            socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
            SocialListService.DeleteSyncedCategory(socialList.Id);
            SocialListToUnsync = null;
        }
        else if (SocialListToUnsync?.Item1 == ActionRequest.None)
        {
            SocialListToUnsync = null;
        }
    }
}
