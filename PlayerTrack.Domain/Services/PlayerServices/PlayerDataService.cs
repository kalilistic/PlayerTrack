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
using Newtonsoft.Json;

public class PlayerDataService
{
    public Action<Player>? PlayerUpdated;
    private const long NinetyDaysInMilliseconds = 7776000000;
    private const int MaxBatchSize = 500;

    public Player? GetPlayer(int playerId) => ServiceContext.PlayerCacheService.GetPlayer(playerId);

    public Player? GetPlayer(string name, uint worldId) => ServiceContext.PlayerCacheService.GetPlayer(name, worldId);

    public Player? GetPlayer(string playerKey) => ServiceContext.PlayerCacheService.GetPlayer(playerKey);

    public Player? GetPlayer(uint playerObjectId) => ServiceContext.PlayerCacheService.GetPlayer(playerObjectId);

    public IEnumerable<Player> GetAllPlayers() => ServiceContext.PlayerCacheService.GetPlayers();

    public void DeletePlayer(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.DeletePlayer(): {playerId}");
        PlayerChangeService.DeleteCustomizeHistory(playerId);
        PlayerChangeService.DeleteNameWorldHistory(playerId);
        PlayerCategoryService.DeletePlayerCategoryByPlayerId(playerId);
        PlayerConfigService.DeletePlayerConfig(playerId);
        PlayerTagService.DeletePlayerTagsByPlayerId(playerId);
        PlayerLodestoneService.DeleteLookupsByPlayer(playerId);
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
        player.Id = RepositoryContext.PlayerRepository.CreatePlayer(player);
        player.PlayerConfig.PlayerId = player.Id;
        ServiceContext.PlayerCacheService.AddPlayer(player);
        PlayerLodestoneService.CreateLodestoneLookup(player.Id, player.Name, player.WorldId);
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
                DalamudContext.PluginLog.Debug(ex, "PlayerDataService.RecalculatePlayerRankings()");
            }
        });
    }

    public void MergePlayers(Player oldestPlayer, int newPlayerId)
    {
        DalamudContext.PluginLog.Verbose($"PlayerDataService.MergePlayer(): {oldestPlayer.Id} -> {newPlayerId}");
        var newPlayer = ServiceContext.PlayerCacheService.GetPlayer(newPlayerId);
        if (newPlayer == null)
        {
            return;
        }

        // save state before changing
        var oldestPlayerString = JsonConvert.SerializeObject(oldestPlayer);
        var newPlayerString = JsonConvert.SerializeObject(newPlayer);
        var isCurrent = newPlayer.IsCurrent;
        var payloads = ServiceContext.PlayerAlertService.CreatePlayerNameWorldChangeAlert(oldestPlayer, newPlayer);

        // remove players from cache
        ServiceContext.PlayerProcessService.RemoveCurrentPlayer(newPlayer.ObjectId);
        ServiceContext.PlayerCacheService.RemovePlayer(oldestPlayer);
        ServiceContext.PlayerCacheService.RemovePlayer(newPlayer);

        // create records
        PlayerChangeService.HandleNameWorldChange(oldestPlayer, newPlayer);
        PlayerChangeService.HandleCustomizeChange(oldestPlayer, newPlayer);

        // re-parent records
        PlayerChangeService.UpdatePlayerId(newPlayer.Id, oldestPlayer.Id);
        PlayerEncounterService.UpdatePlayerId(newPlayer.Id, oldestPlayer.Id);

        // delete records
        PlayerConfigService.DeletePlayerConfig(newPlayer.Id);
        PlayerCategoryService.DeletePlayerCategoryByPlayerId(newPlayer.Id);
        PlayerTagService.DeletePlayerTagsByPlayerId(newPlayer.Id);
        PlayerLodestoneService.DeleteLookupsByPlayer(newPlayer.Id);
        RepositoryContext.PlayerRepository.DeletePlayer(newPlayer.Id);

        // merge data into original
        oldestPlayer.Merge(newPlayer);

        // add to current players if needed
        oldestPlayer.IsCurrent = isCurrent;
        if (oldestPlayer.IsCurrent)
        {
            ServiceContext.PlayerProcessService.RegisterCurrentPlayer(oldestPlayer);
        }
        
        // update player in repo & cache
        RepositoryContext.PlayerRepository.UpdatePlayer(oldestPlayer);
        ServiceContext.PlayerCacheService.AddPlayer(oldestPlayer);
        
        // send alert
        if (!payloads.Any())
        {
            DalamudContext.PluginLog.Warning("Skipping empty alert for name/world change.");
            DalamudContext.PluginLog.Warning($"Oldest Player: {oldestPlayerString}");
            DalamudContext.PluginLog.Warning($"New Player: {newPlayerString}");
            return;
        }

        PlayerAlertService.SendNameWorldChangeAlert(payloads);
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
