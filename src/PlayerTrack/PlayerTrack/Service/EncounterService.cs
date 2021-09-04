using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <summary>
    /// Encounter service.
    /// </summary>
    public class EncounterService : BaseRepository
    {
        private readonly object locker = new ();
        private readonly SortedList<string, Encounter> currentEncounters = new ();
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncounterService"/> class.
        /// </summary>
        /// <param name="plugin">EncounterTrack plugin.</param>
        public EncounterService(PlayerTrackPlugin plugin)
            : base(PlayerTrackPlugin.GetPluginFolder())
        {
            this.plugin = plugin;
        }

        /// <summary>
        /// Delete all encounters for a player.
        /// </summary>
        /// <param name="playerKey">player key to delete encounters from.</param>
        public void DeleteEncounters(string playerKey)
        {
            lock (this.locker)
            {
                this.currentEncounters.Remove(playerKey);
            }

            this.DeleteItems<Encounter>(encounter => encounter.PlayerKey.Equals(playerKey));
        }

        /// <summary>
        /// Clear current encounters.
        /// </summary>
        public void ClearCurrentEncounters()
        {
            lock (this.locker)
            {
                this.currentEncounters.Clear();
            }
        }

        /// <summary>
        /// Gets encounters for player.
        /// </summary>
        /// <param name="playerKey">player key.</param>
        /// <returns>list of encounters.</returns>
        public IEnumerable<Encounter> GetEncountersByPlayer(string playerKey)
        {
            var encounters = this.GetItems<Encounter>(encounter => encounter.PlayerKey.Equals(playerKey)).ToList();
            foreach (var encounter in encounters)
            {
                this.SetDerivedFields(encounter);
            }

            return encounters;
        }

        /// <summary>
        /// Gets encounters.
        /// </summary>
        /// <returns>list of encounters.</returns>
        public IEnumerable<Encounter> GetEncounters()
        {
            return this.GetItems<Encounter>().ToList();
        }

        /// <summary>
        /// Add encounters in bulk (used for migration).
        /// </summary>
        /// <param name="newEncounters">list of encounters to add.</param>
        public void AddEncounters(IEnumerable<Encounter> newEncounters)
        {
            this.InsertItems(newEncounters);
            this.RebuildIndex<Encounter>(enc => enc.PlayerKey);
        }

        /// <summary>
        /// Add encounter.
        /// </summary>
        /// <param name="encounter">encounter to add.</param>
        public void AddOrUpdateEncounter(Encounter encounter)
        {
            lock (this.locker)
            {
                if (!this.currentEncounters.ContainsKey(encounter.PlayerKey))
                {
                    // use existing encounter if same territory type and within threshold to avoid spam
                    var lastEncounter = this.GetEncountersByPlayer(encounter.PlayerKey).LastOrDefault();
                    if (lastEncounter != null && (lastEncounter.TerritoryType == encounter.TerritoryType &&
                        encounter.Updated - lastEncounter.Updated < this.plugin.Configuration
                                                                        .CreateNewEncounterThreshold))
                    {
                        Logger.LogDebug($"Updating existing encounter {encounter.PlayerKey}");
                        lastEncounter.Updated = encounter.Updated;
                        this.currentEncounters.Add(encounter.PlayerKey, lastEncounter);
                        this.UpdateItem(lastEncounter);
                    }
                    else
                    {
                        Logger.LogDebug($"Adding new encounter for {encounter.PlayerKey}");
                        this.currentEncounters.Add(encounter.PlayerKey, encounter);
                        this.InsertItem(encounter);
                    }
                }
            }

            this.RebuildIndex<Encounter>(enc => enc.PlayerKey);
        }

        /// <summary>
        /// Update last updated time.
        /// </summary>
        /// <param name="playerKey">player key for encounter.</param>
        /// <param name="currentTime">current time (unix timestamp).</param>
        public void UpdateLastUpdated(string playerKey, long currentTime)
        {
            if (this.currentEncounters.ContainsKey(playerKey))
            {
                this.currentEncounters[playerKey].Updated = currentTime;
                this.UpdateItem(this.currentEncounters[playerKey]);
            }
        }

        /// <summary>
        /// Delete encounter.
        /// </summary>
        /// <param name="encounter">encounter to delete.</param>
        public void DeleteEncounter(Encounter encounter)
        {
            lock (this.locker)
            {
                if (this.currentEncounters.ContainsKey(encounter.PlayerKey))
                {
                    this.currentEncounters.Remove(encounter.PlayerKey);
                }
            }

            this.DeleteItem<Encounter>(encounter.Id);
        }

        /// <summary>
        /// Update encounter.
        /// </summary>
        /// <param name="encounter">encounter to update.</param>
        public void UpdateEncounter(Encounter encounter)
        {
            lock (this.locker)
            {
                if (this.currentEncounters.ContainsKey(encounter.PlayerKey))
                {
                    this.currentEncounters.Remove(encounter.PlayerKey);
                }
            }

            this.UpdateItem(encounter);
        }

        /// <summary>
        /// Set derived fields to reduce storage needs.
        /// </summary>
        /// <param name="encounter">encounter to calculate derived fields for.</param>
        public void SetDerivedFields(Encounter encounter)
        {
            // job code
            encounter.JobCode = PlayerTrackPlugin.DataManager.ClassJobCode(encounter.JobId);

            // last location / content id
            var contentId = PlayerTrackPlugin.DataManager.ContentId(encounter.TerritoryType);
            if (contentId == 0)
            {
                var placeName =
                    PlayerTrackPlugin.PluginInterface.Sanitizer.Sanitize(
                        PlayerTrackPlugin.DataManager.PlaceName(encounter.TerritoryType));
                encounter.LocationName = string.IsNullOrEmpty(placeName) ? "Eorzea" : placeName;
            }
            else
            {
                encounter.LocationName = PlayerTrackPlugin.DataManager.ContentName(encounter.TerritoryType);
            }
        }

        /// <summary>
        /// Delete overworld encounters.
        /// </summary>
        public void DeleteOverworldEncounters()
        {
            Task.Run(() =>
            {
                PlayerTrackPlugin.Chat.PluginPrintNotice("Starting to delete overworld encounters...this may take awhile.");
                var encounters = this.GetEncounters().ToList();
                var deleteCount = 0;
                foreach (var encounter in encounters)
                {
                    var contentId = PlayerTrackPlugin.DataManager.ContentId(encounter.TerritoryType);
                    if (contentId == 0)
                    {
                        Logger.LogDebug($"Deleting Encounter: {encounter.Id} {encounter.PlayerKey}");
                        this.DeleteEncounter(encounter);
                        deleteCount += 1;
                    }
                }

                Logger.LogInfo($"Deleted {deleteCount} encounters.");
                this.RebuildDatabase();
                PlayerTrackPlugin.Chat.PluginPrintNotice("Finished deleting overworld encounters.");
            });
        }
    }
}
