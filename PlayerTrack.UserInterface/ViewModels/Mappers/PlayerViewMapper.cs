using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.Models.Structs;
using PlayerTrack.UserInterface.Helpers;

namespace PlayerTrack.UserInterface.ViewModels.Mappers;

public static class PlayerViewMapper
{
    private const char MaleSymbol = '\u2642';
    private const char FemaleSymbol = '\u2640';
    private static string na = string.Empty;

    public static PlayerView MapPlayer(Player player)
    {
        na = ServiceContext.Localization.GetString("NotAvailable");
        var playerView = new PlayerView
        {
            Id = player.Id,
            Name = player.Name,
            PrimaryCategoryId = player.PrimaryCategoryId,
            PlayerConfig = player.PlayerConfig,
            HomeWorld = GetHomeWorld(player.WorldId),
            FreeCompany = GetFreeCompany(player.FreeCompany),
            LodestoneId = player.LodestoneId,
            Appearance = GetAppearance(player.Customize),
            FirstSeen = player.SeenCount != 0 && player.Created != 0 ? player.Created.ToTimeSpan() : na,
            LastSeen = player.SeenCount != 0 && player.LastSeen != 0 ? player.LastSeen.ToTimeSpan() : na,
            LastLocation = GetLastLocation(player.LastTerritoryType),
            SeenCount = player.SeenCount != 0 ? $"{player.SeenCount}x" : na,
            Notes = player.Notes,
            PreviousNames = PlayerChangeService.GetPreviousNames(player.Id, player.Name),
            PreviousWorlds = PlayerChangeService.GetPreviousWorlds(player.Id, GetHomeWorld(player.WorldId)),
        };

        AddTags(player.AssignedTags, playerView);
        AddCategories(player.AssignedCategories, playerView);
        AddEncounters(player.Id, playerView);

        return playerView;
    }

    public static string GetLastLocation(ushort lastTerritoryType)
    {
        var locationName = lastTerritoryType != 0
            ? DalamudContext.DataManager.Locations[lastTerritoryType].GetName()
            : null;
        return string.IsNullOrEmpty(locationName) ? na : locationName;
    }
    
    private static string GetHomeWorld(uint worldId)
    {
        var worldName = DalamudContext.DataManager.GetWorldNameById(worldId);
        return !string.IsNullOrEmpty(worldName) ? worldName : na;
    }

    private static string GetFreeCompany(KeyValuePair<FreeCompanyState, string> freeCompany)
    {
        switch (freeCompany.Key)
        {
            case FreeCompanyState.InFC:
                return freeCompany.Value;
            case FreeCompanyState.NotInFC:
                return ServiceContext.Localization.GetString("None");
            case FreeCompanyState.Unknown:
            default:
                return na;
        }
    }

    private static string GetAppearance(byte[]? customizeArr)
    {
        if (customizeArr is { Length: > 0 })
        {
            var customize = CharaCustomizeData.MapCustomizeData(customizeArr);
            var gender = customize.Gender;
            return gender switch
            {
                0 => $"{DalamudContext.DataManager.Races[customize.Race].MasculineName} {MaleSymbol}",
                1 => $"{DalamudContext.DataManager.Races[customize.Race].FeminineName} {FemaleSymbol}",
                _ => na,
            };
        }

        return na;
    }

    private static void AddTags(IReadOnlyCollection<Tag> assignedTags, PlayerView playerView)
    {
        var tags = ServiceContext.TagService.GetAllTags();
        if (tags.Any())
        {
            foreach (var tag in tags)
            {
                if (assignedTags.Any(t => t.Id == tag.Id))
                {
                    playerView.AssignedTags.Add(tag);
                }
                else
                {
                    playerView.UnassignedTags.Add(tag);
                }
            }
        }
    }

    private static void AddCategories(IReadOnlyCollection<Category> assignedCategories, PlayerView playerView)
    {
        var cats = ServiceContext.CategoryService.GetCategories();
        if (cats.Any())
        {
            foreach (var cat in cats)
            {
                if (assignedCategories.Any(c => c.Id == cat.Id))
                {
                    playerView.AssignedCategories.Add(cat);
                }
                else if (cat.SocialListId == 0)
                {
                    playerView.UnassignedCategories.Add(cat);
                }
            }
        }
    }

    private static void AddEncounters(int playerId, PlayerView playerView)
    {
        playerView.Encounters = new List<PlayerEncounterView>();
        var pEncs = PlayerEncounterService.GetPlayerEncountersByPlayer(playerId);
        if (pEncs == null)
        {
            return;
        }

        if (pEncs.Count > 0)
        {
            foreach (var pEnc in pEncs)
            {
                var enc = EncounterService.GetEncounter(pEnc.EncounterId);
                if (enc == null)
                {
                    continue;
                }

                var pEncView = new PlayerEncounterView
                {
                    Id = pEnc.Id,
                    Time = pEnc.Created.ToTimeSpan(),
                    Duration = pEnc.Ended == 0 ? (UnixTimestampHelper.CurrentTime() - pEnc.Created).ToDuration() : (pEnc.Ended - pEnc.Created).ToDuration(),
                    Job = DalamudContext.DataManager.ClassJobs[pEnc.JobId].Code,
                    Level = pEnc.JobLvl.ToString(),
                    Location = GetLastLocation(enc.TerritoryTypeId)
                };
                playerView.Encounters.Add(pEncView);
            }
        }
    }
}
