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
        private SortedList<string, Player> viewPlayers = new ();

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
            this.ResetViewPlayers();
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
        ///  Build composite player sort key.
        /// </summary>
        /// <param name="player">player for sort key update.</param>
        /// <returns>sort key.</returns>
        public static string BuildPlayerSortKey(Player player)
        {
            return string.Concat(player.CategoryRank.ToString().PadLeft(4, '0'), "_", player.Key);
        }

        /// <summary>
        /// Update view player.
        /// </summary>
        /// <param name="sortKey">original sort key.</param>
        /// <param name="player">new player.</param>
        public void UpdateViewPlayer(string sortKey, Player player)
        {
            if (!this.plugin.Configuration.ShowWindow) return;
            lock (this.locker)
            {
                if (this.viewPlayers.ContainsKey(sortKey))
                {
                    this.viewPlayers.Remove(sortKey);
                    this.viewPlayers.Add(player.SortKey, player);
                }
            }
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
                Player[] playersList = { };
                if (!this.plugin.Configuration.ShowWindow) return playersList;
                lock (this.locker)
                {
                    // create player list by mode
                    playersList = this.viewPlayers.Select(pair => pair.Value).ToArray();
                }

                // filter by search
                if (!string.IsNullOrEmpty(nameFilter))
                {
                    playersList = this.plugin.Configuration.SearchType switch
                    {
                        PlayerSearchType.startsWith => playersList
                                                       .Where(
                                                           player => player.Names.First().ToLower().StartsWith(nameFilter.ToLower())).ToArray(),
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
                    if (this.plugin.Configuration.ShowWindow && this.viewPlayers.ContainsKey(player.SortKey))
                    {
                        this.viewPlayers.Remove(player.SortKey);
                    }
                }

                this.encounterService.DeleteEncounters(player.Key);
                this.DeleteItem<Player>(player.Id);
                this.plugin.ActorManager.ClearPlayerCharacter(player.ActorId);
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
                    if (this.plugin.Configuration.ShowWindow && this.viewPlayers.ContainsKey(player.SortKey))
                    {
                        if (PlayerFilterType.GetPlayerFilterTypeByIndex(this.plugin.Configuration.PlayerFilterType) ==
                            PlayerFilterType.CurrentPlayers)
                        {
                            this.viewPlayers.Remove(player.SortKey);
                        }
                        else
                        {
                            this.UpdateViewPlayer(player.SortKey, player);
                        }
                    }
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
                    var originalSortKey = player.SortKey;
                    this.players[player.Key].CategoryId = player.CategoryId;
                    this.players[player.Key].CategoryRank = this.plugin.CategoryService.GetCategory(player.CategoryId).Rank;
                    this.players[player.Key].SortKey = BuildPlayerSortKey(this.players[player.Key]);
                    this.UpdateViewPlayer(originalSortKey, this.players[player.Key]);
                }

                this.UpdateItem(this.players[player.Key]);
                this.plugin.NamePlateManager.ForceRedraw();
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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
                    this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
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

                    if (this.plugin.Configuration.ShowWindow)
                    {
                        if (this.viewPlayers.ContainsKey(this.players[player.Key].SortKey))
                        {
                            this.UpdateViewPlayer(this.players[player.Key].SortKey, this.players[player.Key]);
                        }
                        else
                        {
                            this.viewPlayers.Add(player.SortKey, this.players[player.Key]);
                        }
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
                        if (this.plugin.Configuration.ShowWindow)
                        {
                            this.viewPlayers.Add(player.SortKey, player);
                        }
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
                this.viewPlayers.Add(player.SortKey, player);
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
            this.SetDerivedFields(player);
            lock (this.locker)
            {
                if (this.players.ContainsKey(player.Key)) return player;
                this.players.Add(player.Key, player);
                this.viewPlayers.Add(player.SortKey, player);
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
            this.SetDerivedFields(player);
            lock (this.locker)
            {
                if (this.players.ContainsKey(player.Key)) return player;
                this.players.Add(player.Key, player);
                this.viewPlayers.Add(player.SortKey, player);
            }

            this.InsertItem(player);
            this.RebuildIndex<Player>(p => p.Key);
            this.SubmitLodestoneRequest(player);
            this.ResetViewPlayers();

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
                    // get current player
                    var currentPlayer = this.players[response.PlayerKey];

                    // merge if duplicate
                    if (response.Status == LodestoneStatus.Verified)
                    {
                        var originalPlayer = this.GetPlayerByLodestoneId(response.LodestoneId);
                        if (originalPlayer != null)
                        {
                            // debug logs
                            Logger.LogInfo("Found Duplicate Lodestone Id: " + response.LodestoneId);
                            Logger.LogDebug("Original Player: " + originalPlayer);
                            Logger.LogDebug("Newer Player: " + currentPlayer);

                            // capture keys
                            var originalKey = originalPlayer.Key;
                            var originalSortKey = originalPlayer.SortKey;

                            // merge current record into original record
                            originalPlayer.Merge(currentPlayer);
                            originalPlayer.Key = currentPlayer.Key;
                            originalPlayer.LodestoneId = response.LodestoneId;
                            originalPlayer.LodestoneStatus = response.Status;
                            originalPlayer.LodestoneLastUpdated = DateUtil.CurrentTime();
                            this.SetDerivedFields(originalPlayer);

                            // delete current player
                            this.DeletePlayer(currentPlayer);

                            // remove and re-add original player due to key change
                            this.players.Remove(originalKey);
                            if (this.plugin.Configuration.ShowWindow && this.viewPlayers.ContainsKey(originalSortKey))
                            {
                                this.viewPlayers.Remove(originalSortKey);
                            }

                            this.players.Add(originalPlayer.Key, originalPlayer);
                            this.UpdateItem(originalPlayer);

                            // regenerate view players
                            this.ResetViewPlayers();

                            return;
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
                    this.UpdateViewPlayer(currentPlayer.SortKey, currentPlayer);
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

                if (PlayerFilterType.GetPlayerFilterTypeByIndex(this.plugin.Configuration.PlayerFilterType) ==
                    PlayerFilterType.CurrentPlayers)
                {
                    this.ResetViewPlayers();
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
                this.ResetViewPlayers();
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
            if (player.LodestoneStatus == LodestoneStatus.Verified) return;
            if (player.LodestoneStatus == LodestoneStatus.Failed)
            {
                if (player.LodestoneFailureCount >= this.plugin.Configuration.LodestoneMaxFailure) return;
                if (DateUtil.CurrentTime() >
                    player.LodestoneLastUpdated + this.plugin.Configuration.LodestoneFailureDelay) return;
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

            // set sort key
            player.SortKey = BuildPlayerSortKey(player);
        }

        /// <summary>
        /// Recalculate player category ranks after rank changing.
        /// </summary>
        public void SetDerivedFieldsForAllPlayers()
        {
            lock (this.locker)
            {
                foreach (var player in this.players)
                {
                    this.SetDerivedFields(player.Value);
                }

                this.ResetViewPlayers();
            }
        }

        /// <summary>
        /// Update sorted player list for display.
        /// </summary>
        public void ResetViewPlayers()
        {
            lock (this.locker)
            {
                this.viewPlayers = new SortedList<string, Player>();
                var filterType = PlayerFilterType.GetPlayerFilterTypeByIndex(this.plugin.Configuration.PlayerFilterType);

                if (filterType == PlayerFilterType.CurrentPlayers)
                {
                    foreach (var kvp in this.players)
                    {
                        if (kvp.Value.IsCurrent && !this.viewPlayers.ContainsKey(kvp.Value.SortKey))
                        {
                            this.viewPlayers.Add(kvp.Value.SortKey, kvp.Value);
                        }
                    }
                }
                else if (filterType == PlayerFilterType.RecentPlayers)
                {
                    foreach (var kvp in this.players)
                    {
                        if (kvp.Value.IsRecent && !this.viewPlayers.ContainsKey(kvp.Value.SortKey))
                        {
                            this.viewPlayers.Add(kvp.Value.SortKey, kvp.Value);
                        }
                    }
                }
                else if (filterType == PlayerFilterType.AllPlayers)
                {
                    foreach (var kvp in this.players)
                    {
                        if (!this.viewPlayers.ContainsKey(kvp.Value.SortKey))
                        {
                            this.viewPlayers.Add(kvp.Value.SortKey, kvp.Value);
                        }
                    }
                }
                else if (filterType == PlayerFilterType.PlayersByCategory)
                {
                    foreach (var kvp in this.players)
                    {
                        if (kvp.Value.CategoryId == this.plugin.Configuration.CategoryFilterId && !this.viewPlayers.ContainsKey(kvp.Value.SortKey))
                        {
                            this.viewPlayers.Add(kvp.Value.SortKey, kvp.Value);
                        }
                    }
                }
            }
        }

        private void LoadPlayer(Player player)
        {
            lock (this.locker)
            {
                this.SetDerivedFields(player);
                this.players.Add(player.Key, player);
            }

            this.SubmitLodestoneRequest(player);
        }

        private void LoadPlayers()
        {
            this.MergeKeyDuplicates();
            this.MergeLodestoneDuplicates();
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
                }

                this.SubmitLodestoneRequest(player);
            }
        }

        private void MergeLodestoneDuplicates()
        {
            var playersInDB = this.GetItems<Player>().ToList();
            var playerLodestoneIds = playersInDB.Where(player => player.LodestoneId != 0).Select(player => player.LodestoneId).ToList();
            var dupeLodestoneIds = playerLodestoneIds.GroupBy(x => x)
                                     .Where(group => group.Count() > 1)
                                     .Select(group => group.Key).ToList();
            if (dupeLodestoneIds.Count > 0)
            {
                Logger.LogInfo("Found Duplicate Lodestone Ids: " + dupeLodestoneIds.Count);
                foreach (var dupeLodestoneId in dupeLodestoneIds)
                {
                    var dupePlayers = playersInDB.Where(p => p.LodestoneId == dupeLodestoneId).ToList();
                    var sortedPlayers = dupePlayers.OrderBy(player => player.Created).ToArray();
                    var originalPlayer = sortedPlayers.First();
                    var deletePlayers = sortedPlayers.Skip(1).ToArray();
                    var newKey = sortedPlayers.Last().Key;
                    Logger.LogDebug("Original Player: " + originalPlayer);
                    foreach (var deletedPlayer in deletePlayers)
                    {
                        Logger.LogDebug("Newer Player: " + deletedPlayer);
                        originalPlayer.Merge(deletedPlayer);
                        this.DeleteItem<Player>(deletedPlayer.Id);
                    }

                    originalPlayer.Key = newKey;
                    this.UpdateItem(originalPlayer);
                }
            }
        }

        private void MergeKeyDuplicates()
        {
            var playersInDB = this.GetItems<Player>().ToList();
            var playerKeys = playersInDB.Select(pair => pair.Key.ToString()).ToList();
            var dupeKeys = playerKeys.GroupBy(x => x)
                                     .Where(group => group.Count() > 1)
                                     .Select(group => group.Key).ToList();
            if (dupeKeys.Count > 0)
            {
                Logger.LogError("Found Duplicate Keys: " + dupeKeys.Count);
                foreach (var dupeKey in dupeKeys)
                {
                    var dupePlayers = playersInDB.Where(p => p.Key.Equals(dupeKey)).ToList();
                    var sortedPlayers = dupePlayers.OrderBy(player => player.Created).ToArray();
                    var originalPlayer = sortedPlayers.First();
                    var deletePlayers = sortedPlayers.Skip(1).ToArray();

                    foreach (var deletedPlayer in deletePlayers)
                    {
                        originalPlayer.Merge(deletedPlayer);
                        this.DeleteItem<Player>(deletedPlayer.Id);
                    }

                    this.UpdateItem(originalPlayer);
                }
            }
        }
    }
}
