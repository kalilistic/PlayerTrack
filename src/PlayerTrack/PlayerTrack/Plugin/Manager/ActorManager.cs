using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using Dalamud.DrunkenToad;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

using Timer = System.Timers.Timer;

namespace PlayerTrack
{
    /// <summary>
    /// Process actors and generate events.
    /// </summary>
    public class ActorManager
    {
        private readonly Dictionary<uint, Player> playerList = new();
        private readonly Timer timer;
        private readonly PlayerTrackPlugin plugin;
        private readonly object locker = new();
        private long eventId;
        private ushort territoryType;
        private ushort nextTerritoryType;
        private uint contentId;
        private bool isProcessing;
        private bool needsUpdate;
        private bool territoryIsChanged;
        private GameObject[]? actorTable;
        private uint localPlayerActorId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorManager"/> class.
        /// </summary>
        /// <param name="plugin">player track plugin..</param>
        public ActorManager(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.SetLocationProperties(PlayerTrackPlugin.ClientState.TerritoryType);
            PlayerTrackPlugin.ClientState.TerritoryChanged += this.TerritoryChanged;
            PlayerTrackPlugin.Framework.Update += this.Update;
            PlayerTrackPlugin.ClientState.Logout += this.Logout;
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
            PlayerTrackPlugin.ClientState.TerritoryChanged -= this.TerritoryChanged;
            PlayerTrackPlugin.Framework.Update -= this.Update;
            PlayerTrackPlugin.ClientState.Logout -= this.Logout;
            this.timer.Dispose();
        }

        /// <summary>
        /// Clear character from player list (used after deletions).
        /// </summary>
        /// <param name="actorId">actorId of player to remove.</param>
        public void ClearPlayerCharacter(uint actorId)
        {
            lock (this.locker)
            {
                if (this.playerList.ContainsKey(actorId))
                {
                    this.playerList.Remove(actorId);
                }
            }
        }

        private void Logout(object? sender, EventArgs e)
        {
            lock (this.locker)
            {
                try
                {
                    this.plugin.PlayerService.RemoveCurrentPlayers();
                    this.playerList.Clear();
                    this.actorTable = null;
                }
                catch (Exception err)
                {
                    Logger.LogError(err, "Failed to handle logout event.");
                }
            }
        }

        private bool ShouldProcess()
        {
            if (this.plugin.Configuration.RestrictInCombat &&
                PlayerTrackPlugin.Condition.InCombat()) return false;
            var restrict =
                ContentRestrictionType.GetContentRestrictionTypeByIndex(
                    this.plugin.Configuration.RestrictAddUpdatePlayers);
            if (restrict == ContentRestrictionType.Never) return false;
            if (restrict == ContentRestrictionType.ContentOnly && !PlayerTrackPlugin.DataManager.InContent(PlayerTrackPlugin.ClientState.TerritoryType)) return false;
            if (restrict == ContentRestrictionType.HighEndDutyOnly &&
                !PlayerTrackPlugin.DataManager.InHighEndDuty(PlayerTrackPlugin.ClientState.TerritoryType)) return false;
            return true;
        }

        private bool SetUpdateEncounter()
        {
            var restrict =
                ContentRestrictionType.GetContentRestrictionTypeByIndex(
                    this.plugin.Configuration.RestrictAddEncounters);
            if (restrict == ContentRestrictionType.Never) return false;
            if (restrict == ContentRestrictionType.ContentOnly && !PlayerTrackPlugin.DataManager.InContent(PlayerTrackPlugin.ClientState.TerritoryType)) return false;
            if (restrict == ContentRestrictionType.HighEndDutyOnly &&
                !PlayerTrackPlugin.DataManager.InHighEndDuty(PlayerTrackPlugin.ClientState.TerritoryType)) return false;
            return true;
        }

