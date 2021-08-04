using System.Collections.Generic;
using System.Linq;

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
            : base(plugin.PluginService)
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
        public void AddEncounter(Encounter encounter)
        {
            lock (this.locker)
            {
                if (!this.currentEncounters.ContainsKey(encounter.PlayerKey))
                {
                    this.currentEncounters.Add(encounter.PlayerKey, encounter);
                }
            }

            this.InsertItem(encounter);
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
        /// Set derived fields to reduce storage needs.
        /// </summary>
        /// <param name="encounter">encounter to calculate derived fields for.</param>
        public void SetDerivedFields(Encounter encounter)
        {
            // job code
            encounter.JobCode = this.plugin.PluginService.GameData.ClassJobCode(encounter.JobId);

            // last location / content id
            var contentId = this.plugin.PluginService.GameData.ContentId(encounter.TerritoryType);
            if (contentId == 0)
            {
                var placeName = this.plugin.PluginService.GameData.PlaceName(encounter.TerritoryType);
                encounter.LocationName = string.IsNullOrEmpty(placeName) ? "Eorzea" : placeName;
            }
            else
            {
                encounter.LocationName = this.plugin.PluginService.GameData.ContentName(contentId);
            }
        }
    }
}
