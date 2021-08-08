using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.Colors;

namespace PlayerTrack
{
    /// <summary>
    /// Player service.
    /// </summary>
    public class PlayerService : BaseRepository
    {
        private readonly EncounterService encounterService;
        private readonly CategoryService categoryService;
        private readonly object locker = new ();
        private readonly SortedList<string, Player> players = new ();
        private readonly PlayerTrackPlugin plugin;
        private Player[] viewPlayers = new Player[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerService"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public PlayerService(PlayerTrackPlugin plugin)
            : base(plugin.PluginService)
        {
            this.plugin = plugin;
            this.encounterService = plugin.EncounterService;
            this.categoryService = plugin.CategoryService;
            this.LoadPlayers();
            this.UpdateViewPlayers();
        }

        /// <summary>
        /// Build composite player key.
        /// </summary>
        /// <param name="name">player name.</param>
        /// <param name="worldId">world id.</param>
        /// <returns>player key.</returns>
        public static string BuildPlayerKey(string name, uint worldId)
        {
            return string.Concat(name.Replace(' ', '_').ToUpper(), "_", worldId);
        }

        /// <summary>
        /// Get players.
        /// </summary>
        /// <returns>list of players.</returns>
        public KeyValuePair<string, Player>[]? GetPlayers()
        {
            try
            {
                lock (this.locker)
                {
                    return this.players.ToArray();
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get sorted players.
        /// </summary>
        /// <param name="nameFilter">name to filter by.</param>
        /// <returns>list of players.</returns>
        public Player[] GetSortedPlayers(string nameFilter = "")
        {
            try
            {
                Player[] playersList;
                lock (this.locker)
                {
                    // create player list by mode
                    playersList = this.viewPlayers.ToArray();
                }

                // filter by search
                if (!string.IsNullOrEmpty(nameFilter))
                {
                    playersList = this.plugin.Configuration.SearchType switch
                    {
                        PlayerSearchType.startsWith => playersList
                                                       .Where(
                                                           player => player.Names.First().ToLower().StartsWith(nameFilter.ToLower()))
                                                       .ToArray(),
                        PlayerSearchType.contains => playersList
                                                     .Where(player => player.Names.First().ToLower().Contains(nameFilter.ToLower()))
                                                     .ToArray(),
                        PlayerSearchType.exact => playersList
                                                  .Where(player => player.Names.First().ToLower().Equals(nameFilter.ToLower()))
                                                  .ToArray(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }

                return playersList;
            }
            catch (Exception)
            {
                Logger.LogDebug("Failed to retrieve players to display so trying again.");
                return this.GetSortedPlayers(nameFilter);
            }
        }

        /// <summary>
        /// Get player.
        /// </summary>
        /// <param name="key">player key to retrieve.</param>
        /// <returns>player or null.</returns>
        public Player? GetPlayer(string key)
        {
            try
            {
                lock (this.locker)
                {
                    this.players.TryGetValue(key, out Player player);
                    return player;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get player by actor id.
        /// </summary>
        /// <param name="actorId">player actor id to retrieve.</param>
        /// <returns>player or null.</returns>
        public Player? GetPlayer(uint actorId)
        {
            try
            {
                lock (this.locker)
                {
                    var player = this.players.FirstOrDefault(pair => pair.Value.ActorId == actorId);
                    return player.Value;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get player by lodestone id.
        /// </summary>
        /// <param name="lodestoneId">player lodestoneId to retrieve.</param>
        /// <returns>player or null.</returns>
        public Player? GetPlayerByLodestoneId(uint lodestoneId)
        {
            try
            {
                lock (this.locker)
                {
                    var player = this.players.FirstOrDefault(pair => pair.Value.LodestoneId == lodestoneId);
                    return player.Value;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get player by name and world name.
        /// </summary>
        /// <param name="playerName">player name.</param>
        /// <param name="worldName">player's home world name.</param>
        /// <returns>player or null.</returns>
        public Player? GetPlayer(string playerName, string worldName)
        {
            var worldId = this.plugin.PluginService.GameData.WorldId(worldName);
            return this.GetPlayer(playerName, (ushort)worldId);
        }

        /// <summary>
        /// Get player by name and world name.
        /// </summary>
        /// <param name="playerName">player name.</param>
        /// <param name="worldId">player's home world id.</param>
        /// <returns>player or null.</returns>
        public Player? GetPlayer(string playerName, ushort worldId)
        {
            try
            {
                var key = BuildPlayerKey(playerName, worldId);
                lock (this.locker)
                {
                    this.players.TryGetValue(key, out Player player);
                    return player;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Delete player.
        /// </summary>
        /// <param name="player">player to delete.</param>
        public void DeletePlayer(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players.Remove(player.Key);
                }

                this.encounterService.DeleteEncounters(player.Key);
                this.DeleteItem<Player>(player.Id);
                this.plugin.ActorManager.ClearPlayerCharacter(player.ActorId);
                this.UpdateViewPlayers();
            }
        }

        /// <summary>
        /// Remove player from current players.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void RemovePlayerFromCurrentPlayers(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].IsCurrent = player.IsCurrent;
                    this.players[player.Key].Updated = player.Updated;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Update player category.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerCategory(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].CategoryId = player.CategoryId;
                    this.players[player.Key].CategoryRank = this.plugin.CategoryService.GetCategory(player.CategoryId).Rank;
                }

                this.UpdateItem(this.players[player.Key]);
                this.UpdateViewPlayers();
            }
        }

        /// <summary>
        /// Update player title.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerTitle(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].Title = player.Title;
                    this.players[player.Key].SetSeTitle();
                }

                this.UpdateItem(this.players[player.Key]);
                this.plugin.NamePlateManager.ForceRedraw();
            }
        }

        /// <summary>
        /// Update player icon.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerIcon(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].Icon = player.Icon;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Update player nameplate color.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerNamePlateColor(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].NamePlateColor = player.NamePlateColor;
                }

                this.UpdateItem(this.players[player.Key]);
                this.plugin.NamePlateManager.ForceRedraw();
            }
        }

        /// <summary>
        /// Update player alert.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerAlert(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].IsAlertEnabled = player.IsAlertEnabled;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Reset player overrides.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void ResetPlayerOverrides(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].Reset();
                }

                this.UpdateItem(this.players[player.Key]);
                this.plugin.NamePlateManager.ForceRedraw();
            }
        }

        /// <summary>
        /// Update player list color.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerListColor(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].ListColor = player.ListColor;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Update player notes.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerNotes(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].Notes = player.Notes;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Update player lodestone state.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayerLodestoneState(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                lock (this.locker)
                {
                    this.players[player.Key].LodestoneStatus = player.LodestoneStatus;
                    this.players[player.Key].LodestoneFailureCount = player.LodestoneFailureCount;
                }

                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Add new or update existing player.
        /// </summary>
        /// <param name="player">player to add/update.</param>
        public void AddOrUpdatePlayer(Player player)
        {
            try
            {
                // update player
                if (this.players.ContainsKey(player.Key))
                {
                    var lastSeen = this.players[player.Key].Updated;
                    var lastLocation = this.players[player.Key].LastLocationName;
                    this.players[player.Key].UpdateFromNewCopy(player);
                    if (this.GetPlayerIsAlertEnabled(this.players[player.Key]) &&
                        DateUtil.CurrentTime() > this.players[player.Key].SendNextAlert)
                    {
                        this.players[player.Key].SendNextAlert =
                            DateUtil.CurrentTime() + this.plugin.Configuration.AlertFrequency;
                        var message = string.Format(
                            Loc.Localize("PlayerAlert", "last seen {0} ago in {1}."),
                            (DateUtil.CurrentTime() - lastSeen).ToDuration(),
                            lastLocation);
                        this.plugin.PluginService.Chat.Print(
                            this.players[player.Key].Names.First(),
                            this.players[player.Key].HomeWorlds.First().Key,
                            message,
                            XivChatType.Notice);
                    }

                    this.UpdateItem(this.players[player.Key]);
                }

                // add player
                else
                {
                    player.SendNextAlert = DateUtil.CurrentTime() + this.plugin.Configuration.AlertFrequency;
                    lock (this.locker)
                    {
                        this.players.Add(player.Key, player);
                    }

                    this.InsertItem(player);
                    this.RebuildIndex<Player>(p => p.Key);
                    this.SubmitLodestoneRequest(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update " + player.Key);
            }
        }

        /// <summary>
        /// Add players in bulk (used for migration).
        /// </summary>
        /// <param name="newPlayers">list of players to add.</param>
        public void AddPlayers(IEnumerable<Player> newPlayers)
        {
            var enumerable = newPlayers.ToList();
            foreach (var player in enumerable)
            {
                this.players.Add(player.Key, player);
            }

            this.InsertItems(enumerable);
            this.RebuildIndex<Player>(p => p.Key);
        }

        /// <summary>
        /// Add new player manually.
        /// </summary>
        /// <param name="playerName">player name to add.</param>
        /// <param name="worldName">player world name to add.</param>
        /// <returns>returns new player.</returns>
        public Player AddPlayer(string playerName, string worldName)
        {
            var worldId = this.plugin.PluginService.GameData.WorldId(worldName);
            var currentTime = DateUtil.CurrentTime();
            var player = new Player
            {
                Key = BuildPlayerKey(playerName, worldId),
                Names = new List<string> { playerName },
                HomeWorlds = new List<KeyValuePair<uint, string>> { new (worldId, worldName) },
                FreeCompany = "N/A",
                Created = currentTime,
                Updated = currentTime,
                LastLocationName = "N/A",
                CategoryId = this.plugin.CategoryService.GetDefaultCategory().Id,
                SeenCount = 0,
            };
            lock (this.locker)
            {
                if (this.players.ContainsKey(player.Key)) return player;
                this.players.Add(player.Key, player);
            }

            this.InsertItem(player);
            this.RebuildIndex<Player>(p => p.Key);
            this.SubmitLodestoneRequest(player);

            return player;
        }

        /// <summary>
        /// Add new player manually.
        /// </summary>
        /// <param name="playerName">player name to add.</param>
        /// <param name="worldId">player world id to add.</param>
        /// <returns>returns new player.</returns>
        public Player AddPlayer(string playerName, ushort worldId)
        {
            var worldName = this.plugin.PluginService.GameData.WorldName(worldId);
            var currentTime = DateUtil.CurrentTime();
            var player = new Player
            {
                Key = BuildPlayerKey(playerName, worldId),
                Names = new List<string> { playerName },
                HomeWorlds = new List<KeyValuePair<uint, string>> { new (worldId, worldName) },
                FreeCompany = "N/A",
                Created = currentTime,
                Updated = currentTime,
                LastLocationName = "Never Seen",
                CategoryId = this.plugin.CategoryService.GetDefaultCategory().Id,
                SeenCount = 0,
            };
            lock (this.locker)
            {
                if (this.players.ContainsKey(player.Key)) return player;
                this.players.Add(player.Key, player);
            }

            this.InsertItem(player);
            this.RebuildIndex<Player>(p => p.Key);
            this.SubmitLodestoneRequest(player);
            this.UpdateViewPlayers();

            return player;
        }

        /// <summary>
        /// Update lodestone details.
        /// </summary>
        /// <param name="response">lodestone response.</param>
        public void UpdateLodestone(LodestoneResponse response)
        {
            lock (this.locker)
            {
                if (this.players.ContainsKey(response.PlayerKey))
                {
                    var currentPlayer = this.players[response.PlayerKey];

                    // if verified
                    if (response.Status == LodestoneStatus.Verified)
                    {
                        // get player and merge for name/world changes
                        var player = this.GetPlayerByLodestoneId(response.LodestoneId);
                        if (player != null)
                        {
                            if (player.Created < currentPlayer.Created)
                            {
                                player.Merge(currentPlayer);
                                this.UpdateItem(player);
                                if (currentPlayer.IsCurrent)
                                {
                                    this.players.Remove(currentPlayer.Key);
                                }

                                this.DeletePlayer(currentPlayer);
                                return;
                            }

                            currentPlayer.Merge(player);
                            if (player.IsCurrent)
                            {
                                this.players.Remove(player.Key);
                            }

                            this.DeletePlayer(player);
                        }
                    }

                    currentPlayer.LodestoneId = response.LodestoneId;
                    currentPlayer.LodestoneStatus = response.Status;
                    currentPlayer.LodestoneLastUpdated = DateUtil.CurrentTime();
                    if (currentPlayer.LodestoneStatus == LodestoneStatus.Failed)
                    {
                        currentPlayer.LodestoneFailureCount++;
                    }

                    this.UpdateItem(currentPlayer);
                }
            }
        }

        /// <summary>
        /// Remove all current players.
        /// </summary>
        public void RemoveCurrentPlayers()
        {
            lock (this.locker)
            {
                IEnumerable<KeyValuePair<string, Player>> currentPlayers = this.players.Where(kvp => kvp.Value.IsCurrent);
                foreach (var player in currentPlayers)
                {
                    this.players[player.Key].IsCurrent = false;
                    this.UpdateItem(this.players[player.Key]);
                }
            }

            this.encounterService.ClearCurrentEncounters();
        }

        /// <summary>
        /// Remove deleted category.
        /// </summary>
        /// <param name="deletedCategoryId">category id to remove from players.</param>
        /// <param name="defaultCategoryId">category id to replace with deleted one.</param>
        public void RemoveDeletedCategory(int deletedCategoryId, int defaultCategoryId)
        {
            lock (this.locker)
            {
                IEnumerable<KeyValuePair<string, Player>> playersWithCategory = this.players.Where(kvp => kvp.Value.CategoryId == deletedCategoryId);
                List<Player> playersWithCategoryList = new ();
                foreach (var player in playersWithCategory)
                {
                    this.players[player.Key].CategoryId = defaultCategoryId;
                    playersWithCategoryList.Add(player.Value);
                }

                this.UpsertItems(playersWithCategoryList);
                this.UpdateViewPlayers();
            }
        }

        /// <summary>
        /// Get effective player list color based on category and overrides.
        /// </summary>
        /// <param name="player">player to get color for.</param>
        /// <returns>player list color.</returns>
        public Vector4 GetPlayerListColor(Player player)
        {
            if (player.ListColor != null) return (Vector4)player.ListColor;
            var category = this.categoryService.GetCategory(player.CategoryId);
            if (category is { ListColor: { } }) return (Vector4)category.ListColor;
            return ImGuiColors.White;
        }

        /// <summary>
        /// Get effective player nameplate color based on category and overrides.
        /// </summary>
        /// <param name="player">player to get color for.</param>
        /// <returns>player nameplate color.</returns>
        public Vector4? GetPlayerNamePlateColor(Player player)
        {
            if (player.NamePlateColor != null) return (Vector4)player.NamePlateColor;
            var category = this.categoryService.GetCategory(player.CategoryId);
            return category.NamePlateColor;
        }

        /// <summary>
        /// Get effective player icon based on category and overrides.
        /// </summary>
        /// <param name="player">player to get icon for.</param>
        /// <returns>player icon.</returns>
        public string GetPlayerIcon(Player player)
        {
            if (player.Icon != 0) return ((FontAwesomeIcon)player.Icon).ToIconString();
            var category = this.categoryService.GetCategory(player.CategoryId);
            if (category.Icon != 0) return ((FontAwesomeIcon)category.Icon).ToIconString();
            return FontAwesomeIcon.UserAlt.ToIconString();
        }

        /// <summary>
        /// Get effective player alert state based on category and overrides.
        /// </summary>
        /// <param name="player">player to get alert for.</param>
        /// <returns>player alert state.</returns>
        public bool GetPlayerIsAlertEnabled(Player player)
        {
            if (player.IsAlertEnabled) return true;
            var category = this.categoryService.GetCategory(player.CategoryId);
            if (category is { IsAlertEnabled: true }) return true;
            return false;
        }

        /// <summary>
        /// Reprocess lodestone requests after initial load.
        /// </summary>
        public void ReprocessPlayersForLodestone()
        {
            lock (this.locker)
            {
                var existingPlayers = this.GetItems<Player>().ToList();
                var lodestoneRequests = this.plugin.LodestoneService.GetRequests().Select(request => request.PlayerKey).ToArray();
                foreach (var player in existingPlayers)
                {
                    if (!lodestoneRequests.Contains(player.Key))
                    {
                        this.SubmitLodestoneRequest(player);
                    }
                }
            }
        }

        /// <summary>
        /// Submit lodestone request for player.
        /// </summary>
        /// <param name="player">player to submit for ldoestone lookup.</param>
        public void SubmitLodestoneRequest(Player player)
        {
            // filter requests not to be sent
            if (!this.plugin.Configuration.SyncToLodestone) return;
            if (player.LodestoneStatus == LodestoneStatus.Verified || (player.LodestoneStatus == LodestoneStatus.Failed &&
                player.LodestoneFailureCount >= this.plugin.Configuration.LodestoneMaxFailure &&
                DateUtil.CurrentTime() > player.LodestoneLastUpdated + this.plugin.Configuration.LodestoneFailureDelay))
            {
                return;
            }

            // add request to queue
            this.plugin.LodestoneService.AddRequest(new LodestoneRequest
            {
                PlayerKey = player.Key,
                PlayerName = player.Names.First(),
                WorldName = player.HomeWorlds.First().Value,
            });
        }

        /// <summary>
        /// Set derived fields to reduce storage needs.
        /// </summary>
        /// <param name="player">player to calculate derived fields for.</param>
        public void SetDerivedFields(Player player)
        {
            // customize data
            if (player.Customize != null)
            {
                player.CharaCustomizeData = CharaCustomizeData.MapCustomizeData(player.Customize);
            }

            // last location / content id
            player.LastContentId = this.plugin.PluginService.GameData.ContentId(player.LastTerritoryType);
            if (player.LastContentId == 0)
            {
                var placeName = this.plugin.PluginService.GameData.PlaceName(player.LastTerritoryType);
                player.LastLocationName = string.IsNullOrEmpty(placeName) ? "Eorzea" : placeName;
            }
            else
            {
                player.LastLocationName = this.plugin.PluginService.GameData.ContentName(player.LastContentId);
            }

            // seTitle for nameplates
            player.SetSeTitle();

            // category rank
            player.CategoryRank = this.plugin.CategoryService.GetCategory(player.CategoryId).Rank;
        }

        /// <summary>
        /// Recalculate player category ranks after rank changing.
        /// </summary>
        public void UpdatePlayerCategoryRank()
        {
            lock (this.locker)
            {
                foreach (var player in this.players)
                {
                    player.Value.CategoryRank = this.plugin.CategoryService.GetCategory(player.Value.CategoryId).Rank;
                }
            }

            this.UpdateViewPlayers();
        }

        /// <summary>
        /// Update sorted player list for display.
        /// </summary>
        public void UpdateViewPlayers()
        {
            lock (this.locker)
            {
                if (this.plugin.Configuration.CategoryFilterId != 0)
                {
                    this.viewPlayers = this.plugin.Configuration.ListMode switch
                    {
                        PlayerListMode.current => this.players.Where(pair => pair.Value.IsCurrent && pair.Value.CategoryId == this.plugin.Configuration.CategoryFilterId)
                                                      .Select(pair => pair.Value).OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        PlayerListMode.recent => this.players.Where(pair => pair.Value.IsRecent && pair.Value.CategoryId == this.plugin.Configuration.CategoryFilterId).Select(pair => pair.Value)
                                                     .OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        PlayerListMode.all => this.players.Where(pair => pair.Value.CategoryId == this.plugin.Configuration.CategoryFilterId).Select(pair => pair.Value).OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
                else
                {
                    this.viewPlayers = this.plugin.Configuration.ListMode switch
                    {
                        PlayerListMode.current => this.players.Where(pair => pair.Value.IsCurrent)
                                                      .Select(pair => pair.Value).OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        PlayerListMode.recent => this.players.Where(pair => pair.Value.IsRecent).Select(pair => pair.Value)
                                                     .OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        PlayerListMode.all => this.players.Select(pair => pair.Value).OrderBy(player => player.CategoryRank).ThenBy(player => player.Names.First()).ToArray(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }
            }
        }

        private void LoadPlayers()
        {
            var existingPlayers = this.GetItems<Player>().ToList();
            foreach (var player in existingPlayers)
            {
                if (!this.players.ContainsKey(player.Key))
                {
                    lock (this.locker)
                    {
                        this.SetDerivedFields(player);
                        this.players.Add(player.Key, player);
                    }

                    this.SubmitLodestoneRequest(player);
                }
                else
                {
                    Logger.LogError("Duplicate player detected so merging: " + player.Key);
                    var dupePlayers = existingPlayers.Where(p => p.Key.Equals(player.Key)).ToList();
                    if (dupePlayers.Count < 2) return;
                    var player1 = dupePlayers[0];
                    var player2 = dupePlayers[1];
                    if (player1.Created < player2.Created)
                    {
                        player1.Merge(player2);
                        this.UpdateItem(player1);
                        if (player2.IsCurrent)
                        {
                            this.players.Remove(player2.Key);
                        }

                        this.DeletePlayer(player2);
                        return;
                    }

                    player2.Merge(player1);
                    if (player1.IsCurrent)
                    {
                        this.players.Remove(player1.Key);
                    }

                    this.DeletePlayer(player1);
                }
            }
        }
    }
}
