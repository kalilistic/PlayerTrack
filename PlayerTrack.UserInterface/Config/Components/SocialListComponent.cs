using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.DrunkenToad.Gui.Enums;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Config.Components;

public class SocialListComponent : ConfigViewComponent
{
    private const int maxLinkShells = 8;
    private LocalPlayer? player;
    private List<LocalPlayer> players = new();
    private List<string> playerNames = new();
    private int selectedPlayerIndex;
    private List<string> categoryNames = new();
    private List<string> dataCenterNames = new();
    private List<SocialList> socialLists = new();
    private int selectedDataCenterIndex;
    private uint selectedDataCenterId;
    private int lastPlayerCount;
    private Tuple<ActionRequest, SocialList>? socialListToUnsync;
    
    public override void Draw()
    {
        DrawWarning();
        DrawSelection();
        if (players.Count == 0 || player == null) return;
        Initialize();
        ImGuiHelpers.ScaledDummy(3f);
        if (ImGui.BeginTabBar("SocialList_TabBar", ImGuiTabBarFlags.None))
        {
            this.DrawSocialListTab("FL/BL/FC", SocialListType.FriendList, SocialListType.BlackList, SocialListType.FreeCompany);
            this.DrawSocialListTab("LS", SocialListType.LinkShell);
            this.DrawSocialListTab("CWLS", SocialListType.CrossWorldLinkShell);
        }

        ImGui.EndTabBar();
    }

    private void Initialize()
    {
        socialLists = SocialListService.GetSocialLists(player!.ContentId);
        categoryNames = ServiceContext.CategoryService.GetCategoryNames();
        dataCenterNames = DalamudContext.DataManager.DataCenters.Select(x => x.Value.Name).OrderBy(x => x).ToList();
        if (selectedDataCenterId == 0)
        {
            selectedDataCenterId = LocalPlayerService.GetLocalPlayerDataCenter();
            if (selectedDataCenterId == 0) return;
            selectedDataCenterIndex = dataCenterNames.IndexOf(DalamudContext.DataManager.DataCenters[selectedDataCenterId].Name);
        }
    }

    private void DrawWarning()
    {
        if (players.Count == 0) return;
        LocGui.SafeTextWrapped("SocialListWarning");
        ImGuiHelpers.ScaledDummy(1f);
    }

    private void DrawSelection()
    {
        players = LocalPlayerService.GetLocalPlayers();
        playerNames = LocalPlayerService.GetLocalPlayerNames();
        if (players.Count == 0)
        {
            LocGui.TextColored("NoLocalPlayers", ImGuiColors.DalamudYellow);
            return;
        }
        
        if (lastPlayerCount != players.Count)
        {
            this.selectedPlayerIndex = 0;
            lastPlayerCount = players.Count;
        }

        ToadGui.Combo("SelectPlayer", ref this.selectedPlayerIndex, playerNames, 180, false, false);
        player = players[this.selectedPlayerIndex];
    }
    
