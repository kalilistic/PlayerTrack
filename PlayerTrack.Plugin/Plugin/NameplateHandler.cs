using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.Nameplates;

namespace PlayerTrack.Plugin;

public static class NameplateHandler
{
    private static readonly ConcurrentDictionary<uint, PlayerNameplate> Nameplates = new();
    private static NamePlateGui? namePlateGuiHandler;
    
    public static void Start()
    {
        DalamudContext.PluginLog.Verbose("Entering NameplateHandler.Start()");
        namePlateGuiHandler = new NamePlateGui();
        namePlateGuiHandler.OnNamePlateUpdate += UpdateNameplates;
        ServiceContext.PlayerProcessService.CurrentPlayerAdded += player => UpdateNameplate(player.EntityId, player);
        ServiceContext.PlayerProcessService.CurrentPlayerRemoved += player => RemoveNameplate(player.EntityId);
        ServiceContext.PlayerDataService.PlayerUpdated += player => UpdateNameplate(player.EntityId, player);
    }
    
    public static void UpdateNameplate(uint entityId, Player player)
    {
        DalamudContext.PluginLog.Verbose($"Entering NameplateHandler.UpdateNameplate(): {entityId}");
        var currentLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
        if (currentLocation == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get current location.");
            return;
        }

        var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation.LocationType);
        Nameplates.AddOrUpdate(entityId, nameplate, (_, _) => nameplate);
    }
    
    public static void RemoveNameplate(uint entityId)
    {
        DalamudContext.PluginLog.Verbose($"Entering NameplateHandler.RemoveNameplate(): {entityId}");
        Nameplates.TryRemove(entityId, out _);
    }

    public static void Dispose()
    {
        if (namePlateGuiHandler != null)
        {
            namePlateGuiHandler.Dispose();
            namePlateGuiHandler.OnNamePlateUpdate -= UpdateNameplates;
            namePlateGuiHandler = null;
        }
    }
     

    public static void RefreshNameplates() => Task.Run(() =>
    {
        DalamudContext.PluginLog.Debug("Entering NameplateHandler.RefreshNameplates()");
        var currentLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
        if (currentLocation == null)
        {
            DalamudContext.PluginLog.Verbose("Failed to get current location.");
            return;
        }

        foreach (var cachedNameplate in Nameplates)
        {
            var player = ServiceContext.PlayerDataService.GetPlayer(cachedNameplate.Key);
            if (player == null)
            {
                DalamudContext.PluginLog.Verbose($"Failed to get player for {cachedNameplate.Key}.");
                continue;
            }
        
            var nameplate = PlayerNameplateService.GetPlayerNameplate(player, currentLocation.LocationType);
            Nameplates.AddOrUpdate(cachedNameplate.Key, nameplate, (_, _) => nameplate);
        }
    });

    private static void UpdateNameplates(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers) {

            // only apply to players
            if (handler is { NamePlateKind: NamePlateKind.PlayerCharacter, PlayerCharacter: not null })
            {
                // get nameplate from cache 
                var nameplate = Nameplates.GetValueOrDefault(handler.PlayerCharacter.EntityId);
                
                // if nameplate is not customized, skip
                if (nameplate is not { CustomizeNameplate: true }) continue;
                
                // apply title
                if (nameplate is { HasCustomTitle: true, CustomTitle: not null })
                {
                    handler.DisplayTitle = true;
                    handler.Title = nameplate.CustomTitle;
                }
                
                // stop here if dead and not using color
                if (handler.PlayerCharacter.IsDead && !nameplate.NameplateUseColorIfDead) continue;
    
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
