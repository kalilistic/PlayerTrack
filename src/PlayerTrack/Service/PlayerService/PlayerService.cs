// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace PlayerTrack
{
    public partial class PlayerService
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IPlayerTrackPlugin _plugin;

        public PlayerService(IPlayerTrackPlugin plugin)
        {
            _plugin = plugin;
            CurrentPlayers = new ConcurrentDictionary<string, TrackPlayer>();
            RecentPlayers = new ConcurrentDictionary<string, TrackPlayer>();
            _jsonSerializerSettings = SerializerUtil.CamelCaseJsonSerializer();
            InitPlayers();
            LoadPlayers();
            _plugin.CategoryService.CategoriesUpdated += OnCategoriesUpdated;
        }

        public ConcurrentDictionary<string, TrackPlayer> RecentPlayers { get; set; }
        public ConcurrentDictionary<string, TrackPlayer> CurrentPlayers { get; set; }
        public ConcurrentDictionary<string, TrackPlayer> AllPlayers { get; set; }

        public TrackPlayer GetPlayer(string playerKey)
        {
            if (string.IsNullOrEmpty(playerKey)) return null;
            var retrievedPlayer = AllPlayers.TryGetValue(playerKey, out var existingPlayer);
            EnrichPlayerData(existingPlayer);
            return !retrievedPlayer ? null : existingPlayer;
        }

        public void UpdatePlayer(TrackPlayer player)
        {
            try
            {
                EnrichPlayerData(player);
                player.ClearBackingFields();
                AllPlayers[player.Key] = player;
            }
            catch
            {
                // ignored
            }
        }

        public bool DeletePlayer(string playerKey)
        {
            var player = GetPlayer(playerKey);
            return player != null && AllPlayers.TryRemove(playerKey, out _);
        }

        public bool ResetPlayer(string playerKey)
        {
            var player = GetPlayer(playerKey);
            if (player == null) return false;
            player.Icon = 0;
            player.Color = null;
            player.Notes = string.Empty;
            player.Alert.State = TrackAlertState.NotSet;
            player.CategoryId = _plugin.CategoryService.GetDefaultCategory().Id;
            player.ClearBackingFields();
            EnrichPlayerData(player);
            return true;
        }

        public bool AddPlayer(string name, string worldName)
        {
            var currentTime = DateUtil.CurrentTime();
            var newPlayer = new TrackPlayer
            {
                IsManual = true,
                Names = new List<string> {name},
                HomeWorlds = new List<TrackWorld>
                {
                    new TrackWorld
                    {
                        Id = _plugin.GetWorldId(worldName) ?? 0,
                        Name = worldName
                    }
                },
                FreeCompany = string.Empty,
                CategoryId = _plugin.CategoryService.GetDefaultCategory().Id,
                Encounters = new List<TrackEncounter>
                {
                    new TrackEncounter
                    {
                        Created = currentTime,
                        Updated = currentTime,
                        Location = new TrackLocation
                        {
                            TerritoryType = 1,
                            PlaceName = string.Empty,
                            ContentName = string.Empty
                        },
                        Job = new TrackJob
                        {
                            Id = 0,
                            Lvl = 0,
                            Code = "ADV"
                        }
                    }
                }
            };
            EnrichPlayerData(newPlayer);
            var playerAdded = AllPlayers.TryAdd(newPlayer.Key, newPlayer);
            SubmitLodestoneRequest(newPlayer, currentTime);
            return playerAdded;
        }

        private void OnCategoriesUpdated(object sender, bool e)
        {
            foreach (var player in AllPlayers) AddCategoryData(player.Value);
        }

        public TrackPlayer GetCurrentPlayer(string playerKey)
        {
            if (string.IsNullOrEmpty(playerKey)) return null;
            var retrievedPlayer = CurrentPlayers.TryGetValue(playerKey, out var existingPlayer);
            return !retrievedPlayer ? null : existingPlayer;
        }

        private void RemoveBadRecords()
        {
            var actorIds = AllPlayers.Select(pair => pair.Value.ActorId).ToList();
            var duplicateActorIds = actorIds.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .Distinct().ToList();
            if (duplicateActorIds.Any())
                foreach (var actorId in duplicateActorIds)
                {
                    if (actorId == 0) continue;
                    var players = AllPlayers
                        .Where(pair => pair.Value.ActorId == actorId).OrderBy(pair => pair.Value.Created).ToList();
                    if (players.Count < 2) continue;
                    var originalPlayer = players[0].Value;
                    var newPlayer = players[1].Value;
                    if (!originalPlayer.Name.Equals(newPlayer.Name)) continue;
                    if (newPlayer.Lodestone.Status == TrackLodestoneStatus.Verified)
                    {
                        newPlayer.NonDestructiveMerge(originalPlayer);
                        DeletePlayer(originalPlayer.Key);
                    }
                    else
                    {
                        originalPlayer.NonDestructiveMerge(newPlayer);
                        DeletePlayer(newPlayer.Key);
                    }
                }
        }

        private void MergeDuplicates()
        {
            var lodestoneIds = AllPlayers.Select(pair => pair.Value.Lodestone.Id).ToList();
            var duplicateLodestoneIds = lodestoneIds.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key)
                .Distinct().ToList();
            if (duplicateLodestoneIds.Any())
                foreach (var lodestoneId in duplicateLodestoneIds)
                {
                    var players = AllPlayers
                        .Where(pair =>
                            pair.Value.Lodestone.Status == TrackLodestoneStatus.Verified &&
                            pair.Value.Lodestone.Id == lodestoneId).OrderBy(pair => pair.Value.Created).ToList();
                    if (players.Count < 2) continue;
                    var originalPlayer = players[0].Value;
                    var newPlayer = players[1].Value;
                    newPlayer.Merge(originalPlayer);
                    DeletePlayer(originalPlayer.Key);
                }
        }
    }
}