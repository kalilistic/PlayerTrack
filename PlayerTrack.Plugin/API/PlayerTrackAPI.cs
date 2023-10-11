using System;
using PlayerTrack.Domain;

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

    private void CheckInitialized()
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerTrackAPI.CheckInitialized()");
        if (this.initialized) return;
        const string msg = "API is not initialized.";
        DalamudContext.PluginLog.Warning(msg);
        throw new InvalidOperationException(msg);
    }
}
