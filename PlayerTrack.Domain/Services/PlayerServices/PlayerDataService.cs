using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

// ReSharper disable MemberCanBeMadeStatic.Global
namespace PlayerTrack.Domain;

using System;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Helpers;

public class PlayerDataService
{
    public Action<Player>? PlayerUpdated;
    private const long NinetyDaysInMilliseconds = 7776000000;
    private const int MaxBatchSize = 500;

    public IEnumerable<Player> GetAllPlayers() => ServiceContext.PlayerCacheService.GetPlayers();
    
    public Player? GetPlayer(int playerId) => ServiceContext.PlayerCacheService.GetPlayer(playerId);
    
    public Player? GetPlayer(ulong contentId)
    {
        return contentId == 0 ? null : ServiceContext.PlayerCacheService.GetPlayer(contentId);
    }

    public Player? GetPlayer(uint entityId)
    {
        return ServiceContext.PlayerCacheService.GetPlayer(entityId);
    }

    public Player? GetPlayer(string name, uint worldId)
    {
        var players = ServiceContext.PlayerCacheService.GetPlayers(name, worldId);
        return players.Count switch
        {
            0 => null,
            1 => players.First(),
            _ => players.OrderByDescending(p => p.Created).First()
        };
    }
    
    public Player? GetPlayer(ulong contentId, string name, uint worldId)
    {
        if (contentId != 0)
        {
            var playerFromContentId = GetPlayer(contentId);
            if (playerFromContentId != null)
            {
                return playerFromContentId;
            }
            var playerFromNameWorldId = GetPlayer(name, worldId);
            return playerFromNameWorldId?.ContentId == 0 ? playerFromNameWorldId : null;
        }
    
        return GetPlayer(name, worldId);
    }
    
    public void DeletePlayer(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.DeletePlayer(): {playerId}");
        PlayerChangeService.DeleteCustomizeHistory(playerId);
        PlayerChangeService.DeleteNameWorldHistory(playerId);
        PlayerCategoryService.DeletePlayerCategoryByPlayerId(playerId);
        PlayerConfigService.DeletePlayerConfig(playerId);
        PlayerTagService.DeletePlayerTagsByPlayerId(playerId);
        PlayerEncounterService.DeletePlayerEncountersByPlayer(playerId);
        RepositoryContext.PlayerRepository.DeletePlayer(playerId);
        ServiceContext.PlayerCacheService.RemovePlayer(playerId);
    }

