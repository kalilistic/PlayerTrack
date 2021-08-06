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
        /// <param name="nameFilter">name to filter by.</param>
        /// <returns>list of players.</returns>
        public KeyValuePair<string, Player>[] GetPlayers(string nameFilter = "")
        {
            try
            {
                lock (this.locker)
                {
                    // create player list by mode
                    KeyValuePair<string, Player>[] playersList = this.plugin.Configuration.ListMode switch
                    {
                        PlayerListMode.current => this.players.Where(pair => pair.Value.IsCurrent).ToArray(),
                        PlayerListMode.recent => this.players.Where(pair => pair.Value.IsRecent).ToArray(),
                        PlayerListMode.all => this.players.ToArray(),
                        _ => throw new ArgumentOutOfRangeException(),
                    };

                    // filter by search
                    if (!string.IsNullOrEmpty(nameFilter))
                    {
                        playersList = this.plugin.Configuration.SearchType switch
                        {
                            PlayerSearchType.startsWith => playersList
                                                          .Where(
                                                              pair => pair.Value.Names.First().ToLower().StartsWith(nameFilter.ToLower()))
                                                          .ToArray(),
                            PlayerSearchType.contains => playersList
                                                        .Where(pair => pair.Value.Names.First().ToLower().Contains(nameFilter.ToLower()))
                                                        .ToArray(),
                            PlayerSearchType.exact => playersList
                                                     .Where(pair => pair.Value.Names.First().ToLower().Equals(nameFilter.ToLower()))
                                                     .ToArray(),
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                    }

                    // filter by category
                    if (this.plugin.Configuration.CategoryFilterId == 0) return playersList;
                    return playersList
                           .Where(pair => pair.Value.CategoryId == this.plugin.Configuration.CategoryFilterId)
                           .ToArray();
                }
            }
            catch (Exception)
            {
                Logger.LogDebug("Failed to retrieve players to display so trying again.");
                return this.GetPlayers(nameFilter);
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
            }
        }

        /// <summary>
        /// Update player.
        /// </summary>
        /// <param name="player">player to update.</param>
        public void UpdatePlayer(Player player)
        {
            if (this.players.ContainsKey(player.Key))
            {
                this.players[player.Key] = player;
                this.UpdateItem(this.players[player.Key]);
            }
        }

        /// <summary>
        /// Add new or update existing player.
        /// </summary>
        /// <param name="player">player to add/update.</param>
        public void AddOrUpdatePlayer(Player player)
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
                    if (response.Status == LodestoneStatus.Verified)
                    {
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
                List<Player> playersWithCategoryList = new List<Player>();
                foreach (var player in playersWithCategory)
                {
                    this.players[player.Key].CategoryId = defaultCategoryId;
                    playersWithCategoryList.Add(player.Value);
                }

                this.UpsertItems(playersWithCategoryList);
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
