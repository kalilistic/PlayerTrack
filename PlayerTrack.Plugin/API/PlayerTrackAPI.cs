using System;
using PlayerTrack.Domain;

namespace PlayerTrack.API;

using Dalamud.DrunkenToad.Core;
using Dalamud.Utility;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

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
    public uint GetPlayerLodestoneId(string name, uint worldId) {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayerLodestoneId({name}, {worldId})");
        this.CheckInitialized();
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null) {
            DalamudContext.PluginLog.Warning("Player not found");
            return 0;
        }

        return player.LodestoneId;
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
    public string[] GetPlayerPreviousNames(string name, uint worldId) 
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayerPreviousNames({name}, {worldId})");
        this.CheckInitialized();
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null) 
        {
            //DalamudContext.PluginLog.Warning("Player not found");
            return new string[] { };
        }
        var pNames = PlayerChangeService.GetPreviousNames(player.Id, player.Name);
        DalamudContext.PluginLog.Debug($"Previous names: {pNames}");
        return pNames.Split(",");
    }

    /// <inheritdoc />
    public string[] GetPlayerPreviousWorlds(string name, uint worldId) 
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayerPreviousWorlds({name}, {worldId})");
        this.CheckInitialized();
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null) 
        {
            //DalamudContext.PluginLog.Warning("Player not found");
            return new string[] { };
        }

        var pWorlds = PlayerChangeService.GetPreviousWorlds(player.Id, player.Name);
        DalamudContext.PluginLog.Debug($"Previous worlds: {pWorlds}");
        return pWorlds.Split(",");
    }

    public ((string, uint), string[], uint[])[] GetPlayersPreviousNamesWorlds((string, uint)[] players) 
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.GetPlayersPreviousNamesWorlds(count: {players.Length})");
        this.CheckInitialized();
        List<Player> playerObjList = new();
        playerObjList = ServiceContext.PlayerDataService.GetAllPlayers().Where(x => players.ToList().Contains((x.Name, x.WorldId))).ToList();

        //foreach(var player in players)
        //{
        //    var playerObj = ServiceContext.PlayerDataService.GetPlayer(player.Item1, player.Item2);
        //    if(playerObj != null)
        //    {
        //        playerObjList.Add(playerObj);
        //    }
        //}
        var playerHistories = PlayerChangeService.GetPlayerNameWorldHistories(playerObjList.Select(x => x.Id).ToArray());

        if (playerHistories == null) 
        {
            DalamudContext.PluginLog.Warning("No player name/world history found.");
            return new ((string, uint), string[], uint[])[] { };
        }

        Dictionary<int, (List<string>, List<uint>)> combinedResults = new();
        foreach(var playerHistory in playerHistories)
        {
            var player = playerObjList.Where(x => x.Id == playerHistory.PlayerId).FirstOrDefault();
            if (!combinedResults.ContainsKey(playerHistory.PlayerId))
            {
                combinedResults.Add(playerHistory.PlayerId, (new(), new()));
            }
            if (!playerHistory.PlayerName.IsNullOrEmpty() && !combinedResults[playerHistory.PlayerId].Item1.Contains(playerHistory.PlayerName) && player?.Name != playerHistory.PlayerName)
            {
                combinedResults[playerHistory.PlayerId].Item1.Add(playerHistory.PlayerName);
            }
            if (playerHistory.WorldId != 0 && !combinedResults[playerHistory.PlayerId].Item2.Contains(playerHistory.WorldId) && player?.WorldId != playerHistory.WorldId)
            {
                combinedResults[playerHistory.PlayerId].Item2.Add(playerHistory.WorldId);
            }
        }
        //return combinedResults.Select(x => (playerObjList.Where(y => y.Id == x.Key)
        //.Select(z => $"{z.Name} {z.WorldId}").First(), x.Value.Item1.ToArray(), x.Value.Item2.ToArray())).ToArray();
        return combinedResults.Select(x => (playerObjList.Where(y => y.Id == x.Key)
        .Select(y => (y.Name, y.WorldId)).First(), x.Value.Item1.ToArray(), x.Value.Item2.ToArray())).ToArray();
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
