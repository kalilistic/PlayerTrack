using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Plugin.Services;

namespace PlayerTrack.Domain;

using System;
using System.Collections;
using Common;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Helpers;
using Models;

public class PlayerProcessService
{
    private volatile bool isProcessing;
    private readonly ReaderWriterLockSlim locker = new ();
    public event Action<Player>? PlayerSelected;
    public event Action<Player>? CurrentPlayerAdded;
    public event Action<Player>? CurrentPlayerRemoved;
    
    public void RegisterCurrentPlayer(Player player) => this.CurrentPlayerAdded?.Invoke(player);
    
    public void Dispose()
    {
        DalamudContext.GameFramework.Update -= this.ProcessCurrentPlayers;
        locker.Dispose();
    }

    public void Start()
    {
        DalamudContext.GameFramework.Update += this.ProcessCurrentPlayers;
    }
    
    private void ProcessCurrentPlayers(IFramework framework)
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerProcessService.ReconcileCurrentPlayerTimerOnElapsed()");
        locker.EnterWriteLock();
        try
        {
            if (isProcessing) return;
            isProcessing = true;
        }
        finally
        {
            locker.ExitWriteLock();
        }
        
        DalamudContext.GameFramework.RunOnFrameworkThread(() =>
        {
            var objectPlayers = DalamudContext.ObjectCollection.GetPlayers().ToList();

            Task.Run(() =>
            {
                try
                {
                    var currentPlayers = ServiceContext.PlayerCacheService.GetCurrentPlayers();
                    if (currentPlayers.Count > 0)
                    {
                        foreach (var currentPlayer in currentPlayers)
                        {
                            if (objectPlayers.All(p => p.ContentId != currentPlayer.ContentId))
                            {
                                DalamudContext.PluginLog.Verbose($"Removing current player: {currentPlayer.ContentId}, {currentPlayer.Name}@{currentPlayer.WorldId}");
                                this.RemoveCurrentPlayer(currentPlayer);
                            }
                        }
                    }

                    if (objectPlayers.Count > 0)
                    {
                        foreach (var objectPlayer in objectPlayers)
                        {
                            var currentPlayer = currentPlayers.FirstOrDefault(p => p.ContentId == objectPlayer.ContentId);
                            if (currentPlayer == null)
                            {
                                DalamudContext.PluginLog.Verbose($"Adding current player: {objectPlayer.ContentId}, {objectPlayer.Name}");
                                this.AddOrUpdatePlayer(objectPlayer);
                            }
                        }
                    }
                }
                finally
                {
                    locker.EnterWriteLock();
                    try
                    {
                        isProcessing = false;
                    }
                    finally
                    {
                        locker.ExitWriteLock();
                    }
                }
            });
        });
    }
        
    public static void CreateNewPlayer(string name, uint worldId, ulong contentId = 0)
    {
        var key = PlayerKeyBuilder.Build(name, worldId);
        var player = new Player
        {
            Key = key,
            Name = name,
            WorldId = worldId,
            ContentId = contentId,
            Created = UnixTimestampHelper.CurrentTime(),
        };

        ServiceContext.PlayerDataService.AddPlayer(player);
    }

    public void SelectPlayer(string name, string worldName)
    {
        var worldId = DalamudContext.DataManager.GetWorldIdByName(worldName);
        var player = ServiceContext.PlayerDataService.GetPlayer(name, worldId);
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found.");
            return;
        }

        this.PlayerSelected?.Invoke(player);
    }

    public void SelectPlayer(int playerId)
    {
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found.");
            return;
        }
        this.PlayerSelected?.Invoke(player);
    }

    public void AddOrUpdatePlayer(ToadPlayer toadPlayer, bool isCurrent = true, bool isUserRequest = false)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerProcessService.AddOrUpdatePlayer(): {toadPlayer.ContentId}, {toadPlayer.Name}, {toadPlayer.HomeWorld}, {isUserRequest}");
        var enc = ServiceContext.EncounterService.GetCurrentEncounter();
        var player = ServiceContext.PlayerDataService.GetPlayer(toadPlayer.ContentId, toadPlayer.Name, toadPlayer.HomeWorld);
        
        if (enc == null)
        {
            HandleContentIdUpdateOnly(player, toadPlayer);
            DalamudContext.PluginLog.Verbose("Encounter is missing.");
            return;
        }

        if (!enc.SavePlayers && !isUserRequest)
        {
            HandleContentIdUpdateOnly(player, toadPlayer);
            DalamudContext.PluginLog.Verbose("Encounter is not set to save players.");
            return;
        }

        var loc = PlayerEncounterService.GetEncounterLocation();
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found, creating new player.");
            var key = PlayerKeyBuilder.Build(toadPlayer.Name, toadPlayer.HomeWorld);
            this.CreateNewPlayer(toadPlayer, key, isCurrent, enc.CategoryId, loc);
            player = ServiceContext.PlayerDataService.GetPlayer(toadPlayer.ContentId, toadPlayer.Name, toadPlayer.HomeWorld);
            if (player != null)
            {
                if (isUserRequest)
                {
                    this.PlayerSelected?.Invoke(player);
                }
                else if (enc.SaveEncounter)
                {
                    player.OpenPlayerEncounterId = PlayerEncounterService.CreatePlayerEncounter(toadPlayer, player);
                }
            }
        }
        else if (isUserRequest)
        {
            DalamudContext.PluginLog.Verbose("Force load player, used for player search.");
            player = this.UpdateExistingPlayer(player, toadPlayer, isCurrent, loc);
            this.PlayerSelected?.Invoke(player);
        }
        else if (!player.IsCurrent)
        {
            DalamudContext.PluginLog.Verbose("Player found, updating existing player.");
            player = this.UpdateExistingPlayer(player, toadPlayer, isCurrent, loc);
            ServiceContext.PlayerAlertService.SendProximityAlert(player);
            if (enc.SaveEncounter)
            {
                player.OpenPlayerEncounterId = PlayerEncounterService.CreatePlayerEncounter(toadPlayer, player);
            }
        }
    }
    
    internal void RemoveCurrentPlayer(uint entityId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerProcessService.RemoveCurrentPlayer(): {entityId}");
        var player = ServiceContext.PlayerDataService.GetPlayer(entityId);
        if (player == null)
        {
            DalamudContext.PluginLog.Verbose("Player not found.");
            return;
        }

        RemoveCurrentPlayer(player);
    }
    
    internal void RemoveCurrentPlayer(Player player)
    {
        PlayerEncounterService.EndPlayerEncounter(player, ServiceContext.EncounterService.GetCurrentEncounter());
        player.IsCurrent = false;
        player.LastSeen = UnixTimestampHelper.CurrentTime();
        player.OpenPlayerEncounterId = 0;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        this.CurrentPlayerRemoved?.Invoke(player);
    }

    private static void HandleContentIdUpdateOnly(Player? player, ToadPlayer toadPlayer)
    {
        if (player is not { ContentId: 0 }) return;
        player.ContentId = toadPlayer.ContentId;
        DalamudContext.PluginLog.Verbose($"Player content id updated: {player.Name}@{player.WorldId}");
        ServiceContext.PlayerDataService.UpdatePlayer(player);
    }
    
    private void CreateNewPlayer(ToadPlayer toadPlayer, string key, bool isCurrent, int categoryId, ToadLocation loc)
    {
        var player = new Player
        {
            Key = key,
            EntityId = toadPlayer.EntityId,
            ContentId = toadPlayer.ContentId,
            Name = toadPlayer.Name,
            WorldId = toadPlayer.HomeWorld,
            PrimaryCategoryId = categoryId,
            FreeCompany = PlayerFCHelper.CheckFreeCompany(toadPlayer.CompanyTag, loc.InContent()),
            Customize = toadPlayer.Customize,
            LastTerritoryType = (ushort)(isCurrent ? loc.TerritoryId : 0),
            Created = UnixTimestampHelper.CurrentTime(),
            IsCurrent = isCurrent,
            IsRecent = isCurrent,
            LastSeen = isCurrent ? UnixTimestampHelper.CurrentTime() : 0,
            SeenCount = isCurrent ? 1 : 0,
        };

        ServiceContext.PlayerDataService.AddPlayer(player);
        this.CurrentPlayerAdded?.Invoke(player);
    }

    private Player UpdateExistingPlayer(Player player, ToadPlayer toadPlayer, bool isCurrent, ToadLocation loc)
    {
        if (isCurrent)
        {
            if (player.Customize != null && !StructuralComparisons.StructuralEqualityComparer.Equals(
                    player.Customize, toadPlayer.Customize))
            {
                PlayerChangeService.AddCustomizeHistory(player.Id, player.Customize);
            }
            
            if (player.Name != toadPlayer.Name || player.WorldId != toadPlayer.HomeWorld)
            {
                PlayerChangeService.AddNameWorldHistory(player.Id, player.Name, player.WorldId);
                ServiceContext.PlayerAlertService.SendPlayerNameWorldChangeAlert(player, player.Name, player.WorldId, toadPlayer.Name, toadPlayer.HomeWorld);
            }

            player.Key = PlayerKeyBuilder.Build(toadPlayer.Name, toadPlayer.HomeWorld);
            player.Name = toadPlayer.Name;
            player.WorldId = toadPlayer.HomeWorld;
            player.Customize = toadPlayer.Customize;
            player.FreeCompany = PlayerFCHelper.CheckFreeCompany(toadPlayer.CompanyTag, player.FreeCompany, loc.InContent());
            player.EntityId = toadPlayer.EntityId;
            player.ContentId = toadPlayer.ContentId;
            player.SeenCount += 1;
            player.LastTerritoryType = loc.TerritoryId;
            player.LastSeen = UnixTimestampHelper.CurrentTime();
            player.IsCurrent = true;
            player.IsRecent = true;
            ServiceContext.PlayerDataService.UpdatePlayer(player);
        }

        player = ServiceContext.PlayerDataService.GetPlayer(player.Id) ?? player;
        this.CurrentPlayerAdded?.Invoke(player);
        return player;
    }
}
