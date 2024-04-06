using System;
using System.Linq;
using System.Collections.Generic;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.API;

using Dalamud.DrunkenToad.Core;

/// <inheritdoc cref="IPlayerTrackAPI" />
public class PlayerTrackAPI : IPlayerTrackAPI
{
    private readonly bool initialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTrackAPI"/> class.
    /// </summary>
    public PlayerTrackAPI() => this.initialized = true;

    /// <inheritdoc />
    public int APIVersion => 1;

    /// <inheritdoc />
    public string GetPlayerCurrentNameWorld(string name, uint worldId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayerCurrentNameWorld({name}, {worldId})");
        this.CheckInitialized();
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning("Player not found");
            return $"{name} {worldId}";
        }

        return $"{player.Name} {player.WorldId}";
    }

    /// <inheritdoc />
    public string GetPlayerNotes(string name, uint worldId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayerNotes({name}, {worldId})");
        this.CheckInitialized();
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning("Player not found");
            return string.Empty;
        }

        return player.Notes;
    }

    /// <inheritdoc />
    public ((string, uint), (string, uint)[])[] GetUniquePlayerNameWorldHistories((string, uint)[] players) 
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetUniquePlayerNameWorldHistories(count: {players.Length})");
        this.CheckInitialized();
        List<Player> playerObjList = new();
        playerObjList = ServiceContext.PlayerDataService.GetAllPlayers().Where(x => players.ToList().Contains((x.Name, x.WorldId))).ToList();
        var playerHistories = PlayerChangeService.GetPlayerNameWorldHistories(playerObjList.Select(x => x.Id).ToArray());
        if (playerHistories == null) 
        {
            DalamudContext.PluginLog.Warning("No player name/world history found.");
            return new ((string, uint), (string, uint)[])[] { };
        }

        Dictionary<int, List<(string, uint)>> combinedResults = new();
        foreach(var playerHistory in playerHistories)
        {
            var player = playerObjList.Where(x => x.Id == playerHistory.PlayerId).FirstOrDefault();
            if (!combinedResults.ContainsKey(playerHistory.PlayerId))
            {
                combinedResults.Add(playerHistory.PlayerId, new());
            }
            //only add non-migrated, non-current and unique records
            if(!playerHistory.IsMigrated
                && !(player?.Name == playerHistory.PlayerName && player?.WorldId == playerHistory.WorldId)
                && !combinedResults[playerHistory.PlayerId].Any(x => x.Item1 == playerHistory.PlayerName && x.Item2 == playerHistory.WorldId))
            {
                combinedResults[playerHistory.PlayerId].Add((playerHistory.PlayerName, playerHistory.WorldId));
            }
        }
        return combinedResults.Where(x => x.Value.Any()).Select(x => (
        playerObjList.Where(y => y.Id == x.Key).Select(y => (y.Name, y.WorldId)).First(),
        x.Value.ToArray()
        )).ToArray();
    }

    private void CheckInitialized()
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.CheckInitialized()");
        if (this.initialized) return;
        const string msg = "API is not initialized.";
        DalamudContext.PluginLog.Warning(msg);
        throw new InvalidOperationException(msg);
    }
}
