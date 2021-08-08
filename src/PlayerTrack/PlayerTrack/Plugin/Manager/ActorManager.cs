using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using Dalamud.DrunkenToad;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;

using Timer = System.Timers.Timer;

namespace PlayerTrack
{
    /// <summary>
    /// Process actors and generate events.
    /// </summary>
    public class ActorManager
    {
        private readonly Dictionary<int, Player> playerList = new ();
        private readonly Timer timer;
        private readonly PlayerTrackPlugin plugin;
        private readonly object locker = new ();
        private ushort territoryType;
        private ushort nextTerritoryType;
        private uint contentId;
        private bool isProcessing;
        private bool needsUpdate;
        private bool territoryIsChanged;
        private Actor[]? actorTable;
        private int? localPlayerActorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorManager"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin..</param>
        public ActorManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.SetLocationProperties(this.plugin.PluginService.ClientState.TerritoryType());
            this.plugin.PluginService.ClientState.OnTerritoryChanged += this.OnTerritoryChanged;
            this.plugin.PluginService.PluginInterface.Framework.OnUpdateEvent += this.OnFrameworkUpdate;
            this.plugin.PluginService.PluginInterface.ClientState.OnLogout += this.OnLogout;
            this.timer = new Timer { Interval = 1000, Enabled = false };
            this.timer.Elapsed += this.OnTimerElapsed;
        }

        /// <summary>
        /// Start service.
        /// </summary>
        public void Start()
        {
            this.needsUpdate = true;
            this.timer.Enabled = true;
        }

        /// <summary>
        /// Dispose service.
        /// </summary>
        public void Dispose()
        {
            this.timer.Enabled = false;
            this.timer.Elapsed -= this.OnTimerElapsed;
            this.plugin.PluginService.ClientState.OnTerritoryChanged -= this.OnTerritoryChanged;
            this.plugin.PluginService.PluginInterface.Framework.OnUpdateEvent -= this.OnFrameworkUpdate;
            this.plugin.PluginService.PluginInterface.ClientState.OnLogout -= this.OnLogout;
            this.timer.Dispose();
        }

        /// <summary>
        /// Clear character from player list (used after deletions).
        /// </summary>
        /// <param name="actorId">actorId of player to remove.</param>
        public void ClearPlayerCharacter(int actorId)
        {
            lock (this.locker)
            {
                if (this.playerList.ContainsKey(actorId))
                {
                    this.playerList.Remove(actorId);
                }
            }
        }

