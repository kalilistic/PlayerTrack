using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Game.Gui.NamePlate;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.Handler;

public static class NameplateHandler
{
    private static readonly ConcurrentDictionary<uint, PlayerNameplate> Nameplates = new();

    public static void Start()
    {
        Plugin.PluginLog.Verbose("Entering NameplateHandler.Start()");
        Plugin.NamePlateGuiHandler.OnNamePlateUpdate += UpdateNameplates;
        ServiceContext.PlayerProcessService.OnCurrentPlayerAdded += player => UpdateNameplate(player.EntityId, player);
        ServiceContext.PlayerProcessService.OnCurrentPlayerRemoved += player => RemoveNameplate(player.EntityId);
        ServiceContext.PlayerDataService.PlayerUpdated += player => UpdateNameplate(player.EntityId, player);
    }

    public static void UpdateNameplate(uint entityId, Player player)
    {
        Plugin.PluginLog.Verbose($"Entering NameplateHandler.UpdateNameplate(): {entityId}");
        var currentLocation = Plugin.PlayerLocationManager.GetCurrentLocation();
        if (currentLocation == null)
        {
            Plugin.PluginLog.Verbose("Failed to get current location.");
            return;
        }

        var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation.LocationType);
        Nameplates.AddOrUpdate(entityId, nameplate, (_, _) => nameplate);
    }

    public static void RemoveNameplate(uint entityId)
    {
        Plugin.PluginLog.Verbose($"Entering NameplateHandler.RemoveNameplate(): {entityId}");
        Nameplates.TryRemove(entityId, out _);
    }

    public static void Dispose()
    {
        Plugin.NamePlateGuiHandler.OnNamePlateUpdate -= UpdateNameplates;
    }

    public static void RefreshNameplates() =>
        Task.Run(() =>
        {
            Plugin.PluginLog.Debug("Entering NameplateHandler.RefreshNameplates()");
            var currentLocation = Plugin.PlayerLocationManager.GetCurrentLocation();
            if (currentLocation == null)
            {
                Plugin.PluginLog.Verbose("Failed to get current location.");
                return;
            }

            foreach (var cachedNameplate in Nameplates)
            {
                var player = ServiceContext.PlayerDataService.GetPlayer(cachedNameplate.Key);
                if (player == null)
                {
                    Plugin.PluginLog.Verbose($"Failed to get player for {cachedNameplate.Key}.");
                    continue;
                }

                var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation.LocationType);
                Nameplates.AddOrUpdate(cachedNameplate.Key, nameplate, (_, _) => nameplate);
            }
        });

    private static void UpdateNameplates(INamePlateUpdateContext namePlateUpdateContext, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {

            // only apply to players
            if (handler is { NamePlateKind: NamePlateKind.PlayerCharacter, PlayerCharacter: not null })
            {
                // get nameplate from cache
                var nameplate = Nameplates.GetValueOrDefault(handler.PlayerCharacter.EntityId);
                if (nameplate is not { CustomizeNameplate: true })
                    continue; // if nameplate is not customized, skip

                // apply title
                if (nameplate is { HasCustomTitle: true, CustomTitle: not null })
                {
                    handler.DisplayTitle = true;
                    handler.Title = nameplate.CustomTitle;
                }

                if (handler.PlayerCharacter.IsDead && !nameplate.NameplateUseColorIfDead)
                    continue; // stop here if dead and not using color

                // apply color
                if (!string.IsNullOrEmpty(handler.Title.TextValue))
                {
                    handler.TitleParts.LeftQuote = nameplate.TitleLeftQuote;
                    handler.TitleParts.RightQuote = nameplate.TitleRightQuote;
                }

                if (!string.IsNullOrEmpty(handler.FreeCompanyTag.TextValue))
                {
                    handler.FreeCompanyTagParts.LeftQuote = nameplate.FreeCompanyLeftQuote;
                    handler.FreeCompanyTagParts.RightQuote = nameplate.FreeCompanyRightQuote;
                }

                handler.NameParts.TextWrap = nameplate.NameTextWrap;
            }
        }
    }
}
