using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

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
        DalamudContext.PluginLog.Verbose("Entering EventDispatcher.Start()");
        Task.Run(() => ProcessChannelAsync(EventChannel.Reader, Cts.Token));
        DalamudContext.PlayerLocationManager.LocationStarted += OnStartLocation;
        DalamudContext.PlayerLocationManager.LocationEnded += OnEndLocation;
        DalamudContext.PlayerEventDispatcher.AddPlayers += OnAddPlayers;
        DalamudContext.PlayerEventDispatcher.RemovePlayers += OnRemovePlayers;
        ContextMenuHandler.SelectPlayer += OnSelectPlayer;
    }

    public static void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering EventDispatcher.Dispose()");
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
            DalamudContext.PluginLog.Error(ex, "Failed to dispose EventDispatcher properly.");
        }
    }

    private static async Task ProcessChannelAsync(ChannelReader<Action> reader, CancellationToken token)
    {
        DalamudContext.PluginLog.Verbose("Entering EventDispatcher.ProcessChannelAsync()");
        await foreach (var action in reader.ReadAllAsync(token))
        {
            action();
        }
    }

    private static void OnStartLocation(ToadLocation location) => EventChannel.Writer.TryWrite(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering EventDispatcher.OnStartLocation(): {location.GetName()}");
        ServiceContext.EncounterService.StartCurrentEncounter(location);
    });

    private static void OnEndLocation(ToadLocation location) => EventChannel.Writer.TryWrite(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering EventDispatcher.OnEndLocation(): {location.GetName()}");
        ServiceContext.EncounterService.EndCurrentEncounter();
    });

    private static void OnAddPlayers(List<ToadPlayer> players) => EventChannel.Writer.TryWrite(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering EventDispatcher.OnAddPlayers(): {players.Count}");
        foreach (var player in players)
        {
            ServiceContext.PlayerProcessService.AddOrUpdatePlayer(player);
        }
    });

    private static void OnRemovePlayers(List<uint> playerObjectIds) => EventChannel.Writer.TryWrite(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering EventDispatcher.OnRemovePlayers(): {playerObjectIds.Count}");
        foreach (var playerObjectId in playerObjectIds)
        {
            ServiceContext.PlayerProcessService.RemoveCurrentPlayer(playerObjectId);
        }
    });

    private static void OnSelectPlayer(ToadPlayer toadPlayer, bool isCurrent) => EventChannel.Writer.TryWrite(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering EventDispatcher.OnSelectPlayer(): {toadPlayer.Name}");
        ServiceContext.PlayerProcessService.AddOrUpdatePlayer(toadPlayer, isCurrent, true);
    });
}
