using System;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using System.Collections.Generic;
using PlayerTrack.Data;

namespace PlayerTrack.Domain;

public class PlayerEncounterService
{
    public static void UpdatePlayerId(int originalPlayerId, int newPlayerId) =>
        RepositoryContext.PlayerEncounterRepository.UpdatePlayerId(originalPlayerId, newPlayerId);

    public static List<PlayerEncounter>? GetPlayerEncountersByPlayer(int playerId) =>
        RepositoryContext.PlayerEncounterRepository.GetAllByPlayerId(playerId);

    public static void DeletePlayerEncountersByPlayer(int playerId) =>
        RepositoryContext.PlayerEncounterRepository.DeleteAllByPlayerId(playerId);

    public static int CreatePlayerEncounter(PlayerData toadPlayer, Player player)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerEncounterService.CreatePlayerEncounter(): {toadPlayer.ContentId}, {player.Id}");
        if (player.Id == 0)
        {
            Plugin.PluginLog.Warning("Player Id is 0, cannot create player encounter.");
            return 0;
        }

        var encId = ServiceContext.EncounterService.CurrentEncounter?.Id ?? 0;
        if (encId == 0)
        {
            Plugin.PluginLog.Verbose("Encounter Id is 0, cannot create player encounter.");
            return 0;
        }

        var playerEncounter = RepositoryContext.PlayerEncounterRepository.GetByPlayerIdAndEncId(player.Id, encId);
        if (playerEncounter != null)
            return playerEncounter.Id;

        playerEncounter = new PlayerEncounter
        {
            PlayerId = player.Id,
            EncounterId = encId,
            JobId = toadPlayer.ClassJob,
            JobLvl = toadPlayer.Level,
        };
        return RepositoryContext.PlayerEncounterRepository.CreatePlayerEncounter(playerEncounter);
    }

    public static LocationData GetEncounterLocation()
    {
        Plugin.PluginLog.Verbose("Entering PlayerEncounterService.GetEncounterLocation()");
        var lastLocId = ServiceContext.EncounterService.CurrentEncounter?.TerritoryTypeId ?? 0;
        return Sheets.Locations[lastLocId];
    }

    public static void EndPlayerEncounters(int encounterId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerEncounterService.EndPlayerEncounters(): {encounterId}");
        var playerEncounters = RepositoryContext.PlayerEncounterRepository.GetAllByEncounterId(encounterId);
        if (playerEncounters == null || playerEncounters.Count == 0)
            return;

        var ended = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var playerEncounter in playerEncounters)
        {
            playerEncounter.Ended = ended;
            RepositoryContext.PlayerEncounterRepository.UpdatePlayerEncounter(playerEncounter);
        }
    }

    public static void EndPlayerEncounter(Player player, Encounter? encounter)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerEncounterService.EndPlayerEncounter(): {player.Id}, {encounter?.Id}");
        if (encounter == null || encounter.Id == 0)
        {
            Plugin.PluginLog.Verbose("Encounter Id is 0, cannot end player encounter.");
            return;
        }

        if (player.OpenPlayerEncounterId == 0)
        {
            Plugin.PluginLog.Verbose("Player open encounter Id is 0, cannot end player encounter.");
            return;
        }

        if (!encounter.SaveEncounter)
        {
            Plugin.PluginLog.Verbose("Encounter is not set to save players, so won't try ending.");
            return;
        }

        var pEnc = RepositoryContext.PlayerEncounterRepository.GetByPlayerIdAndEncId(player.Id, encounter.Id);
        if (pEnc == null)
            return;

        pEnc.Ended = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        RepositoryContext.PlayerEncounterRepository.UpdatePlayerEncounter(pEnc);
    }
}
