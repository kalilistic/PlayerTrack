using System;
using Dalamud.DrunkenToad.Core;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Pilz.Dalamud;
using Pilz.Dalamud.Nameplates.EventArgs;
using Pilz.Dalamud.Nameplates.Tools;
using Pilz.Dalamud.Tools.Strings;
using PlayerTrack.Domain;
using InternalNameplateManager = Pilz.Dalamud.Nameplates.NameplateManager;

namespace PlayerTrack.Plugin;

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Domain.Common;
using Models;

public static class NameplateHandler
{
    private static readonly ConcurrentDictionary<string, PlayerNameplate> Nameplates = new();
    private static InternalNameplateManager? internalNameplateManager;

    public static void Start()
    {
        DalamudContext.PluginLog.Verbose("Entering NameplateHandler.Start()");
        PluginServices.Initialize(DalamudContext.PluginInterface);
        internalNameplateManager = new InternalNameplateManager();
        internalNameplateManager.Hooks.AddonNamePlate_SetPlayerNameManaged += OnNameplateUpdate;
        ServiceContext.PlayerProcessService.CurrentPlayerAdded += player => UpdateNameplate(player.Key, player);
        ServiceContext.PlayerProcessService.CurrentPlayerRemoved += player => RemoveNameplate(player.Key);
        ServiceContext.PlayerDataService.PlayerUpdated += player => UpdateNameplate(player.Key, player);
    }

    public static void RefreshNameplates() => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose("Entering NameplateHandler.RefreshNameplates()");
        var currentLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
        if (currentLocation == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get current location.");
            return;
        }

        foreach (var entry in Nameplates)
        {
            var player = ServiceContext.PlayerDataService.GetPlayer(entry.Key);
            if (player == null)
            {
                DalamudContext.PluginLog.Verbose($"Failed to get player for {entry.Key}.");
                continue;
            }

            var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation);
            Nameplates.AddOrUpdate(entry.Key, nameplate, (_, _) => nameplate);
        }
    });

    public static void UpdateNameplate(string key, Player player)
    {
        DalamudContext.PluginLog.Verbose($"Entering NameplateHandler.UpdateNameplate(): {key}");
        var currentLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
        if (currentLocation == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get current location.");
            return;
        }

        var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation);
        Nameplates.AddOrUpdate(key, nameplate, (_, _) => nameplate);
    }

    public static void RemoveNameplate(string key)
    {
        DalamudContext.PluginLog.Verbose($"Entering NameplateHandler.RemoveNameplate(): {key}");
        Nameplates.TryRemove(key, out _);
    }

    public static void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering NameplateHandler.Dispose()");
        try
        {
            if (internalNameplateManager != null)
            {
                internalNameplateManager.Hooks.AddonNamePlate_SetPlayerNameManaged -= OnNameplateUpdate;
                internalNameplateManager.Dispose();
            }
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to dispose NameplateHandler properly.");
        }
    }

    private static void OnNameplateUpdate(AddonNamePlate_SetPlayerNameManagedEventArgs eventArgs)
    {
        DalamudContext.PluginLog.Verbose("Entering NameplateHandler.OnNameplateUpdate()");
        var pc = InternalNameplateManager.GetNameplateGameObject<PlayerCharacter>(eventArgs.SafeNameplateObject);
        if (pc == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get player character.");
            return;
        }

        var loc = DalamudContext.PlayerLocationManager.GetCurrentLocation();
        if (loc == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get player location.");
            return;
        }

        var key = PlayerKeyBuilder.Build(pc.Name.TextValue, pc.HomeWorld.Id);
        var nameplate = Nameplates.TryGetValue(key, out var nameplateValue) ? nameplateValue : null;
        if (nameplate is not { CustomizeNameplate: true })
        {
            DalamudContext.PluginLog.Verbose("Player nameplate is not customized.");
            return;
        }

        if (!nameplate.NameplateUseColorIfDead && pc.IsDead)
        {
            DalamudContext.PluginLog.Verbose("Player is dead, disabling.");
            return;
        }

        var nameplateChanges = new NameplateChanges(eventArgs);
        foreach (var element in Enum.GetValues<NameplateElements>())
        {
            if (element == NameplateElements.Title && string.IsNullOrEmpty(eventArgs.Title.ToString()) && !nameplate.HasCustomTitle)
            {
                DalamudContext.PluginLog.Verbose("Player does not have a title.");
                continue;
            }

            if (element == NameplateElements.Title && nameplate.HasCustomTitle)
            {
                DalamudContext.PluginLog.Verbose("Player has a custom title.");
                var titlePayload = new TextPayload($"《{nameplate.CustomTitle}》");
                nameplateChanges.GetChange(element, StringPosition.Replace).Payloads.Add(titlePayload);
            }

            var colorPayload = new UIForegroundPayload((ushort)nameplate.Color);
            var resetPayload = new UIForegroundPayload(0);

            nameplateChanges.GetChange(element, StringPosition.Before).Payloads.Add(colorPayload);
            nameplateChanges.GetChange(element, StringPosition.After).Payloads.Add(resetPayload);
        }

        NameplateUpdateFactory.ApplyNameplateChanges(new NameplateChangesProps(nameplateChanges));
    }
}