        /// <summary>
        /// Get actor by id.
        /// </summary>
        /// <param name="actorId">actorId of player to remove.</param>
        /// <returns>actor or null.</returns>
        public PlayerCharacter? GetPlayerCharacter(uint actorId)
        {
            try
            {
                lock (this.locker)
                {
                    if (this.actorTable != null)
                    {
                     return (PlayerCharacter?)this.actorTable.FirstOrDefault(
                         actor => actor.ActorId == actorId && actor is PlayerCharacter);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        /// <summary>
        /// Get actor by name.
        /// </summary>
        /// <param name="playerName">name of player to find.</param>
        /// <param name="worldId">world id of player to find (optional).</param>
        /// <returns>actor or null.</returns>
        public int? GetPlayerCharacter(string playerName, uint worldId = 0)
        {
            try
            {
                lock (this.locker)
                {
                    if (this.actorTable != null)
                    {
                        if (worldId != 0)
                        {
                            return this.actorTable.FirstOrDefault(
                                actor => actor is PlayerCharacter character &&
                                         character.Name.Equals(playerName) &&
                                         character.HomeWorld.Id == worldId)?.ActorId;
                        }

                        return this.actorTable.FirstOrDefault(
                            actor => actor is PlayerCharacter character &&
                                     character.Name.Equals(playerName))?.ActorId;
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        private void OnLogout(object sender, EventArgs e)
        {
            lock (this.locker)
            {
                this.plugin.PlayerService.RemoveCurrentPlayers();
                this.playerList.Clear();
                this.actorTable = null;
            }
        }

        private bool ShouldProcess()
        {
            if (this.plugin.Configuration.RestrictInCombat &&
                this.plugin.PluginService.ClientState.Condition.InCombat()) return false;
            var restrict =
                ContentRestrictionType.GetContentRestrictionTypeByIndex(
                    this.plugin.Configuration.RestrictAddUpdatePlayers);
            if (restrict == ContentRestrictionType.ContentOnly && !this.plugin.PluginService.InContent()) return false;
            if (restrict == ContentRestrictionType.HighEndDutyOnly &&
                !this.plugin.PluginService.InHighEndDuty()) return false;
            return true;
        }

        private void GetPlayerCharacters()
        {
            try
            {
                // skip if waiting on actor table update
                if (this.needsUpdate)
                {
                    return;
                }

                // skip if no location (can happen when zoning)
                if (this.territoryType == 0)
                {
                    return;
                }

                // skip if local player is invalid
                if (this.localPlayerActorId is null or 0)
                {
                    this.needsUpdate = true;
                    return;
                }

                // get latest actors
                List<PlayerCharacter?> currentActors;
                lock (this.locker)
                {
                    if (this.actorTable == null)
                    {
                        this.needsUpdate = true;
                        return;
                    }

                    currentActors = this.actorTable
                                        .Where(actor => actor.IsValidPlayerCharacter() &&
                                                        actor.ActorId != this.localPlayerActorId)
                                        .Select(actor => actor as PlayerCharacter).ToList();
                }

                // prepare for updates
                this.needsUpdate = true;
                var anyUpdate = false;
                var currentTime = DateUtil.CurrentTime();
                var defaultCategoryId = this.plugin.CategoryService.GetDefaultCategory().Id;
                var updateEncounter = this.contentId != 0 ||
                                      (!this.plugin.Configuration.RestrictEncountersToContent && this.contentId == 0);

                // check for removed actors
                foreach (var player in this.playerList.ToList())
                {
                    // remove existing player
                    if (currentActors.All(character => character!.ActorId != player.Value.ActorId))
                    {
                        anyUpdate = true;
                        this.playerList.Remove(player.Value.ActorId);
                        player.Value.IsCurrent = false;
                        player.Value.Updated = currentTime;
                        this.plugin.PlayerService.RemovePlayerFromCurrentPlayers(player.Value);
                        if (updateEncounter)
                        {
                            this.plugin.EncounterService.UpdateLastUpdated(player.Value.Key, currentTime);
                        }
                    }
                }

                // check for new actors
                if (currentActors.Count > 0)
                {
                    foreach (var character in currentActors)
                    {
                        if (character == null) continue;
                        var playerKey =
                            PlayerService.BuildPlayerKey(character.Name, character.HomeWorld.Id);

                        // create new player/encounter if new occurence
                        if (!this.playerList.ContainsKey(character!.ActorId))
                        {
                            // add/update encounter if needed
                            if (updateEncounter)
                            {
                                var encounter = new Encounter
                                {
                                    PlayerKey = playerKey,
                                    Created = currentTime,
                                    Updated = currentTime,
                                    TerritoryType = this.territoryType,
                                    JobId = character.ClassJob.Id,
                                    JobLvl = character.Level,
                                };
                                this.plugin.EncounterService.SetDerivedFields(encounter);
                                this.plugin.EncounterService.AddEncounter(encounter);
                            }

                            // setup player for add/update
                            var newPlayer = new Player
                            {
                                Key = playerKey,
                                ActorId = character.ActorId,
                                Names = new List<string>
                                {
                                    character.Name,
                                },
                                HomeWorlds = new List<KeyValuePair<uint, string>>
                                {
                                    new (character.HomeWorld.Id, this.plugin.PluginService.GameData.WorldName(
                                            character.HomeWorld.Id)),
                                },
                                FreeCompany = Player.DetermineFreeCompany(this.contentId, character.CompanyTag),
                                Customize = character.Customize,
                                LastTerritoryType = this.territoryType,
                                Created = currentTime,
                                Updated = currentTime,
                                CategoryId = defaultCategoryId,
                                IsCurrent = true,
                                IsRecent = true,
                            };
                            anyUpdate = true;
                            this.plugin.PlayerService.SetDerivedFields(newPlayer);
                            this.playerList.Add(character.ActorId, newPlayer);
                            this.plugin.PlayerService.AddOrUpdatePlayer(newPlayer);
                        }
                    }
                }

                if (anyUpdate)
                {
                    this.plugin.PlayerService.UpdateViewPlayers();
                }
            }
            catch (Exception ex)
            {
                this.isProcessing = false;
                this.needsUpdate = true;
                Logger.LogError(ex, "Failed to get players.");
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // check if should continue or is already in-progress
            if (!this.ShouldProcess() || this.isProcessing) return;

            // set to in-progress to avoid concurrent runs
            this.isProcessing = true;

            // update location info if territory has changed since last check
            if (this.territoryIsChanged)
            {
                this.plugin.PlayerService.RemoveCurrentPlayers();
                this.playerList.Clear();
                this.territoryIsChanged = false;
                lock (this.locker)
                {
                    this.actorTable = null;
                }
            }

            // process actors
            this.GetPlayerCharacters();
            this.isProcessing = false;
        }

        private void OnTerritoryChanged(ushort newTerritoryType)
        {
            this.territoryIsChanged = true;
            this.nextTerritoryType = newTerritoryType;
            this.needsUpdate = true;
        }

        private void SetLocationProperties(ushort newTerritoryType)
        {
            this.territoryType = newTerritoryType;
            this.contentId = this.plugin.PluginService.GameData.ContentId(this.territoryType);
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            // check if should process or needs to
            if (!this.ShouldProcess() || !this.needsUpdate) return;
            try
            {
                lock (this.locker)
                {
                    // reset fields if territory changed since last update
                    if (this.territoryIsChanged)
                    {
                        this.SetLocationProperties(this.nextTerritoryType);
                        this.plugin.PlayerService.RemoveCurrentPlayers();
                        this.playerList.Clear();
                        this.territoryIsChanged = false;
                    }

                    // copy actor info
                    this.actorTable = this.plugin.PluginService.PluginInterface.ClientState.Actors.ToArray();
                    this.localPlayerActorId = this.plugin.PluginService.PluginInterface.ClientState.LocalPlayer?.ActorId;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get latest actor table.");
            }

            this.needsUpdate = false;
        }
    }
}
