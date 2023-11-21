using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.DrunkenToad.Helpers;
using PlayerTrack.Domain.Common;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class SocialListService
{
    public static void HandleMembersList(SocialListType listType, List<ToadSocialListMember> toadMembers, 
        ushort listNumber = 0, ushort page = 0, ushort pageCount = 0)
    {
        DalamudContext.PluginLog.Verbose($"Entering SocialListService.HandleMembersList: {listType}");
        
        // get content id
        var contentId = DalamudContext.ClientStateHandler.LocalContentId;
        if (contentId == 0)
        {
            DalamudContext.PluginLog.Warning("HandleMembersList: LocalContentId is 0");
            return;
        }
        
        // get local player
        var localPlayer = DalamudContext.ClientStateHandler.GetLocalPlayer();
        if (localPlayer == null)
        {
            DalamudContext.PluginLog.Warning("HandleMembersList: LocalPlayer is null");
            return;
        }
        
        // get data center
        var dataCenter = DalamudContext.DataManager.Worlds.Values.FirstOrDefault(x => x.Id == localPlayer.HomeWorld)?.DataCenterId ?? 0;
        if (dataCenter == 0)
        {
            DalamudContext.PluginLog.Warning("HandleMembersList: DataCenter is 0");
            return;
        }
        
        // retrieve or create social list
        // use dc for cwls since you have a different set per dc
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Retrieving social list for {contentId} {listType} {listNumber}");
        var socialList = listType != SocialListType.CrossWorldLinkShell ? 
            RepositoryContext.SocialListRepository.GetSocialList(contentId, listType, listNumber) : 
            RepositoryContext.SocialListRepository.GetSocialList(contentId, listType, listNumber, dataCenter);
        
        // check if list already exists
        if (socialList == null)
        {
            // create new list
            socialList = new SocialList
            {
                ContentId = contentId,
                ListType = listType,
            };
            
            // set list number for ls / cwls
            if (listType is SocialListType.LinkShell or SocialListType.CrossWorldLinkShell)
            {
                socialList.ListNumber = listNumber;
            }
            
            // set data center for cwls
            if (listType == SocialListType.CrossWorldLinkShell)
            {
                socialList.DataCenterId = dataCenter;
            }
            
            // set and verify id
            socialList.Id = RepositoryContext.SocialListRepository.CreateSocialList(socialList);
            if (socialList.Id == 0)
            {
                DalamudContext.PluginLog.Warning("HandleMembersList: Failed to create social list");
                return;
            }
        }
        
        // update page fields for FC
        else if (listType == SocialListType.FreeCompany)
        {
            var currentTime = UnixTimestampHelper.CurrentTime();
            if (socialList.PageLastUpdated.TryGetValue(page, out _))
            {
                socialList.PageLastUpdated[page] = currentTime;
            }
            else
            {
                socialList.PageLastUpdated.TryAdd(page, currentTime);
            }

            socialList.PageCount = pageCount;
            RepositoryContext.SocialListRepository.UpdateSocialList(socialList);
        }

        // retrieve existing members
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Retrieving existing members for {socialList.Id}");
        var existingMembers = RepositoryContext.SocialListMemberRepository.GetSocialListMembers(socialList.Id);
        
        // remove self
        toadMembers.RemoveAll(x => x.ContentId == contentId);
        
        // loop through incoming list
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Looping through {toadMembers.Count} members");
        foreach (var toadMember in toadMembers)
        {
            // check if new or existing
            var existingMember = existingMembers.FirstOrDefault(x => x.ContentId == toadMember.ContentId);
            var memberKey = PlayerKeyBuilder.Build(toadMember.Name, toadMember.HomeWorld);
            if (existingMember != null)
            {
                // update existing if name/world changed
                if (!existingMember.Key.Equals(memberKey))
                {
                    existingMember.Key = memberKey;
                    existingMember.Name = toadMember.Name;
                    existingMember.WorldId = toadMember.HomeWorld;
                    existingMember.PageNumber = page;
                }
            }
            else
            {
                // create new member
                var newMember = new SocialListMember
                {
                    SocialListId = socialList.Id,
                    ContentId = toadMember.ContentId,
                    Key = memberKey,
                    Name = toadMember.Name,
                    WorldId = toadMember.HomeWorld,
                    PageNumber = page,
                };
                
                RepositoryContext.SocialListMemberRepository.CreateSocialListMember(newMember);
            }
        }
        
        // get all current members for list
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Retrieving all members for {socialList.Id}");
        var socialListMembers = RepositoryContext.SocialListMemberRepository.GetSocialListMembers(socialList.Id, page);
        
        // remove members that are no longer in list
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Looping through {socialListMembers.Count} members");
        foreach (var socialListMember in socialListMembers)
        {
            var toadMember = toadMembers.FirstOrDefault(x => x.ContentId == socialListMember.ContentId);
            if (toadMember == null)
            {
                DalamudContext.PluginLog.Verbose($"HandleMembersList: Removing member {socialListMember.Name} from page {socialListMember.PageNumber}");
                RepositoryContext.SocialListMemberRepository.DeleteSocialListMember(socialListMember.Id);
            }
        }
        
        // remove members from deleted pages for FC
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Removing members from deleted pages");
        if (listType == SocialListType.FreeCompany)
        {
            var fcList = RepositoryContext.SocialListRepository.GetSocialList(contentId, SocialListType.FreeCompany);
            if (fcList != null)
            {
                var fcMembers = RepositoryContext.SocialListMemberRepository.GetSocialListMembers(fcList.Id);
                foreach (var fcMember in fcMembers)
                {
                    if (fcMember.PageNumber > socialList.PageCount)
                    {
                        DalamudContext.PluginLog.Verbose($"HandleMembersList: Removing FC member {fcMember.Name} from page {fcMember.PageNumber}");
                        RepositoryContext.SocialListMemberRepository.DeleteSocialListMember(fcMember.Id);
                    }
                }
            }
        }
        
        // get fresh copy
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Retrieving fresh copy of social list");
        socialList = listType != SocialListType.CrossWorldLinkShell ? 
            RepositoryContext.SocialListRepository.GetSocialList(contentId, listType, listNumber) : 
            RepositoryContext.SocialListRepository.GetSocialList(contentId, listType, listNumber, dataCenter);
        
        if (socialList == null)
        {
            DalamudContext.PluginLog.Warning("HandleMembersList: Failed to retrieve social list");
            return;
        }
        socialListMembers = RepositoryContext.SocialListMemberRepository.GetSocialListMembers(socialList.Id);

        // add / update players
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Adding / updating players");
        var players = new List<Player>();
        foreach (var socialListMember in socialListMembers)
        {
            var isUnableToRetrieve = string.IsNullOrEmpty(socialListMember.Name);
            var player = !isUnableToRetrieve ? 
                ServiceContext.PlayerDataService.GetPlayer(socialListMember.Key) : 
                ServiceContext.PlayerDataService.GetPlayer(socialListMember.ContentId);

            if (player == null)
            {
                if (socialList.AddPlayers)
                {
                    if (isUnableToRetrieve) continue;
                    PlayerProcessService.CreateNewPlayer(socialListMember.Name, socialListMember.WorldId, socialListMember.ContentId);
                    player = ServiceContext.PlayerDataService.GetPlayer(socialListMember.Key);
                }
            }
            else if (player.ContentId == 0)
            {
                player.ContentId = socialListMember.ContentId;
                ServiceContext.PlayerDataService.UpdatePlayer(player);
            }

            if (player != null) players.Add(player);
            
        }
        
        // setup category list
        var categoryIds = new List<int>();
        
        // sync with dynamic category
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Syncing with dynamic category");
        var syncedCategory = ServiceContext.CategoryService.GetSyncedCategory(socialList.Id);
        if (socialList.SyncWithCategory)
        {
            if (syncedCategory == null)
            {
                ServiceContext.CategoryService.CreateCategory(GetCategoryName(contentId, listType, listNumber), socialList.Id);
                syncedCategory = ServiceContext.CategoryService.GetSyncedCategory(socialList.Id);

                if (syncedCategory == null)
                {
                    DalamudContext.PluginLog.Warning("HandleMembersList: Failed to create synced category");
                    return;
                }
            }
            
            categoryIds.Add(syncedCategory.Id);
        }
        else if (syncedCategory != null)
        {
            ServiceContext.CategoryService.DeleteCategory(syncedCategory);
        }
        
        // add to default category if missing
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Adding to default category");
        if (socialList.DefaultCategoryId != 0)
        {
            categoryIds.Add(socialList.DefaultCategoryId);
        }
        
        // add content id to player
        DalamudContext.PluginLog.Verbose($"HandleMembersList: Adding content id to players");
        foreach (var player in players)
        {
            player.ContentId = contentId;
        }
        
        // assign categories
        if (categoryIds.Count > 0)
        {
            PlayerCategoryService.AssignCategoriesToPlayers(players, categoryIds.ToArray());
        }
    }

    public static List<SocialList> GetSocialLists(ulong contentId)
    {
        return RepositoryContext.SocialListRepository.GetSocialLists(contentId);
    }

    public static int AddOrUpdateSocialList(SocialList socialList)
    {
        if (socialList.Id == 0)
        {
            socialList.Id = RepositoryContext.SocialListRepository.CreateSocialList(socialList);
        }
        else
        {
            RepositoryContext.SocialListRepository.UpdateSocialList(socialList);
        }

        return socialList.Id;

    }

    public static string GetSocialListName(SocialListType socialListType, int listNumber = 0)
    {
        var socialListName = DalamudContext.LocManager.GetString(socialListType.ToString());
        return listNumber == 0 ? socialListName : $"{socialListName} [{listNumber}]";
    }

    public static Category ResetCategoryName(Category category)
    {
        category.Name = GetCategoryName(category);
        ServiceContext.CategoryService.UpdateCategory(category);
        return category;    
    }

    public static string GetCategoryName(Category category)
    {
        var socialList = RepositoryContext.SocialListRepository.GetSocialList(category.SocialListId);
        return socialList == null ? category.Name : GetCategoryName(socialList.ContentId, socialList.ListType, socialList.ListNumber);
    }
    
    public static string GetCategoryName(ulong contentId, SocialListType socialListType, int listNumber)
    {
        var localPlayer = LocalPlayerService.GetLocalPlayer(contentId);
        if (localPlayer == null) return string.Empty;

        var playerName = LocalPlayerService.GetLocalPlayerFullName(contentId);
        var socialListTypeAbbreviation = DalamudContext.LocManager.GetString($"{socialListType}_Abbreviation");

        switch (socialListType)
        {
            case SocialListType.None:
            case SocialListType.FriendList:
            case SocialListType.BlackList:
            case SocialListType.FreeCompany:
                return $"{socialListTypeAbbreviation} [{playerName}]";
            case SocialListType.LinkShell:
            case SocialListType.CrossWorldLinkShell:
                return $"{socialListTypeAbbreviation}#{listNumber} [{playerName}]";
            default:
                throw new ArgumentOutOfRangeException(nameof(socialListType), socialListType, null);
        }
    }

    public static void DeleteSocialLists(ulong localPlayerContentId)
    {
        var socialLists = RepositoryContext.SocialListRepository.GetSocialLists(localPlayerContentId);
        foreach (var socialList in socialLists)
        {
            var syncedCategory = ServiceContext.CategoryService.GetSyncedCategory(socialList.Id);
            if (syncedCategory != null)
            {
                ServiceContext.CategoryService.DeleteCategory(syncedCategory);
            }
            
            RepositoryContext.SocialListMemberRepository.DeleteSocialListMembers(socialList.Id);
            RepositoryContext.SocialListRepository.DeleteSocialList(socialList.Id);
        }
    }

    public static void ClearCategoryFromSocialLists(int categoryId)
    {
       var socialLists = RepositoryContext.SocialListRepository.GetSocialListsWithDefaultCategory(categoryId);
       foreach (var socialList in socialLists)
       {
           socialList.DefaultCategoryId = 0;
           RepositoryContext.SocialListRepository.UpdateSocialList(socialList);
       }
    }

    public static void DeleteSyncedCategory(int socialListId)
    {
        var syncedCategory = ServiceContext.CategoryService.GetSyncedCategory(socialListId);
        if (syncedCategory != null)
        {
            ServiceContext.CategoryService.DeleteCategory(syncedCategory);
        }
    }
}