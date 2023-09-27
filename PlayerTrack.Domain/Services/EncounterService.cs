using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core.Enums;
using Dalamud.DrunkenToad.Helpers;

public class EncounterService
{
    private const long NinetyDaysInMilliseconds = 7776000000;
    private const int MaxBatchSize = 500;

    public Encounter? CurrentEncounter { get; private set;  }

    public Encounter? CurrentEncounterSnapshot { get; private set;  }

    public static void UpdateEncounter(Encounter encounter) => RepositoryContext.EncounterRepository.UpdateEncounter(encounter);

    public static void EnsureNoOpenEncounters()
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterService.EnsureNoOpenEncounters()");
        var encounters = RepositoryContext.EncounterRepository.GetAllOpenEncounters();
        if (encounters == null || encounters.Count == 0)
        {
            DalamudContext.PluginLog.Verbose("No open encounters found.");
            return;
        }

        foreach (var encounter in encounters)
        {
            DalamudContext.PluginLog.Verbose($"Ending encounter: {encounter.Id}");
            encounter.Ended = encounter.Updated;
            UpdateEncounter(encounter);
            PlayerEncounterService.EndPlayerEncounters(encounter.Id);
        }
    }

    public static Encounter? GetEncounter(int id) => RepositoryContext.EncounterRepository.GetEncounter(id);

    public static void CreateEncounter(Encounter encounter) => RepositoryContext.EncounterRepository.CreateEncounter(encounter);

    public static int GetEncountersCount() => RepositoryContext.EncounterRepository.GetAllEncounters()?.Count ?? 0;

    public int GetEncountersForDeletionCount() => this.GetEncountersForDeletion().Count;

    public void Dispose() => this.EndCurrentEncounter();

    public Encounter? GetCurrentEncounter()
    {
        this.CurrentEncounterSnapshot = this.CurrentEncounter;
        return this.CurrentEncounterSnapshot;
    }

    public void StartCurrentEncounter(ToadLocation location)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterService.StartCurrentEncounter(): {location.TerritoryId}");
        var loc = DalamudContext.DataManager.Locations[location.TerritoryId];

        var encounter = new Encounter
        {
            TerritoryTypeId = location.TerritoryId,
        };

        CreateEncounter(encounter);
        this.CurrentEncounter = RepositoryContext.EncounterRepository.GetOpenEncounter();
        if (this.CurrentEncounter == null)
        {
            DalamudContext.PluginLog.Warning("Failed to start encounter.");
            return;
        }

        this.CurrentEncounter.CategoryId = CategoryService.GetDefaultCategory(loc);
        this.CurrentEncounter.SaveEncounter = ShouldSaveEncounter(loc);
        this.CurrentEncounter.SavePlayers = ShouldSavePlayers(loc);
    }

    public void EndCurrentEncounter()
    {
        DalamudContext.PluginLog.Verbose("Entering EncounterService.EndCurrentEncounter()");
        if (this.CurrentEncounter == null)
        {
            if (DalamudContext.ClientStateHandler.IsLoggedIn)
            {
                DalamudContext.PluginLog.Warning("Failed to end encounter.");
            }

            return;
        }

        this.CurrentEncounter.Ended = UnixTimestampHelper.CurrentTime();
        UpdateEncounter(this.CurrentEncounter);
        this.CurrentEncounter = null;
    }

    public void DeleteEncounters()
    {
        var encounters = this.GetEncountersForDeletion();
        var encounterIds = encounters.Select(e => e.Id).ToList();

        for (var i = 0; i < encounterIds.Count; i += MaxBatchSize)
        {
            var currentBatch = encounterIds.Skip(i).Take(MaxBatchSize).ToList();
            RepositoryContext.EncounterRepository.DeleteEncountersWithRelations(currentBatch);
        }

        RepositoryContext.RunMaintenanceChecks(true);
    }

    private static bool ShouldSaveEncounter(ToadLocation loc)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterService.ShouldSaveEncounter(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.AddEncounters;
    }

    private static bool ShouldSavePlayers(ToadLocation loc)
    {
        DalamudContext.PluginLog.Verbose($"Entering EncounterService.ShouldSavePlayers(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.AddPlayers;
    }

    private List<Encounter> GetEncountersForDeletion()
    {
        var allEncounters = RepositoryContext.EncounterRepository.GetAllEncounters();
        if (allEncounters == null)
        {
            return new List<Encounter>();
        }

        var currentTimeUnix = UnixTimestampHelper.CurrentTime();
        var options = ServiceContext.ConfigService.GetConfig().EncounterDataActionOptions;
        var encountersForDeletion = new List<Encounter>();

        foreach (var encounter in allEncounters)
        {
            var location = DalamudContext.DataManager.Locations[encounter.TerritoryTypeId];

            var shouldDelete =
                !(options.KeepEncountersInOverworld && location.LocationType == ToadLocationType.Overworld) &&
                !(options.KeepEncountersInNormalContent && location.LocationType == ToadLocationType.Content) &&
                !(options.KeepEncountersInHighEndContent && location.LocationType == ToadLocationType.HighEndContent) &&
                !(options.KeepEncountersFromLast90Days && currentTimeUnix - encounter.Created <= NinetyDaysInMilliseconds);

            if (shouldDelete)
            {
                encountersForDeletion.Add(encounter);
            }
        }

        if (this.CurrentEncounter != null && encountersForDeletion.Any(encounter => encounter.Id == this.CurrentEncounter?.Id))
        {
            encountersForDeletion.Remove(this.CurrentEncounter);
        }

        return encountersForDeletion;
    }
}
