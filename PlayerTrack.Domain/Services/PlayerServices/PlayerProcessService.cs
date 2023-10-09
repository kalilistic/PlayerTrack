namespace PlayerTrack.Domain;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Helpers;

using Infrastructure;
using Models;

public class PlayerProcessService
{
    public event Action<Player>? PlayerSelected;

    public event Action<Player>? CurrentPlayerAdded;

    public event Action<Player>? CurrentPlayerRemoved;

    public static void HandleDuplicatePlayers(List<Player> players)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerMergeService.HandleDuplicatePlayers(): {players.Count}");
        if (players.Count < 2)
        {
            return;
        }

        var sortedPlayers = new List<Player>(
            players.OrderBy(p => p.LodestoneVerifiedOn)
                .ThenBy(p => p.Created)
                .ThenBy(p => p.Id));
        var oldestPlayer = sortedPlayers[0];
        var newestPlayers = sortedPlayers.Skip(1).ToList();

        foreach (var newPlayer in newestPlayers)
        {
            ServiceContext.PlayerDataService.MergePlayers(oldestPlayer, newPlayer.Id);
        }
    }

    public static void CheckForDuplicates() => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerMergeService.CheckForDuplicates()");
        var allPlayers = ServiceContext.PlayerDataService.GetAllPlayers();
        var groupedPlayers = allPlayers.Where(p => p.LodestoneId > 0)
            .GroupBy(p => p.LodestoneId);

        foreach (var group in groupedPlayers)
        {
            HandleDuplicatePlayers(group.ToList());
        }
    });

    public static void CheckForDuplicates(Player player) => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerMergeService.CheckForDuplicates(): {player.Id}");
        if (player.LodestoneId > 0)
        {
            var players = RepositoryContext.PlayerRepository.GetPlayersByLodestoneId(player.LodestoneId) ?? new List<Player>();
            HandleDuplicatePlayers(players);
        }
    });

    public static void CreateNewPlayer(string name, uint worldId)
    {
        var key = PlayerKeyBuilder.Build(name, worldId);
        var player = new Player
        {
            Key = key,
            Name = name,
            WorldId = worldId,
            Created = UnixTimestampHelper.CurrentTime(),
        };

        ServiceContext.PlayerDataService.AddPlayer(player);
    }

    public void RemoveCurrentPlayer(uint playerObjectId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerProcessService.RemoveCurrentPlayer(): {playerObjectId}");
        var player = ServiceContext.PlayerDataService.GetPlayer(playerObjectId);
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found.");
            return;
        }

        PlayerEncounterService.EndPlayerEncounter(player, ServiceContext.EncounterService.GetCurrentEncounter());

        player.IsCurrent = false;
        player.LastSeen = UnixTimestampHelper.CurrentTime();
        player.OpenPlayerEncounterId = 0;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        this.CurrentPlayerRemoved?.Invoke(player);
        ServiceContext.PlayerDataService.RecentPlayersExpiry.TryAdd(player.Id, UnixTimestampHelper.CurrentTime());
    }

    public void SelectPlayer(string name, string worldName)
    {
        var worldId = DalamudContext.DataManager.GetWorldIdByName(worldName);
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found.");
            return;
        }

        this.PlayerSelected?.Invoke(player);
    }

    public void RegisterCurrentPlayer(Player player) => this.CurrentPlayerAdded?.Invoke(player);

    public void AddOrUpdatePlayer(ToadPlayer toadPlayer, bool isCurrent = true, bool forceLoad = false)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerProcessService.AddOrUpdatePlayer(): {toadPlayer.Id}, {toadPlayer.Name}, {toadPlayer.HomeWorld}, {forceLoad}");
        var enc = ServiceContext.EncounterService.GetCurrentEncounter();

        if (enc == null)
        {
            DalamudContext.PluginLog.Verbose("Encounter is missing.");
            return;
        }

        if (!enc.SavePlayers && !forceLoad)
        {
            DalamudContext.PluginLog.Verbose("Encounter is not set to save players.");
            return;
        }

        var key = PlayerKeyBuilder.Build(toadPlayer.Name, toadPlayer.HomeWorld);
        var player = ServiceContext.PlayerDataService.GetPlayer(key);
        var loc = PlayerEncounterService.GetEncounterLocation();

        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found, creating new player.");
            this.CreateNewPlayer(toadPlayer, key, isCurrent, enc.CategoryId, loc);
            player = ServiceContext.PlayerDataService.GetPlayer(key);
            if (player != null)
            {
                if (forceLoad)
                {
                    this.PlayerSelected?.Invoke(player);
                }
                else if (enc.SaveEncounter)
                {
                    PlayerEncounterService.CreatePlayerEncounter(toadPlayer, player);
                }
            }
        }
        else if (forceLoad)
        {
            DalamudContext.PluginLog.Verbose("Force load player, used for player search.");
            player = this.UpdateExistingPlayer(player, toadPlayer, isCurrent, loc);
            this.PlayerSelected?.Invoke(player);
        }
        else if (!player.IsCurrent)
        {
            DalamudContext.PluginLog.Verbose("Player found, updating existing player.");
            player = this.UpdateExistingPlayer(player, toadPlayer, isCurrent, loc);
            ServiceContext.PlayerAlertService.SendProximityAlert(player);
            if (enc.SaveEncounter)
            {
                PlayerEncounterService.CreatePlayerEncounter(toadPlayer, player);
            }
        }
    }

    private void CreateNewPlayer(ToadPlayer toadPlayer, string key, bool isCurrent, int categoryId, ToadLocation loc)
    {
        var player = new Player
        {
            Key = key,
            ObjectId = toadPlayer.Id,
            Name = toadPlayer.Name,
            WorldId = toadPlayer.HomeWorld,
            PrimaryCategoryId = categoryId,
            FreeCompany = PlayerFCHelper.CheckFreeCompany(toadPlayer.CompanyTag, loc.InContent()),
            Customize = toadPlayer.Customize,
            LastTerritoryType = loc.TerritoryId,
            Created = UnixTimestampHelper.CurrentTime(),
            IsCurrent = isCurrent,
            IsRecent = isCurrent,
            LastSeen = isCurrent ? UnixTimestampHelper.CurrentTime() : 0,
            SeenCount = isCurrent ? 1 : 0,
        };

        ServiceContext.PlayerDataService.AddPlayer(player);
        this.CurrentPlayerAdded?.Invoke(player);
        var playerId = ServiceContext.PlayerDataService.GetPlayer(player.Key)?.Id;
        if (playerId != null)
        {
            ServiceContext.PlayerDataService.RecentPlayersExpiry.TryRemove(player.Id, out _);
        }
    }

    private Player UpdateExistingPlayer(Player player, ToadPlayer toadPlayer, bool isCurrent, ToadLocation loc)
    {
        if (isCurrent)
        {
            if (player.Customize != null && !StructuralComparisons.StructuralEqualityComparer.Equals(
                    player.Customize, toadPlayer.Customize))
            {
                PlayerChangeService.AddCustomizeHistory(player.Id, player.Customize);
            }

            player.Customize = toadPlayer.Customize;
            player.FreeCompany = PlayerFCHelper.CheckFreeCompany(toadPlayer.CompanyTag, player.FreeCompany, loc.InContent());
            player.ObjectId = toadPlayer.Id;
            player.SeenCount += 1;
            player.LastTerritoryType = loc.TerritoryId;
            player.LastSeen = UnixTimestampHelper.CurrentTime();
            player.IsCurrent = true;
            player.IsRecent = true;
            ServiceContext.PlayerDataService.UpdatePlayer(player);
        }

        player = ServiceContext.PlayerDataService.GetPlayer(player.Id) ?? player;
        this.CurrentPlayerAdded?.Invoke(player);
        ServiceContext.PlayerDataService.RecentPlayersExpiry.TryRemove(player.Id, out _);
        return player;
    }
}