        private void GetPlayerCharacters()
        {
            try
            {
                // skip if shouldn't process per conditions
                if (!this.ShouldProcess())
                {
                    return;
                }

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
                if (this.localPlayerActorId == 0)
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

                    currentActors = this.actorTable!
                                        .Where(actor => actor.IsValidPlayerCharacter() &&
                                                        actor.ObjectId != this.localPlayerActorId)
                                        .Select(actor => actor as PlayerCharacter).ToList();
                }

                // prepare for updates
                this.needsUpdate = true;
                var currentTime = DateUtil.CurrentTime();
                var defaultCategoryId = this.plugin.CategoryService.GetDefaultCategory().Id;
                var updateEncounter = this.SetUpdateEncounter();

                // check for removed actors
                foreach (var player in this.playerList.ToList())
                {
                    // remove existing player
                    if (currentActors.All(character => character!.ObjectId != player.Value.ActorId))
                    {
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
                            PlayerService.BuildPlayerKey(character.Name.ToString(), character.HomeWorld.Id);

                        // create new player/encounter if new occurence
                        if (!this.playerList.ContainsKey(character!.ObjectId))
                        {
                            // add/update encounter if needed
                            if (updateEncounter)
                            {
                                var encounter = new Encounter
                                {
                                    PlayerKey = playerKey,
                                    Created = currentTime,
                                    Updated = currentTime,
                                    EventId = this.eventId,
                                    TerritoryType = this.territoryType,
                                    JobId = character.ClassJob.Id,
                                    JobLvl = character.Level,
                                };
                                this.plugin.EncounterService.SetDerivedFields(encounter);
                                this.plugin.EncounterService.AddOrUpdateEncounter(encounter);
                            }

                            // setup player for add/update
                            var newPlayer = new Player
                            {
                                Key = playerKey,
                                ActorId = character.ObjectId,
                                Names = new List<string>
                                {
                                    character.Name.ToString(),
                                },
                                HomeWorlds = new List<KeyValuePair<uint, string>>
                                {
                                    new(character.HomeWorld.Id, character.HomeWorld.GameData?.Name.ToString()!),
                                },
                                FreeCompany = Player.DetermineFreeCompany(this.contentId, character.CompanyTag.ToString()),
                                Customize = character.Customize,
                                LastTerritoryType = this.territoryType,
                                Created = currentTime,
                                Updated = currentTime,
                                CategoryId = defaultCategoryId,
                                IsCurrent = true,
                                IsRecent = true,
                            };
                            if (newPlayer.IsValidPlayer())
                            {
                                this.plugin.PlayerService.SetDerivedFields(newPlayer);
                                this.playerList.Add(character.ObjectId, newPlayer);
                                this.plugin.PlayerService.AddOrUpdatePlayer(newPlayer);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.isProcessing = false;
                this.needsUpdate = true;
                Logger.LogError(ex, "Failed to get players.");
            }
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            // check if should continue or is already in-progress
            if (this.isProcessing) return;

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

        private void TerritoryChanged(object? sender, ushort newTerritoryType)
        {
            this.territoryIsChanged = true;
            this.nextTerritoryType = newTerritoryType;
            this.plugin.LodestoneService.LodestoneLastRequest =
                DateUtil.CurrentTime() - this.plugin.Configuration.LodestoneBatchDelay + 15000;
            this.needsUpdate = true;
        }

        private void SetLocationProperties(ushort newTerritoryType)
        {
            this.territoryType = newTerritoryType;
            this.contentId = PlayerTrackPlugin.DataManager.ContentId(this.territoryType);
            this.eventId = DateUtil.CurrentTime();
        }

        private void Update(Framework framework1)
        {
            // check if should process or needs to
            if (!this.needsUpdate) return;
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
                    this.actorTable = PlayerTrackPlugin.ObjectTable.ToArray();
                    this.localPlayerActorId = PlayerTrackPlugin.ClientState.LocalPlayer?.ObjectId ?? 0;
                }
            }
            catch (Exception)
            {
                Logger.LogDebug("Failed to get latest actor table.");
            }

            this.needsUpdate = false;
        }
    }
}
