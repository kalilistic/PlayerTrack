using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Collections.Generic;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Logging;

public class PlayerEncounterService
{
    public static List<PlayerEncounter>? GetPlayerEncountersByPlayer(int playerId) => RepositoryContext.PlayerEncounterRepository.GetAllByPlayerId(playerId);

    public static void DeletePlayerEncountersByPlayer(int playerId) => RepositoryContext.PlayerEncounterRepository.DeleteAllByPlayerId(playerId);

    public static void CreatePlayerEncounter(ToadPlayer toadPlayer, Player player)
    {
        PluginLog.LogVerbose($"Entering PlayerEncounterService.CreatePlayerEncounter(): {toadPlayer.Id}, {player.Id}");
        if (player.Id == 0)
        {
            PluginLog.LogWarning("Player Id is 0, cannot create player encounter.");
            return;
        }

        var encId = ServiceContext.EncounterService.CurrentEncounter?.Id ?? 0;
        if (encId == 0)
        {
            PluginLog.LogWarning("Encounter Id is 0, cannot create player encounter.");
            return;
        }

        var playerEncounter = RepositoryContext.PlayerEncounterRepository.GetByPlayerIdAndEncId(player.Id, encId);
        if (playerEncounter == null)
        {
            playerEncounter = new PlayerEncounter
            {
                PlayerId = player.Id,
                EncounterId = encId,
                JobId = toadPlayer.ClassJob,
                JobLvl = toadPlayer.Level,
            };
            RepositoryContext.PlayerEncounterRepository.CreatePlayerEncounter(playerEncounter);
        }
    }

    public static ToadLocation GetEncounterLocation()
    {
        PluginLog.LogVerbose($"Entering PlayerEncounterService.GetEncounterLocation()");
        var lastLocId = ServiceContext.EncounterService.CurrentEncounter?.TerritoryTypeId ?? 0;
        return DalamudContext.DataManager.Locations[lastLocId];
    }

    public static void UpdatePlayerId(int oldestPlayerId, int newPlayerId) => RepositoryContext.PlayerEncounterRepository.UpdatePlayerId(oldestPlayerId, newPlayerId);

    public static void EndPlayerEncounters(int encounterId)
    {
        PluginLog.LogVerbose($"Entering PlayerEncounterService.EndPlayerEncounters(): {encounterId}");
        var playerEncounters = RepositoryContext.PlayerEncounterRepository.GetAllByEncounterId(encounterId);
        if (playerEncounters == null || playerEncounters.Count == 0)
        {
            return;
        }

        var ended = UnixTimestampHelper.CurrentTime();
        foreach (var playerEncounter in playerEncounters)
        {
            playerEncounter.Ended = ended;
            RepositoryContext.PlayerEncounterRepository.UpdatePlayerEncounter(playerEncounter);
        }
    }

    public static void EndPlayerEncounter(Player player, Encounter? encounter)
    {
        PluginLog.LogVerbose($"Entering PlayerEncounterService.EndPlayerEncounter(): {player.Id}, {encounter?.Id}");
        if (encounter == null || encounter.Id == 0)
        {
            PluginLog.LogWarning("Encounter Id is 0, cannot end player encounter.");
            return;
        }

        if (player.OpenPlayerEncounterId == 0)
        {
            PluginLog.LogVerbose("Player open encounter Id is 0, cannot end player encounter.");
            return;
        }

        if (!encounter.SaveEncounter)
        {
            PluginLog.LogVerbose("Encounter is not set to save players, so won't try ending.");
            return;
        }

        var pEnc = RepositoryContext.PlayerEncounterRepository.GetByPlayerIdAndEncId(player.Id, encounter.Id);
        if (pEnc == null)
        {
            PluginLog.LogWarning("Player encounter is null, cannot end player encounter.");
            return;
        }

        pEnc.Ended = UnixTimestampHelper.CurrentTime();
        RepositoryContext.PlayerEncounterRepository.UpdatePlayerEncounter(pEnc);
    }
}
