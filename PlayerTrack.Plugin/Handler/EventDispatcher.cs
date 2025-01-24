using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PlayerTrack.Data;
using PlayerTrack.Domain;
using PlayerTrack.Extensions;
using PlayerTrack.Models;

namespace PlayerTrack.Handler;

public static class EventDispatcher
{
    private static readonly Channel<Action> EventChannel = Channel.CreateBounded<Action>(new BoundedChannelOptions(100)
    {
        FullMode = BoundedChannelFullMode.Wait,
    });

    private static readonly CancellationTokenSource Cts = new();

    public static void Start()
    {
        Plugin.PluginLog.Verbose("Entering EventDispatcher.Start()");
        Task.Run(() => ProcessChannelAsync(EventChannel.Reader, Cts.Token));
        Plugin.PlayerLocationManager.OnLocationStarted += OnStartLocation;
        Plugin.PlayerLocationManager.OnLocationEnded += OnEndLocation;
        ContextMenuHandler.OnSelectPlayer += OnSelectPlayer;
        Plugin.ClientStateHandler.Login += OnLogin;
        Plugin.SocialListHandler.OnFriendListReceived += OnFriendListReceived;
        Plugin.SocialListHandler.OnFreeCompanyReceived += OnFreeCompanyReceived;
        Plugin.SocialListHandler.OnBlackListReceived += OnBlackListReceived;
        Plugin.SocialListHandler.OnLinkShellReceived += OnLinkShellReceived;
        Plugin.SocialListHandler.OnCrossWorldLinkShellReceived += OnCrossWorldLinkShellReceived;
    }

    public static void Dispose()
    {
        Plugin.PluginLog.Verbose("Entering EventDispatcher.Dispose()");
        try
        {
            Plugin.SocialListHandler.OnFriendListReceived -= OnFriendListReceived;
            Plugin.SocialListHandler.OnFreeCompanyReceived -= OnFreeCompanyReceived;
            Plugin.SocialListHandler.OnBlackListReceived -= OnBlackListReceived;
            Plugin.SocialListHandler.OnLinkShellReceived -= OnLinkShellReceived;
            Plugin.SocialListHandler.OnCrossWorldLinkShellReceived -= OnCrossWorldLinkShellReceived;
            Plugin.ClientStateHandler.Login -= OnLogin;
            Plugin.PlayerLocationManager.OnLocationStarted -= OnStartLocation;
            Plugin.PlayerLocationManager.OnLocationEnded -= OnEndLocation;
            ContextMenuHandler.OnSelectPlayer -= OnSelectPlayer;
            Cts.Cancel();
            EventChannel.Writer.Complete();
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to dispose EventDispatcher properly.");
        }
    }

    private static async Task ProcessChannelAsync(ChannelReader<Action> reader, CancellationToken token)
    {
        Plugin.PluginLog.Verbose("Entering EventDispatcher.ProcessChannelAsync()");
        await foreach (var action in reader.ReadAllAsync(token))
            action();
    }

    private static void OnStartLocation(LocationData location) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose($"Entering EventDispatcher.OnStartLocation(): {location.GetName()}");
            ServiceContext.EncounterService.StartCurrentEncounter(location);
        });

    private static void OnEndLocation(LocationData location) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose($"Entering EventDispatcher.OnEndLocation(): {location.GetName()}");
            ServiceContext.EncounterService.EndCurrentEncounter();
        });

    private static void OnSelectPlayer(PlayerData toadPlayer, bool isCurrent) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose($"Entering EventDispatcher.OnSelectPlayer(): {toadPlayer.Name}");
            ServiceContext.PlayerProcessService.AddOrUpdatePlayer(toadPlayer, isCurrent, true);
        });

    private static void OnLogin() => EventChannel.Writer.TryWrite(() =>
    {
        Plugin.PluginLog.Verbose("Entering EventDispatcher.OnLogin()");
        Plugin.GameFramework.RunOnFrameworkThread(Plugin.ClientStateHandler.GetLocalPlayer);
        Plugin.GameFramework.RunOnTick(() => LocalPlayerService.AddOrUpdateLocalPlayer(Plugin.ClientStateHandler.GetLocalPlayer()));
    });

    private static void OnFriendListReceived(List<SocialListMemberData> members) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose("Entering EventDispatcher.OnFriendListReceived()");
            Plugin.GameFramework.RunOnTick(() => SocialListService.HandleMembersList(SocialListType.FriendList, members));
        });

    private static void OnCrossWorldLinkShellReceived(byte index, List<SocialListMemberData> members) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose("Entering EventDispatcher.OnCrossWorldLinkShellReceived()");
            Plugin.GameFramework.RunOnTick(() => SocialListService.HandleMembersList(SocialListType.CrossWorldLinkShell, members, (ushort)(index + 1)));
        });

    private static void OnLinkShellReceived(byte index, List<SocialListMemberData> members) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose("Entering EventDispatcher.OnLinkShellReceived()");
            Plugin.GameFramework.RunOnTick(() => SocialListService.HandleMembersList(SocialListType.LinkShell, members, (ushort)(index + 1)));
        });

    private static void OnBlackListReceived(List<SocialListMemberData> members) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose("Entering EventDispatcher.OnBlackListReceived()");
            Plugin.GameFramework.RunOnTick(() => SocialListService.HandleMembersList(SocialListType.BlackList, members));
        });

    private static void OnFreeCompanyReceived(byte pageCount, byte currentPage, List<SocialListMemberData> members) =>
        EventChannel.Writer.TryWrite(() =>
        {
            Plugin.PluginLog.Verbose($"Entering EventDispatcher.OnFreeCompanyReceived(): {currentPage}/{pageCount}: {members.Count}");
            Plugin.GameFramework.RunOnTick(() => SocialListService.HandleMembersList(SocialListType.FreeCompany, members, 0, currentPage, pageCount));
        });
}
