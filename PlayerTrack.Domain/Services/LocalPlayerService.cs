using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Extensions;
using PlayerTrack.Domain.Common;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class LocalPlayerService
{
    public static void AddOrUpdateLocalPlayer(ToadLocalPlayer? localPlayer)
    {
        DalamudContext.PluginLog.Verbose($"AddOrUpdateLocalPlayer: {localPlayer?.ContentId}");
        if (localPlayer == null)
        {
            DalamudContext.PluginLog.Warning("AddOrUpdateLocalPlayer: localPlayer is null");
            return;
        }
        
        var existingPlayer = RepositoryContext.LocalPlayerRepository.GetLocalPlayer(localPlayer.ContentId);
        if (existingPlayer != null)
        {
            DalamudContext.PluginLog.Verbose($"UpdateLocalPlayer: {localPlayer.ContentId}");
            existingPlayer.Name = localPlayer.Name;
            existingPlayer.WorldId = localPlayer.HomeWorld;
            existingPlayer.Key = PlayerKeyBuilder.Build(localPlayer.Name, localPlayer.HomeWorld);
            existingPlayer.Customize = localPlayer.Customize;
            RepositoryContext.LocalPlayerRepository.UpdateLocalPlayer(existingPlayer);
        }
        else
        {
            DalamudContext.PluginLog.Verbose($"CreateLocalPlayer: {localPlayer.ContentId}");
            RepositoryContext.LocalPlayerRepository.CreateLocalPlayer(new LocalPlayer
            {
                ContentId = localPlayer.ContentId,
                Name = localPlayer.Name,
                WorldId = localPlayer.HomeWorld,
                Key = PlayerKeyBuilder.Build(localPlayer.Name, localPlayer.HomeWorld),
                Customize = localPlayer.Customize
            });
        }
    }

    public static LocalPlayer? GetLocalPlayer(ulong contentId)
    {
        return RepositoryContext.LocalPlayerRepository.GetLocalPlayer(contentId);
    }
    
    public static string GetLocalPlayerFullName(ulong contentId)
    {
        var localPlayer = RepositoryContext.LocalPlayerRepository.GetLocalPlayer(contentId);
        var worldName = DalamudContext.DataManager.GetWorldNameById(localPlayer?.WorldId ?? 0);
        return $"{localPlayer?.Name}@{worldName}";
    }
    
    public static List<LocalPlayer> GetLocalPlayers()
    {
        return RepositoryContext.LocalPlayerRepository.GetAllLocalPlayers();
    }

    public static List<string> GetLocalPlayerNames()
    {
        var names = new List<string>();
        var localPlayers = GetLocalPlayers();
        foreach (var localPlayer in localPlayers)
        {
            var worldName = DalamudContext.DataManager.GetWorldNameById(localPlayer.WorldId);
            names.Add($"{localPlayer.Name}@{worldName}");
        }

        return names;
    }

    public static uint GetLocalPlayerDataCenter()
    {
        var localPlayer = DalamudContext.ClientStateHandler.GetLocalPlayer();
        return localPlayer == null ? 0 : DalamudContext.DataManager.Worlds[localPlayer.HomeWorld].DataCenterId;
    }
    
    public static void DeleteLocalPlayer(LocalPlayer localPlayer)
    {
        DalamudContext.PluginLog.Verbose($"DeleteLocalPlayer: {localPlayer.Id}");
        RepositoryContext.LocalPlayerRepository.DeleteLocalPlayer(localPlayer.Id);
        SocialListService.DeleteSocialLists(localPlayer.ContentId);
    }
}