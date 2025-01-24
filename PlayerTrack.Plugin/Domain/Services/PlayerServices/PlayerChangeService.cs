using System;
using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class PlayerChangeService
{
    public static void UpdatePlayerId(int originalPlayerId, int newPlayerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.UpdatePlayerId(): {originalPlayerId}, {newPlayerId}");
        RepositoryContext.PlayerNameWorldHistoryRepository.UpdatePlayerId(originalPlayerId, newPlayerId);
        RepositoryContext.PlayerCustomizeHistoryRepository.UpdatePlayerId(originalPlayerId, newPlayerId);
    }

    public static void AddNameWorldHistory(int playerId, string playerName, uint worldId) =>
        RepositoryContext.PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistory(new PlayerNameWorldHistory
        {
            PlayerId = playerId,
            PlayerName = playerName,
            WorldId = worldId,
        });

    public static void AddCustomizeHistory(int playerId, byte[] playerCustomize) =>
        RepositoryContext.PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistory(new PlayerCustomizeHistory
        {
            PlayerId = playerId,
            Customize = playerCustomize,
        });

    public static void DeleteCustomizeHistory(int playerId) =>
        RepositoryContext.PlayerCustomizeHistoryRepository.DeleteCustomizeHistory(playerId);

    public static void DeleteNameWorldHistory(int playerId) =>
        RepositoryContext.PlayerNameWorldHistoryRepository.DeleteNameWorldHistory(playerId);

    public static string GetPreviousNames(int playerId, string currentName)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.GetPreviousNames(): {playerId}, {currentName}");
        var names = RepositoryContext.PlayerNameWorldHistoryRepository.GetHistoricalNames(playerId);
        if (names == null)
            return string.Empty;

        var uniqueNames = names
            .Distinct()
            .Where(name => !string.IsNullOrEmpty(name) && !string.Equals(name, currentName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return uniqueNames.Count != 0 ? string.Join(", ", uniqueNames) : string.Empty;
    }

    public static string GetPreviousWorlds(int playerId, string currentWorldName)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.GetPreviousWorlds(): {playerId}, {currentWorldName}");
        var worldIds = RepositoryContext.PlayerNameWorldHistoryRepository.GetHistoricalWorlds(playerId);
        if (worldIds == null)
            return string.Empty;

        var worldNames = worldIds
            .Where(worldId => worldId != 0)
            .Select(Sheets.GetWorldNameById)
            .Distinct()
            .Where(worldName => !string.IsNullOrEmpty(worldName) && !string.Equals(worldName, currentWorldName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return worldNames.Count != 0 ? string.Join(", ", worldNames) : string.Empty;
    }

    public static List<PlayerNameWorldHistory> GetPlayerNameWorldHistory(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.GetPlayerNameWorldHistory(): {playerId}");
        var nameWorldHistories = RepositoryContext.PlayerNameWorldHistoryRepository.GetPlayerNameWorldHistories(playerId);
        return nameWorldHistories == null ? [] : nameWorldHistories.ToList();
    }

    public static List<PlayerNameWorldHistory> GetAllPlayerNameWorldHistories()
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.GetAllPlayerNameWorldHistories()");
        var nameWorldHistories = RepositoryContext.PlayerNameWorldHistoryRepository.GetAllPlayerNameWorldHistories();
        return nameWorldHistories == null ? [] : nameWorldHistories.ToList();
    }

    public static List<PlayerCustomizeHistory> GetPlayerCustomizeHistory(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerChangeService.GetPlayerCustomizeHistory(): {playerId}");
        var customizeHistories = RepositoryContext.PlayerCustomizeHistoryRepository.GetPlayerCustomizeHistories(playerId);
        return customizeHistories == null ? [] : customizeHistories.ToList();
    }
}
