using System.Collections.Generic;
using PlayerTrack.Data;
using PlayerTrack.Domain.Common;
using PlayerTrack.Extensions;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class LocalPlayerService
{
    public static void AddOrUpdateLocalPlayer(LocalPlayerData? localPlayer)
    {
        Plugin.PluginLog.Verbose($"AddOrUpdateLocalPlayer: {localPlayer?.ContentId}");
        if (localPlayer == null)
        {
            Plugin.PluginLog.Warning("AddOrUpdateLocalPlayer: localPlayer is null");
            return;
        }

        var existingPlayer = RepositoryContext.LocalPlayerRepository.GetLocalPlayer(localPlayer.ContentId);
        if (existingPlayer != null)
        {
            Plugin.PluginLog.Verbose($"UpdateLocalPlayer: {localPlayer.ContentId}");
            existingPlayer.Name = localPlayer.Name;
            existingPlayer.WorldId = localPlayer.HomeWorld;
            existingPlayer.Key = PlayerKeyBuilder.Build(localPlayer.Name, localPlayer.HomeWorld);
            existingPlayer.Customize = localPlayer.Customize;
            RepositoryContext.LocalPlayerRepository.UpdateLocalPlayer(existingPlayer);
        }
        else
        {
            Plugin.PluginLog.Verbose($"CreateLocalPlayer: {localPlayer.ContentId}");
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
        var worldName = Sheets.GetWorldNameById(localPlayer?.WorldId ?? 0);
        return $"{localPlayer?.Name}@{worldName}";
    }

    public static List<LocalPlayer> GetLocalPlayers()
    {
        return RepositoryContext.LocalPlayerRepository.GetAllLocalPlayers();
    }

    public static List<string> GetLocalPlayerNames()
    {
        var names = new List<string>();
        foreach (var localPlayer in GetLocalPlayers())
        {
            var worldName = Sheets.GetWorldNameById(localPlayer.WorldId);
            names.Add($"{localPlayer.Name}@{worldName}");
        }

        return names;
    }

    public static uint GetLocalPlayerDataCenter()
    {
        var localPlayer = Plugin.ClientStateHandler.GetLocalPlayer();
        return localPlayer == null ? 0 : Sheets.Worlds[localPlayer.HomeWorld].DataCenterId;
    }

    public static void DeleteLocalPlayer(LocalPlayer localPlayer)
    {
        Plugin.PluginLog.Verbose($"DeleteLocalPlayer: {localPlayer.Id}");
        RepositoryContext.LocalPlayerRepository.DeleteLocalPlayer(localPlayer.Id);
        SocialListService.DeleteSocialLists(localPlayer.ContentId);
    }
}
