using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.Logging;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using Dalamud.DrunkenToad.Helpers;

public class EncounterService
{
    public Encounter? CurrentEncounter { get; private set;  }

    public Encounter? CurrentEncounterSnapshot { get; private set;  }

    public static void UpdateEncounter(Encounter encounter) => RepositoryContext.EncounterRepository.UpdateEncounter(encounter);

    public static void EnsureNoOpenEncounters()
    {
        PluginLog.LogVerbose("Entering EncounterService.EnsureNoOpenEncounters()");
        var encounters = RepositoryContext.EncounterRepository.GetAllOpenEncounters();
        if (encounters == null || encounters.Count == 0)
        {
            PluginLog.LogVerbose("No open encounters found.");
            return;
        }

        foreach (var encounter in encounters)
        {
            PluginLog.LogVerbose($"Ending encounter: {encounter.Id}");
            encounter.Ended = encounter.Updated;
            UpdateEncounter(encounter);
            PlayerEncounterService.EndPlayerEncounters(encounter.Id);
        }
    }

    public static Encounter? GetEncounter(int id) => RepositoryContext.EncounterRepository.GetEncounter(id);

    public static void CreateEncounter(Encounter encounter) => RepositoryContext.EncounterRepository.CreateEncounter(encounter);

    public void Dispose() => this.EndCurrentEncounter();

    public Encounter? GetCurrentEncounter()
    {
        this.CurrentEncounterSnapshot = this.CurrentEncounter;
        return this.CurrentEncounterSnapshot;
    }

    public void StartCurrentEncounter(ToadLocation location)
    {
        PluginLog.LogVerbose($"Entering EncounterService.StartCurrentEncounter(): {location.TerritoryId}");
        var loc = DalamudContext.DataManager.Locations[location.TerritoryId];

        var encounter = new Encounter
        {
            TerritoryTypeId = location.TerritoryId,
        };

        CreateEncounter(encounter);
        this.CurrentEncounter = RepositoryContext.EncounterRepository.GetOpenEncounter();
        if (this.CurrentEncounter == null)
        {
            PluginLog.LogWarning("Failed to start encounter.");
            return;
        }

        this.CurrentEncounter.CategoryId = CategoryService.GetDefaultCategory(loc);
        this.CurrentEncounter.SaveEncounter = ShouldSaveEncounter(loc);
        this.CurrentEncounter.SavePlayers = ShouldSavePlayers(loc);
    }

    public void EndCurrentEncounter()
    {
        PluginLog.LogVerbose("Entering EncounterService.EndCurrentEncounter()");
        if (this.CurrentEncounter == null)
        {
            if (DalamudContext.ClientStateHandler.IsLoggedIn)
            {
                PluginLog.LogWarning("Failed to end encounter.");
            }

            return;
        }

        this.CurrentEncounter.Ended = UnixTimestampHelper.CurrentTime();
        UpdateEncounter(this.CurrentEncounter);
        this.CurrentEncounter = null;
    }

    private static bool ShouldSaveEncounter(ToadLocation loc)
    {
        PluginLog.LogVerbose($"Entering EncounterService.ShouldSaveEncounter(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.AddEncounters;
    }

    private static bool ShouldSavePlayers(ToadLocation loc)
    {
        PluginLog.LogVerbose($"Entering EncounterService.ShouldSavePlayers(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.AddPlayers;
    }
}
