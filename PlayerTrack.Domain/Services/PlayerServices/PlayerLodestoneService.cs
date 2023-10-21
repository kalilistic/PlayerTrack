using Dalamud.DrunkenToad.Helpers;

namespace PlayerTrack.Domain;

using System.Diagnostics;
using Dalamud.DrunkenToad.Core;

using Infrastructure;
using Models;

public class PlayerLodestoneService
{
    public static void CreateLodestoneLookup(int playerId, string name, string worldName) => RepositoryContext.LodestoneRepository.CreateLodestoneLookup(new LodestoneLookup
    {
        PlayerId = playerId,
        PlayerName = name,
        WorldName = worldName,
    });

    public static void CreateLodestoneLookup(int playerId, string name, uint worldId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.CreateLodestoneLookup(): {playerId}, {name}, {worldId}");
        var isTestDC = DalamudContext.DataManager.IsTestDC(worldId);
        if (isTestDC)
        {
            DalamudContext.PluginLog.Debug("Cannot create lodestone lookup for test DC.");
            var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
            if (player == null)
            {
                DalamudContext.PluginLog.Warning("Player not found for test DC.");
                return;
            }

            player.LodestoneStatus = LodestoneStatus.NotApplicable;
            player.LodestoneVerifiedOn = UnixTimestampHelper.CurrentTime();
            ServiceContext.PlayerDataService.UpdatePlayer(player);
            return;
        }
        
        var worldName = DalamudContext.DataManager.GetWorldNameById(worldId);
        CreateLodestoneLookup(playerId, name, worldName);
    }

    public static void ResetLodestoneLookup(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.ResetLodestoneLookup(): {playerId}");
        var lookup = RepositoryContext.LodestoneRepository.GetLodestoneLookupByPlayerId(playerId);
        if (lookup == null)
        {
            return;
        }

        lookup.Reset();
        RepositoryContext.LodestoneRepository.UpdateLodestoneLookup(lookup);
    }

    public static void DeleteLookupsByPlayer(int playerId) => RepositoryContext.LodestoneRepository.DeleteLodestoneRequestByPlayerId(playerId);

    public static void UpdateLodestone(LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.UpdateLodestone(): {lookup.PlayerId}, {lookup.LodestoneId}, {lookup.LodestoneStatus}");
        var player = ServiceContext.PlayerDataService.GetPlayer(lookup.PlayerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning("Player not found");
            return;
        }

        player.LodestoneId = lookup.LodestoneId;
        player.LodestoneStatus = lookup.LodestoneStatus;
        player.LodestoneVerifiedOn = lookup.Updated;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        PlayerProcessService.CheckForDuplicates(player);
    }

    public static void ResetLodestone(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerLodestoneService.ResetLodestone(): {playerId}");
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning("Player not found");
            return;
        }

        player.LodestoneId = 0;
        player.LodestoneStatus = LodestoneStatus.Unverified;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        ResetLodestoneLookup(playerId);
    }

    public static void OpenLodestoneProfile(uint lodestoneId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneService.OpenLodestoneProfile(): {lodestoneId}");
        if (lodestoneId == 0)
        {
            DalamudContext.PluginLog.Warning("LodestoneId is 0, cannot open lodestone profile.");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "https://" + ServiceContext.ConfigService.GetConfig().LodestoneLocale +
                       ".finalfantasyxiv.com/lodestone/character/" + lodestoneId,
            UseShellExecute = true,
        });
    }
}
