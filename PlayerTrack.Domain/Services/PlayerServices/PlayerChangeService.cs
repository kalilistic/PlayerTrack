using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class PlayerChangeService
{
    public static void UpdatePlayerId(int oldestPlayerId, int newPlayerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.UpdatePlayerId(): {oldestPlayerId}, {newPlayerId}");
        RepositoryContext.PlayerNameWorldHistoryRepository.UpdatePlayerId(oldestPlayerId, newPlayerId);
        RepositoryContext.PlayerCustomizeHistoryRepository.UpdatePlayerId(oldestPlayerId, newPlayerId);
    }
    
    public static void HandleNameWorldChange(Player player, string playerName, uint worldId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.HandleNameWorldChange(): {player.Name}@{player.WorldId}, {playerName}@{worldId}");
        if (!player.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase) || player.WorldId != worldId)
        {
            RepositoryContext.PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistory(new PlayerNameWorldHistory
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                WorldId = player.WorldId,
            });
        }
    }

    public static void HandleNameWorldChange(Player oldestPlayer, Player newPlayer)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.HandleNameWorldChange(): {oldestPlayer.Name}, {newPlayer.Name}");
        var nameChanged = !oldestPlayer.Name.Equals(newPlayer.Name, StringComparison.OrdinalIgnoreCase);
        var worldChanged = oldestPlayer.WorldId != newPlayer.WorldId;

        if (nameChanged || worldChanged)
        {
            RepositoryContext.PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistory(new PlayerNameWorldHistory
            {
                PlayerId = oldestPlayer.Id,
                PlayerName = oldestPlayer.Name,
                WorldId = oldestPlayer.WorldId,
            });
        }
    }

    public static void HandleCustomizeChange(Player oldestPlayer, Player newPlayer)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.HandleCustomizeChange(): {oldestPlayer.Name}, {newPlayer.Name}");
        if (oldestPlayer.Customize != newPlayer.Customize && oldestPlayer.Customize != null)
        {
            RepositoryContext.PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistory(new PlayerCustomizeHistory
            {
                PlayerId = oldestPlayer.Id,
                Customize = oldestPlayer.Customize,
            });
        }
    }

    public static void AddCustomizeHistory(int playerId, byte[] playerCustomize) => RepositoryContext.PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistory(new PlayerCustomizeHistory
    {
        PlayerId = playerId,
        Customize = playerCustomize,
    });

    public static void DeleteCustomizeHistory(int playerId) => RepositoryContext.PlayerCustomizeHistoryRepository.DeleteCustomizeHistory(playerId);

    public static void DeleteNameWorldHistory(int playerId) => RepositoryContext.PlayerNameWorldHistoryRepository.DeleteNameWorldHistory(playerId);

    public static string GetPreviousNames(int playerId, string currentName)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.GetPreviousNames(): {playerId}, {currentName}");
        var names = RepositoryContext.PlayerNameWorldHistoryRepository.GetHistoricalNames(playerId);
        if (names == null)
        {
            return string.Empty;
        }

        var uniqueNames = names
            .Distinct()
            .Where(name => !string.IsNullOrEmpty(name) && !string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return uniqueNames.Any() ? string.Join(", ", uniqueNames) : string.Empty;
    }

    public static string GetPreviousWorlds(int playerId, string currentWorldName)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.GetPreviousWorlds(): {playerId}, {currentWorldName}");
        var worldIds = RepositoryContext.PlayerNameWorldHistoryRepository.GetHistoricalWorlds(playerId);
        if (worldIds == null)
        {
            return string.Empty;
        }

        var worldNames = worldIds
            .Where(worldId => worldId != 0)
            .Select(worldId => DalamudContext.DataManager.GetWorldNameById(worldId))
            .Distinct()
            .Where(worldName => !string.IsNullOrEmpty(worldName) && !string.Equals(worldName, currentWorldName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return worldNames.Any() ? string.Join(", ", worldNames) : string.Empty;
    }

    public static List<PlayerNameWorldHistory> GetPlayerNameWorldHistories(IEnumerable<int> playerIds)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerChangeService.GetPlayerNameWorldHistories()");
        var nameWorldHistories = RepositoryContext.PlayerNameWorldHistoryRepository.GetPlayerNameWorldHistories(playerIds.ToArray());
        if (nameWorldHistories == null)
        {
            return new List<PlayerNameWorldHistory> { };
        }
        return nameWorldHistories.ToList();
    }
}
