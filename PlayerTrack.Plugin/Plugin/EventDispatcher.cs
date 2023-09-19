using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.Logging;
using PlayerTrack.Domain;

namespace PlayerTrack.Plugin;

public static class EventDispatcher
{
    private static readonly Channel<Action> EventChannel = Channel.CreateBounded<Action>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });

    private static readonly CancellationTokenSource Cts = new();

    public static void Start()
    {
        PluginLog.LogVerbose("Entering EventDispatcher.Start()");
        Task.Run(() => ProcessChannelAsync(EventChannel.Reader, Cts.Token));
        DalamudContext.PlayerLocationManager.LocationStarted += OnStartLocation;
        DalamudContext.PlayerLocationManager.LocationEnded += OnEndLocation;
        DalamudContext.PlayerEventDispatcher.AddPlayers += OnAddPlayers;
        DalamudContext.PlayerEventDispatcher.RemovePlayers += OnRemovePlayers;
        ContextMenuHandler.SelectPlayer += OnSelectPlayer;
    }

    public static void Dispose()
    {
        PluginLog.LogVerbose("Entering EventDispatcher.Dispose()");
        try
        {
            DalamudContext.PlayerLocationManager.LocationStarted -= OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationEnded -= OnEndLocation;
            DalamudContext.PlayerEventDispatcher.AddPlayers -= OnAddPlayers;
            DalamudContext.PlayerEventDispatcher.RemovePlayers -= OnRemovePlayers;
            ContextMenuHandler.SelectPlayer -= OnSelectPlayer;
            Cts.Cancel();
            EventChannel.Writer.Complete();
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to dispose EventDispatcher properly.");
        }
    }

    private static async Task ProcessChannelAsync(ChannelReader<Action> reader, CancellationToken token)
    {
        PluginLog.LogVerbose("Entering EventDispatcher.ProcessChannelAsync()");
        await foreach (var action in reader.ReadAllAsync(token))
        {
            action();
        }
    }

    private static void OnStartLocation(ToadLocation location) => EventChannel.Writer.TryWrite(() =>
    {
        PluginLog.LogVerbose($"Entering EventDispatcher.OnStartLocation(): {location.GetName()}");
        ServiceContext.EncounterService.StartCurrentEncounter(location);
    });

    private static void OnEndLocation(ToadLocation location) => EventChannel.Writer.TryWrite(() =>
    {
        PluginLog.LogVerbose($"Entering EventDispatcher.OnEndLocation(): {location.GetName()}");
        ServiceContext.EncounterService.EndCurrentEncounter();
    });

    private static void OnAddPlayers(List<ToadPlayer> players) => EventChannel.Writer.TryWrite(() =>
    {
        PluginLog.LogVerbose($"Entering EventDispatcher.OnAddPlayers(): {players.Count}");
        foreach (var player in players)
        {
            ServiceContext.PlayerProcessService.AddOrUpdatePlayer(player);
        }
    });

    private static void OnRemovePlayers(List<uint> playerObjectIds) => EventChannel.Writer.TryWrite(() =>
    {
        PluginLog.LogVerbose($"Entering EventDispatcher.OnRemovePlayers(): {playerObjectIds.Count}");
        foreach (var playerObjectId in playerObjectIds)
        {
            ServiceContext.PlayerProcessService.RemoveCurrentPlayer(playerObjectId);
        }
    });

    private static void OnSelectPlayer(ToadPlayer toadPlayer) => EventChannel.Writer.TryWrite(() =>
    {
        PluginLog.LogVerbose($"Entering EventDispatcher.OnSelectPlayer(): {toadPlayer.Name}");
        ServiceContext.PlayerProcessService.AddOrUpdatePlayer(toadPlayer, false, true);
    });
}
