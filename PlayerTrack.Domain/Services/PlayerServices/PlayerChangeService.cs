using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class PlayerChangeService
{
    public static void AddNameWorldHistory(int playerId, string playerName, uint worldId) => RepositoryContext.PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistory(new PlayerNameWorldHistory
    {
        PlayerId = playerId,
        PlayerName = playerName,
        WorldId = worldId,
    });

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
            return new List<PlayerNameWorldHistory>();
        }
        return nameWorldHistories.ToList();
    }
}
