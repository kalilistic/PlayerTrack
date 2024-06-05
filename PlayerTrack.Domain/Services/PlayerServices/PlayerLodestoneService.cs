using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Helpers;
using PlayerTrack.Domain.Common;

namespace PlayerTrack.Domain;

using System.Diagnostics;
using Dalamud.DrunkenToad.Core;

using Infrastructure;
using Models;

public class PlayerLodestoneService
{
    public static void CreateBatchLookup(Player player)
    {
        try
        {
            DalamudContext.PluginLog.Verbose($"Entering LodestoneService.CreateLodestoneLookup(): {player.Name}@{player.WorldId}");
        
            if (IsMissingNameOrWorld(player))
            {
                DalamudContext.PluginLog.Warning($"Skipping lodestone lookup since missing name or world: {player.Name}@{player.WorldId}");
                return;
            }
        
            if (HasOpenLookup(player))
            {
                DalamudContext.PluginLog.Warning($"Skipping lodestone lookup since already in-progress: {player.Name}@{player.WorldId}");
                return;
            }
        
            if (IsTestDC(player))
            {
                DalamudContext.PluginLog.Verbose($"Skipping lodestone lookup for test DC: {player.Name}@{player.WorldId}");
                player.LodestoneStatus = LodestoneStatus.NotApplicable;
                player.LodestoneVerifiedOn = UnixTimestampHelper.CurrentTime();
                ServiceContext.PlayerDataService.UpdatePlayer(player);
                return;
            }
            
            var lookup = new LodestoneLookup
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                WorldId = player.WorldId,
                LodestoneLookupType = LodestoneLookupType.Batch,
            };
            
            lookup.SetLodestoneId(player.LodestoneId);
            
            RepositoryContext.LodestoneRepository.CreateLodestoneLookup(lookup);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to create lodestone lookup for player: {player.Name}@{player.WorldId}");
        }
    }
    
    public static int CreateRefreshLookup(int playerId)
    {
        try
        {
            DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.RefreshLodestone(): {playerId}");
        
            var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
            if (player == null)
            {
                DalamudContext.PluginLog.Warning($"Skipping lodestone lookup refresh since player not found: {playerId}");
                return 0;
            }

            if (player.LodestoneId == 0)
            {
                DalamudContext.PluginLog.Warning($"Cannot refresh lodestone lookup for player without lodestone id: {player.Name}@{player.WorldId}");
                return 0;
            }
            
            var mostRecentLookup = RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPlayerId(player.Id).MaxBy(lookup => lookup.Updated);
            if (mostRecentLookup is { LodestoneLookupType: LodestoneLookupType.Refresh } && !NameWorldChanged(player, mostRecentLookup))
            {
                DalamudContext.PluginLog.Warning($"Already refreshed player recently, skipping: {player.Name}@{player.WorldId}");
                return 0;
            }
        
            var existingLookups = RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPlayerId(player.Id);
            foreach (var lookup in existingLookups)
            {
                if (!lookup.IsDone)
                {
                    DalamudContext.PluginLog.Debug($"Cancelled in-progress lookup for refresh: {lookup.PlayerName}@{lookup.WorldId}");
                    lookup.SetLodestoneStatus(LodestoneStatus.Cancelled);
                    player.LodestoneStatus = LodestoneStatus.Cancelled;
                    UpdatePlayerAndLookup(player, lookup);
                }
            }

            var newLookup = new LodestoneLookup
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                WorldId = player.WorldId,
                LodestoneLookupType = LodestoneLookupType.Refresh
            };
            
            newLookup.SetLodestoneId(player.LodestoneId);
            
            return RepositoryContext.LodestoneRepository.CreateLodestoneLookup(newLookup);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to create lodestone lookup refresh for player: {playerId}");
            return 0;
        }
    }

    public static List<LodestoneLookup> GetLodestoneLookups()
    {
        var allLookups = RepositoryContext.LodestoneRepository.GetAllLodestoneLookups();
        return allLookups == null ? new List<LodestoneLookup>() : allLookups.OrderByDescending(lookup => lookup.Updated).ToList();
    }

    public static void DeleteLookupsByPlayer(int playerId)
    {
        var lookups = RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPlayerId(playerId);
        foreach (var lookup in lookups) RemoveLookupAsPreReq(lookup);
        RepositoryContext.LodestoneRepository.DeleteLodestoneRequestByPlayerId(playerId);
    }
    
    public static void UpdatePlayerFromLodestone(LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.UpdateLodestone(): {lookup.PlayerId}, {lookup.LodestoneId}, {lookup.LodestoneStatus}");
        var player = ServiceContext.PlayerDataService.GetPlayer(lookup.PlayerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning($"Skipping lodestone lookup update since player not found: {lookup.PlayerId}");
            return;
        }
        
        HandleCommonPlayerUpdates(player, lookup);
        HandleVerifiedPlayerUpdates(player, lookup);
        UpdatePlayerAndLookup(player, lookup);
        PlayerProcessService.CheckForDuplicates(player);
    }

    public static void OpenLodestoneProfile(uint lodestoneId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.OpenLodestoneProfile(): {lodestoneId}");
        if (lodestoneId == 0)
        {
            DalamudContext.PluginLog.Warning("LodestoneId is 0, cannot open lodestone profile.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://" + ServiceContext.ConfigService.GetConfig().LodestoneLocale +
                       ".finalfantasyxiv.com/lodestone/character/" + lodestoneId,
            UseShellExecute = true,
        });
    }
    
    public static void UpdatePlayerId(int playerId1, int playerId2)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.UpdatePlayerId(): {playerId1}, {playerId2}");
        var lookups = RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPlayerId(playerId1);
        if (lookups.Count == 0) return; 
        foreach (var lookup in lookups)
        {
            lookup.PlayerId = playerId2;
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
        }
    }
    
    private static void RemoveLookupAsPreReq(LodestoneLookup lookup)
    {
        var dependentLookups = RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPrerequisiteLookupId(lookup.Id);
        foreach (var dependentLookup in dependentLookups)
        {
            dependentLookup.PrerequisiteLookupId = null;
            dependentLookup.SetLodestoneStatus(LodestoneStatus.Verified);
            RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(dependentLookup);
            UpdatePlayerFromLodestone(dependentLookup);
        }
    }

    private static void HandleCommonPlayerUpdates(Player player, LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.ApplyCommonLookupFieldsToPlayer(): {lookup.PlayerId}, {lookup.LodestoneId}, {lookup.LodestoneStatus}");
        player.LodestoneStatus = lookup.LodestoneStatus;
        player.LodestoneVerifiedOn = lookup.Updated;
        if (player.LodestoneId == 0 && lookup.LodestoneId != 0)
        {
            DalamudContext.PluginLog.Verbose($"Player lodestone id was 0, updating to lookup lodestone id: {lookup.UpdatedPlayerName}@{lookup.UpdatedWorldId}");
            player.LodestoneId = lookup.LodestoneId;
        }
    }
    
    private static bool NameWorldChanged(Player player, LodestoneLookup lookup)
    {
        if (string.IsNullOrEmpty(lookup.UpdatedPlayerName) || lookup.UpdatedWorldId == 0)
        {
            DalamudContext.PluginLog.Warning($"Name or world is missing on lodestone lookup, skipping: {lookup.Id}");
            return false;
        }
        return player.Name != lookup.UpdatedPlayerName || player.WorldId != lookup.UpdatedWorldId;
    }
    
    private static void UpdatePlayerAndLookup(Player player, LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.UpdatePlayerAndLodestone(): " +
                                         $"{player.Name}@{player.WorldId}, " +
                                         $"{lookup.UpdatedPlayerName}@{lookup.UpdatedWorldId}, " +
                                         $"{lookup.LodestoneStatus}");
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
        if (lookup.LodestoneStatus == LodestoneStatus.Verified)
        {
            RemoveLookupAsPreReq(lookup);
            ServiceContext.PlayerDataService.RefreshAllPlayers();
        }
    }

    private static Player? getExistingPlayerWithNameWorld(int playerId, string playerName, uint worldId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.IsNameWorldInUse(): {playerId}, {playerName}@{worldId}");
        var existingPlayer = ServiceContext.PlayerDataService.GetPlayer(playerName, worldId);
        if (existingPlayer == null || existingPlayer.Id == playerId) return null;
        return existingPlayer;
    }

    private static void HandleVerifiedPlayerUpdates(Player player, LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.ApplyVerifiedLookupFieldsToPlayer(): {lookup.Id}, {lookup.PlayerId}, {lookup.LodestoneId}, {lookup.LodestoneStatus}");
        if (lookup.LodestoneStatus == LodestoneStatus.Verified)
        {
            if (NameWorldChanged(player, lookup))
            {
                DalamudContext.PluginLog.Debug($"Player name and world don't match on lodestone lookup, need to update: {player.Name}@{player.WorldId} -> {lookup.UpdatedPlayerName}@{lookup.UpdatedWorldId}");
                var existingPlayer = getExistingPlayerWithNameWorld(player.Id, lookup.UpdatedPlayerName, lookup.UpdatedWorldId);
                if (existingPlayer != null)
                {
                    DalamudContext.PluginLog.Debug($"Player name and world on lodestone lookup already exist, need to refresh original player: {existingPlayer.Name}@{existingPlayer.WorldId}");
                    var prerequisiteLookupId = CreateRefreshLookup(existingPlayer.Id);
                    if (prerequisiteLookupId == 0)
                    {
                        DalamudContext.PluginLog.Warning($"Failed to refresh create dependent player lookup which shouldn't happen, cancelling: {player.Name}@{player.WorldId}");
                        player.LodestoneStatus = LodestoneStatus.Cancelled;
                        lookup.SetLodestoneStatus(LodestoneStatus.Cancelled);
                    }
                    else
                    {
                        DalamudContext.PluginLog.Debug($"Setting pre-requisite for lookup {lookup.Id} to {prerequisiteLookupId}");
                        player.LodestoneStatus = LodestoneStatus.Blocked;
                        lookup.SetLodestoneStatus(LodestoneStatus.Blocked);
                        lookup.PrerequisiteLookupId = prerequisiteLookupId;
                    }
                    
                }
                else
                {
                    if (!string.IsNullOrEmpty(player.Name) && player.WorldId != 0)
                    {
                        PlayerChangeService.HandleNameWorldChange(player, lookup.UpdatedPlayerName, lookup.UpdatedWorldId);
                        player.Name = lookup.UpdatedPlayerName;
                        player.WorldId = lookup.UpdatedWorldId;
                        player.Key = PlayerKeyBuilder.Build(player.Name, player.WorldId);
                    }
                }
            }
            else
            {
                DalamudContext.PluginLog.Verbose($"Player name and world match on lodestone lookup, so can do a simple update: {player.Name}@{player.WorldId}");
            }
        }
    }
    
    private static bool HasOpenLookup(Player player) => RepositoryContext.LodestoneRepository.GetLodestoneLookupsByPlayerId(player.Id).Any(lookup => !lookup.IsDone);
    private static bool IsTestDC(Player player) => DalamudContext.DataManager.IsTestDC(player.WorldId);
    private static bool IsMissingNameOrWorld(Player player) => string.IsNullOrEmpty(player.Name) || player.WorldId == 0;
}
