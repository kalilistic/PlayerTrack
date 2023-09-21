using System;
using Dalamud.ContextMenu;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Utility;
using PlayerTrack.Domain;

namespace PlayerTrack.Plugin;

using Dalamud.Game.Text;
using Domain.Common;

public static class ContextMenuHandler
{
    private static GameObjectContextMenuItem? openPlayerTrackMenuItem;
    private static GameObjectContextMenuItem? openLodestoneItem;
    private static DalamudContextMenu? contextMenu;
    private static bool isStarted;

    public delegate void SelectPlayerDelegate(ToadPlayer player);

    public static event SelectPlayerDelegate? SelectPlayer;

    public static void Start()
    {
        PluginLog.LogVerbose("Entering ContextMenuHandler.Start()");
        contextMenu = new DalamudContextMenu();
        contextMenu.OnOpenGameObjectContextMenu += OpenGameObjectContextMenu;
        BuildContentMenuItems();
        isStarted = true;
    }

    public static void Restart()
    {
        PluginLog.LogVerbose("Entering ContextMenuHandler.Restart()");
        Dispose();
        Start();
    }

    public static void Dispose()
    {
        PluginLog.LogVerbose("Entering ContextMenuHandler.Dispose()");
        try
        {
            if (contextMenu != null)
            {
                contextMenu.OnOpenGameObjectContextMenu -= OpenGameObjectContextMenu;
                contextMenu.Dispose();
            }
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to dispose ContextMenuHandler properly.");
        }
    }

    private static void BuildContentMenuItems()
    {
        PluginLog.LogVerbose("Entering ContextMenuHandler.BuildContentMenuItems()");
        var showContextMenuIndicator = ServiceContext.ConfigService.GetConfig().ShowContextMenuIndicator;
        SeString openPlayerTrackMenuItemName;
        SeString openLodestoneItemName;
        if (showContextMenuIndicator)
        {
            openPlayerTrackMenuItemName = new SeString().Append(new UIForegroundPayload(539)).Append($"{SeIconChar.BoxedLetterP.ToIconString()} ")
                .Append(new UIForegroundPayload(0)).Append(DalamudContext.LocManager.GetString("OpenPlayerTrack"));
            openLodestoneItemName = new SeString().Append(new UIForegroundPayload(539)).Append($"{SeIconChar.BoxedLetterP.ToIconString()} ")
                .Append(new UIForegroundPayload(0)).Append(DalamudContext.LocManager.GetString("OpenLodestone"));
        }
        else
        {
            openPlayerTrackMenuItemName = new SeString(new TextPayload(DalamudContext.LocManager.GetString("OpenPlayerTrack")));
            openLodestoneItemName = new SeString(new TextPayload(DalamudContext.LocManager.GetString("OpenLodestone")));
        }

        openPlayerTrackMenuItem = new GameObjectContextMenuItem(openPlayerTrackMenuItemName, OnSelectPlayer);
        openLodestoneItem = new GameObjectContextMenuItem(openLodestoneItemName, OnOpenLodestone);
    }

    private static void OnOpenLodestone(BaseContextMenuArgs args)
    {
        PluginLog.LogVerbose($"Entering ContextMenuHandler.OnOpenLodestone(): {args.Text} {args.ObjectWorld}");
        if (args.Text == null || !args.Text.IsValidCharacterName())
        {
            PluginLog.LogWarning($"Invalid character name: {args.Text}");
            return;
        }

        var key = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
        var player = ServiceContext.PlayerDataService.GetPlayer(key);
        var lodestoneId = player?.LodestoneId ?? 0;
        PlayerLodestoneService.OpenLodestoneProfile(lodestoneId);
    }

    private static void OnSelectPlayer(GameObjectContextMenuItemSelectedArgs args)
    {
        PluginLog.LogVerbose($"Entering ContextMenuHandler.OnSelectPlayer(), {args.Text} {args.ObjectWorld}");
        if (args.Text == null || !args.Text.IsValidCharacterName())
        {
            PluginLog.LogWarning($"Invalid character name: {args.Text}");
            return;
        }

        var toadPlayer = DalamudContext.PlayerEventDispatcher.GetPlayerByNameAndWorldId(args.Text.TextValue, args.ObjectWorld) ?? new ToadPlayer
        {
            Name = args.Text.TextValue,
            HomeWorld = args.ObjectWorld,
            CompanyTag = string.Empty,
        };

        SelectPlayer?.Invoke(toadPlayer);
    }

    private static bool IsMenuValid(BaseContextMenuArgs args)
    {
        PluginLog.LogVerbose($"Entering ContextMenuHandler.IsMenuValid(), ParentAddonName: {args.ParentAddonName}");
        switch (args.ParentAddonName)
        {
            case null:
            case "LookingForGroup":
            case "PartyMemberList":
            case "FriendList":
            case "FreeCompany":
            case "SocialList":
            case "ContactList":
            case "ChatLog":
            case "_PartyList":
            case "LinkShell":
            case "CrossWorldLinkshell":
            case "ContentMemberList":
            case "BlackList":
            case "BeginnerChatList":
                return args.Text != null && args.ObjectWorld != 0 && args.ObjectWorld != 65535;

            default:
                PluginLog.LogWarning($"Invalid ParentAddonName: {args.ParentAddonName}");
                return false;
        }
    }

    private static void OpenGameObjectContextMenu(GameObjectContextMenuOpenArgs args)
    {
        PluginLog.LogVerbose($"Entering ContextMenuHandler.OpenGameObjectContextMenu(), ObjectId: {args.ObjectId}, ParentAddonName: {args.ParentAddonName}");

        // check if plugin started
        if (!isStarted)
        {
            return;
        }

        // name can't be null
        if (string.IsNullOrEmpty(args.Text?.TextValue))
        {
            return;
        }

        // get self key
        var name = DalamudContext.ClientStateHandler.LocalPlayer?.Name.TextValue;
        var worldId = DalamudContext.ClientStateHandler.LocalPlayer?.HomeWorld.Id;
        if (string.IsNullOrEmpty(name) || worldId == null)
        {
            PluginLog.LogWarning("Failed to get self key.");
            return;
        }

        // check if self menu
        var selfKey = PlayerKeyBuilder.Build(name, worldId.Value);
        var menuKey = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
        if (selfKey.Equals(menuKey, StringComparison.Ordinal))
        {
            PluginLog.LogVerbose("Self menu, skipping.");
            return;
        }

        // validate menu
        if (!IsMenuValid(args))
        {
            return;
        }

        // add menu items
        if (ServiceContext.ConfigService.GetConfig().ShowOpenInPlayerTrack)
        {
            args.AddCustomItem(openPlayerTrackMenuItem);
        }

        if (ServiceContext.ConfigService.GetConfig().ShowOpenLodestone)
        {
            var key = PlayerKeyBuilder.Build(args.Text.TextValue, args.ObjectWorld);
            var player = ServiceContext.PlayerDataService.GetPlayer(key);
            var lodestoneId = player?.LodestoneId ?? 0;
            if (lodestoneId != 0)
            {
                args.AddCustomItem(openLodestoneItem);
            }
        }
    }
}