    public void UpdatePlayer(Player player)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.UpdatePlayer(): {player.Id}");
        ServiceContext.PlayerCacheService.UpdatePlayer(player);
        RepositoryContext.PlayerRepository.UpdatePlayer(player);
        this.PlayerUpdated?.Invoke(player);
    }

    public void AddPlayer(Player player)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.AddPlayer(): {player.Id}");
        player.Id = RepositoryContext.PlayerRepository.CreatePlayer(player, player.ContentId);
        player.PlayerConfig.PlayerId = player.Id;
        ServiceContext.PlayerCacheService.AddPlayer(player);
        if (player.PrimaryCategoryId != 0)
        {
            PlayerCategoryService.AssignCategoryToPlayer(player.Id, player.PrimaryCategoryId);
        }
        else
        {
            DalamudContext.PluginLog.Verbose($"PlayerDataService.AddPlayer(): No category assigned to player: {player.Id}");
        }
    }
    
    public void ClearCategoryFromPlayers(int categoryId)
    {
        ServiceContext.PlayerCacheService.RemoveCategory(categoryId);
    }

    public void RefreshAllPlayers()
    {
        DalamudContext.PluginLog.Verbose("PlayerDataService.RefreshAllPlayers()");
        Task.Run(ServiceContext.PlayerCacheService.LoadPlayers);
    }

    public void RecalculatePlayerRankings()
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.RecalculatePlayerRankings()");
        Task.Run(() =>
        {
            try
            {
                ServiceContext.PlayerCacheService.Resort();
            }
            catch (Exception ex)
            {
                DalamudContext.PluginLog.Verbose(ex, "PlayerDataService.RecalculatePlayerRankings()");
            }
        });
    }
    
    public void DeletePlayers()
    {
        var players = this.GetPlayersForDeletion();
        var playerIds = players.Select(p => p.Id).ToList();

        for (var i = 0; i < playerIds.Count; i += MaxBatchSize)
        {
            var currentBatch = playerIds.Skip(i).Take(MaxBatchSize).ToList();
            RepositoryContext.PlayerRepository.DeletePlayersWithRelations(currentBatch);
        }

        RepositoryContext.RunMaintenanceChecks(true);
        this.RefreshAllPlayers();
    }

    public void DeletePlayerConfigs()
    {
        var playerConfigs = this.GetPlayerConfigsForDeletion();
        var playerConfigIds = playerConfigs.Select(p => p.Id).ToList();

        for (var i = 0; i < playerConfigIds.Count; i += MaxBatchSize)
        {
            var currentBatch = playerConfigIds.Skip(i).Take(MaxBatchSize).ToList();
            RepositoryContext.PlayerConfigRepository.DeletePlayerConfigs(currentBatch);
        }

        RepositoryContext.RunMaintenanceChecks(true);
        this.RefreshAllPlayers();
    }

    public void UpdatePlayerNotes(int playerId, string notes)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.UpdatePlayerNotes(): {playerId}");
        var player = ServiceContext.PlayerCacheService.GetPlayer(playerId);
        if (player == null)
        {
            return;
        }

        player.Notes = notes;
        RepositoryContext.PlayerRepository.UpdatePlayer(player);
        ServiceContext.PlayerCacheService.UpdatePlayer(player);
    }
    
    public void MergePlayers(Player playerToDelete, Player playerToUpdate)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.MergePlayer(): {playerToDelete.Id} into {playerToUpdate.Id}");

        // save state before changing
        var isCurrent = playerToDelete.IsCurrent;

        // remove players from cache
        ServiceContext.PlayerProcessService.RemoveCurrentPlayer(playerToDelete.EntityId);
        ServiceContext.PlayerCacheService.RemovePlayer(playerToUpdate.Id);
        ServiceContext.PlayerCacheService.RemovePlayer(playerToDelete.Id);

        // handle name/world change
        if (playerToDelete.Name != playerToUpdate.Name || playerToDelete.WorldId != playerToUpdate.WorldId)
        {
            var name = playerToDelete.LastSeen > playerToUpdate.LastSeen ? playerToUpdate.Name : playerToDelete.Name;
            var worldId = playerToDelete.LastSeen > playerToUpdate.LastSeen ? playerToUpdate.WorldId : playerToDelete.WorldId;
            PlayerChangeService.AddNameWorldHistory(playerToUpdate.Id, name, worldId);
        }
        
        // handle customize change
        if (playerToDelete.Customize != null && playerToUpdate.Customize != null &&
            !StructuralComparisons.StructuralEqualityComparer.Equals(playerToDelete.Customize, playerToUpdate.Customize))
        {
            var customize = playerToDelete.LastSeen > playerToUpdate.LastSeen ? playerToUpdate.Customize : playerToDelete.Customize;
            PlayerChangeService.AddCustomizeHistory(playerToUpdate.Id, customize);
        }

        // re-parent records
        PlayerChangeService.UpdatePlayerId(playerToDelete.Id, playerToUpdate.Id);
        PlayerEncounterService.UpdatePlayerId(playerToDelete.Id, playerToUpdate.Id);
        
        // assign categories not already assigned
        foreach (var category in playerToDelete.AssignedCategories)
        {
            if (playerToUpdate.AssignedCategories.All(c => c.Id != category.Id))
            {
                playerToUpdate.AssignedCategories.Add(category);
                RepositoryContext.PlayerCategoryRepository.CreatePlayerCategory(playerToUpdate.Id, category.Id);
            }
        }
        
        // assign tags not already assigned
        foreach (var tag in playerToDelete.AssignedTags)
        {
            if (playerToUpdate.AssignedTags.All(t => t.Id != tag.Id))
            {
                playerToUpdate.AssignedTags.Add(tag);
                RepositoryContext.PlayerTagRepository.CreatePlayerTag(playerToUpdate.Id, tag.Id);
            }
        }

        // delete records
        PlayerConfigService.DeletePlayerConfig(playerToDelete.Id);
        PlayerCategoryService.DeletePlayerCategoryByPlayerId(playerToDelete.Id);
        PlayerTagService.DeletePlayerTagsByPlayerId(playerToDelete.Id);
        RepositoryContext.PlayerRepository.DeletePlayer(playerToDelete.Id);

        // merge player data
        playerToUpdate.Merge(playerToDelete);

        // add to current players if needed
        playerToUpdate.IsCurrent = isCurrent;
        if (playerToUpdate.IsCurrent)
        {
            ServiceContext.PlayerProcessService.RegisterCurrentPlayer(playerToUpdate);
        }

        // update player in repo & cache
        RepositoryContext.PlayerRepository.UpdatePlayer(playerToUpdate);
        ServiceContext.PlayerCacheService.AddPlayer(playerToUpdate);
    }

    private List<Player> GetPlayersForDeletion()
    {
        var playersWithEncounters = RepositoryContext.PlayerEncounterRepository.GetPlayersWithEncounters();
        var currentTimeUnix = UnixTimestampHelper.CurrentTime();
        var options = ServiceContext.ConfigService.GetConfig().PlayerDataActionOptions;
        return ServiceContext.PlayerCacheService.GetPlayers(p =>
            (!options.KeepPlayersWithNotes || string.IsNullOrEmpty(p.Notes)) &&
            (!options.KeepPlayersWithCategories || !p.AssignedCategories.Any()) &&
            (!options.KeepPlayersWithAnySettings || p.PlayerConfig.Id == 0) &&
            (!options.KeepPlayersWithEncounters || !playersWithEncounters.Contains(p.Id)) &&
            (!options.KeepPlayersSeenInLast90Days || currentTimeUnix - p.LastSeen > NinetyDaysInMilliseconds) &&
            (!options.KeepPlayersVerifiedOnLodestone || p.LodestoneStatus != LodestoneStatus.Verified));
    }

    private List<PlayerConfig> GetPlayerConfigsForDeletion()
    {
        var playersWithEncounters = RepositoryContext.PlayerEncounterRepository.GetPlayersWithEncounters();
        var currentTimeUnix = UnixTimestampHelper.CurrentTime();
        var options = ServiceContext.ConfigService.GetConfig().PlayerSettingsDataActionOptions;
        return ServiceContext.PlayerCacheService.GetPlayers(p =>
            p.PlayerConfig.Id != 0 &&
            (!options.KeepSettingsForPlayersWithNotes || string.IsNullOrEmpty(p.Notes)) &&
            (!options.KeepSettingsForPlayersWithCategories || !p.AssignedCategories.Any()) &&
            (!options.KeepSettingsForPlayersWithAnySettings || p.PlayerConfig.Id == 0) &&
            (!options.KeepSettingsForPlayersWithEncounters || !playersWithEncounters.Contains(p.Id)) &&
            (!options.KeepSettingsForPlayersSeenInLast90Days || currentTimeUnix - p.LastSeen > NinetyDaysInMilliseconds) &&
            (!options.KeepSettingsForPlayersVerifiedOnLodestone || p.LodestoneStatus != LodestoneStatus.Verified)).Select(p => p.PlayerConfig).ToList();
    }

    public void DeleteHistory(int playerId)
    {
        PlayerChangeService.DeleteCustomizeHistory(playerId);
        PlayerChangeService.DeleteNameWorldHistory(playerId);
        RefreshAllPlayers();
    }
}
