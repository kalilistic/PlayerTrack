using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using PlayerTrack.Domain;
using PlayerTrack.Models;

// ReSharper disable UseCollectionExpression
// ReSharper disable RedundantAssignment
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
// ReSharper disable ReplaceWithSingleCallToFirstOrDefault
// ReSharper disable UnusedVariable
// ReSharper disable InconsistentNaming
namespace PlayerTrack.API;

using Dalamud.DrunkenToad.Core;

/// <inheritdoc cref="IPlayerTrackAPI" />
[SuppressMessage("Performance", "CA1854:Prefer the \'IDictionary.TryGetValue(TKey, out TValue)\' method")]
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
    public ((string, uint), (string, uint)[])[] GetAllPlayerNameWorldHistories() 
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetAllPlayerNameWorldHistories()");
        this.CheckInitialized();
        var playerHistories = PlayerChangeService.GetAllPlayerNameWorldHistories();
        if (playerHistories == null) 
        {
            DalamudContext.PluginLog.Warning("No player name/world history found.");
            return [];
        }
        Dictionary<int, List<(string, uint)>> combinedResults = [];
        foreach(var playerHistory in playerHistories)
        {
            if (!combinedResults.ContainsKey(playerHistory.PlayerId))
            {
                combinedResults.Add(playerHistory.PlayerId, []);
            }
            combinedResults[playerHistory.PlayerId].Add((playerHistory.PlayerName, playerHistory.WorldId));
        }

        var playerObjList = ServiceContext.PlayerDataService.GetAllPlayers().Where(x => combinedResults.ContainsKey(x.Id)).ToList();
        List<((string, uint), (string, uint)[])> toReturn = [];
        foreach(var result in combinedResults) 
        {
            var player = playerObjList.Where(player => player.Id == result.Key).FirstOrDefault();
            if(player != null) {
                toReturn.Add(((player.Name, player.WorldId), result.Value.ToArray()));
            }
        }

        return [.. toReturn];
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