    private void DrawSocialListTab(string tabKey, params SocialListType[] socialListTypes)
    {
        if (player == null || players.Count == 0) return;

        if (LocGui.BeginTabItem(tabKey))
        {
            ImGui.BeginChild($"###{tabKey}_Child");
            ImGuiHelpers.ScaledDummy(1f);
            DrawDataCenterSelection(socialListTypes);

            if (socialListTypes.Any(x => x is SocialListType.LinkShell or SocialListType.CrossWorldLinkShell))
            {
                DrawLinkShells(socialListTypes);
            }
            else
            {
                DrawOtherSocialLists(socialListTypes);
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void DrawLinkShells(SocialListType[] socialListTypes)
    {
        for (var i = 1; i <= maxLinkShells; i++)
        {
            foreach (var type in socialListTypes.Where(t => t is SocialListType.LinkShell or SocialListType.CrossWorldLinkShell))
            {
                var socialList = GetSocialList(type, i);
                if (socialList == null) continue;
                DrawSocialList(socialList);
            }
        }
    }

    private void DrawOtherSocialLists(IEnumerable<SocialListType> socialListTypes)
    {
        foreach (var type in socialListTypes)
        {
            var socialList = GetSocialList(type);
            if (socialList == null) continue;
            DrawSocialList(socialList);
        }
    }

    private SocialList? GetSocialList(SocialListType type, int listNumber = -1)
    {
        if (player == null) return null;
        var match = socialLists.FirstOrDefault(x => x.ListType == type && x.ContentId == player.ContentId && 
                                                    (listNumber == -1 || x.ListNumber == listNumber) &&
                                                    (type != SocialListType.CrossWorldLinkShell || x.DataCenterId == selectedDataCenterId));
        if (match != null) return match;

        var newSocialList = new SocialList
        {
            ContentId = player.ContentId,
            ListType = type,
            ListNumber = listNumber != -1 ? (ushort)listNumber : default,
            DataCenterId = type == SocialListType.CrossWorldLinkShell ? selectedDataCenterId : default
        };

        if (newSocialList.Id == 0)
        {
            this.socialLists.Add(newSocialList);
        }

        return newSocialList;
    }
    
    private void DrawDataCenterSelection(IEnumerable<SocialListType> socialListTypes)
    {
        if (socialListTypes.Contains(SocialListType.CrossWorldLinkShell))
        {
            if (ToadGui.Combo("DataCenter", ref this.selectedDataCenterIndex, this.dataCenterNames, 140, false, false))
            {
                var dataCenterName = this.dataCenterNames.ElementAt(this.selectedDataCenterIndex);
                this.selectedDataCenterId = DalamudContext.DataManager.DataCenters.FirstOrDefault(x => x.Value.Name == dataCenterName).Key;
            }
            ImGuiHelpers.ScaledDummy(1f);
        }
    }
    
    private void DrawSocialList(SocialList socialList)
    {
        var listName = SocialListService.GetSocialListName(socialList.ListType, socialList.ListNumber);
        LocGui.TextColored(listName, ImGuiColors.DalamudViolet);
        var addPlayers = socialList.AddPlayers;
        if (ToadGui.Checkbox("AddPlayers", listName, ref addPlayers))
        {
            socialList.AddPlayers = addPlayers;
            socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
        }
        
        ToadGui.HelpMarker("SocialListAddPlayersHelpText");
        
        var syncWithCategory = socialList.SyncWithCategory;
        if (ToadGui.Checkbox("SyncWithCategory", listName, ref syncWithCategory))
        {
            if (syncWithCategory)
            {
                socialList.SyncWithCategory = syncWithCategory;
                socialList.Id = SocialListService.AddOrUpdateSocialList(socialList); 
            }
            else
            {
                this.socialListToUnsync = new Tuple<ActionRequest, SocialList>(ActionRequest.Pending, socialList);
            }
        }
        
        ToadGui.HelpMarker("SocialListSyncCategoryHelpText");
        
        ImGui.SameLine();
        
        DrawSyncConfirmation(socialList);

        if (socialList.SyncWithCategory && this.socialListToUnsync == null )
        {
            var syncedCategoryName = ServiceContext.CategoryService.GetSyncedCategory(socialList.Id)?.Name;
            if (!string.IsNullOrEmpty(syncedCategoryName))
            {
                ImGui.SameLine();
                ImGui.TextDisabled(syncedCategoryName);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 5.0f * ImGuiHelpers.GlobalScale);
            }
        }
        
        var disableCategoryBox = categoryNames.Count == 1;
        if (disableCategoryBox)
        {
            ImGui.BeginDisabled();
        }

        var selectedCategoryIndex = 0;
        var categoryName = ServiceContext.CategoryService.GetCategory(socialList.DefaultCategoryId)?.Name;
        if (!string.IsNullOrEmpty(categoryName))
        {
            selectedCategoryIndex = categoryNames.ToList().IndexOf(categoryName);
        }

        if (ToadGui.Combo("DefaultCategory", listName, ref selectedCategoryIndex, categoryNames, 140))
        {
            var category = ServiceContext.CategoryService.GetCategoryByName(categoryNames.ElementAt(selectedCategoryIndex));
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
        
        ToadGui.HelpMarker("SocialListDefaultCategoryHelpText");
        
        if (disableCategoryBox)
        {
            ImGui.EndDisabled();
        }
        
        ImGuiHelpers.ScaledDummy(3f);
    }
    
    private void DrawSyncConfirmation(SocialList socialList)
    {
        ToadGui.Confirm(socialList, "ConfirmUnsync", ref this.socialListToUnsync);
        if (this.socialListToUnsync?.Item1 == ActionRequest.Confirmed)
        {
            socialList.SyncWithCategory = false;
            socialList.Id = SocialListService.AddOrUpdateSocialList(socialList);
            SocialListService.DeleteSyncedCategory(socialList.Id);
            this.socialListToUnsync = null;
        }
        else if (this.socialListToUnsync?.Item1 == ActionRequest.None)
        {
            this.socialListToUnsync = null;
        }
    }
    
}
