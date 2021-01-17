// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CheapLoc;
using Newtonsoft.Json;

namespace PlayerTrack
{
	public class RosterService : IRosterService
	{
		private readonly Queue<TrackPlayer> _addPlayerRequests = new Queue<TrackPlayer>();
		private readonly Queue<string> _deletePlayerRequests = new Queue<string>();
		private readonly JsonSerializerSettings _jsonSerializerSettings;
		private readonly IPlayerTrackPlugin _playerTrackPlugin;

		public RosterService(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
			Current = new TrackRoster(new Dictionary<string, TrackPlayer>(), _playerTrackPlugin);
			_jsonSerializerSettings = SerializerUtil.CamelCaseJsonSerializer();
			InitRoster();
			LoadRoster();
		}

		public Dictionary<string, TrackPlayer> Recent { get; set; }

		public TrackRoster Current { get; set; }
		public TrackRoster All { get; set; }
		public TrackPlayer SelectedPlayer { get; set; }

		public void ClearPlayers()
		{
			Current.Roster = new Dictionary<string, TrackPlayer>();
		}

		public void ProcessPlayers(List<TrackPlayer> incomingPlayers)
		{
			var currentPlayers = new TrackRoster(new Dictionary<string, TrackPlayer>(), _playerTrackPlugin);
			foreach (var player in incomingPlayers)
			{
				var encounter = player.Encounters.Last();
				if (All.IsNewPlayer(player.Key))
				{
					All.AddPlayer(player);
					SubmitLodestoneRequest(player);
				}
				else
				{
					if (All.IsNewEncounter(player.Key, encounter))
						All.AddEncounter(player.Key, encounter);
					else
						All.UpdateEncounter(player.Key, encounter);
					All.UpdatePlayer(player);
				}

				var currentPlayer = All.GetPlayer(player.Key);
				currentPlayer.ClearBackingFields();
				try
				{
					currentPlayers.AddPlayer(currentPlayer);
				}
				catch (Exception ex)
				{
					_playerTrackPlugin.LogError(ex, "Failed to add to currentPlayer.. " + currentPlayer.Key);
				}
			}

			Current = currentPlayers;
			_playerTrackPlugin.GetCategoryService().SetPlayerPriority();
			SortRosters();
			SendAlerts();
		}

		public bool IsNewPlayer(string name, string worldName)
		{
			var worldId = _playerTrackPlugin.GetWorldId(worldName) ?? 0;
			var key = TrackPlayer.CreateKey(name, worldId);
			return All.IsNewPlayer(key);
		}

		public void DeletePlayer(string key)
		{
			_deletePlayerRequests.Enqueue(key);
		}

		public void ChangeSelectedPlayer(string key)
		{
			SelectedPlayer = All.GetPlayer(key);
		}

		public void BackupRoster(bool forceBackup = false)
		{
			if (forceBackup || (DateTime.UtcNow - _playerTrackPlugin.Configuration.LastBackup.ToDateTime())
				.TotalMilliseconds >
				_playerTrackPlugin.Configuration.BackupFrequency)
			{
				_playerTrackPlugin.GetDataManager().CreateBackup();
				_playerTrackPlugin.GetDataManager().DeleteBackups(_playerTrackPlugin.Configuration.BackupRetention);
				_playerTrackPlugin.Configuration.LastBackup = DateUtil.CurrentTime();
				_playerTrackPlugin.SaveConfig();
			}
		}

		public void SaveData()
		{
			try
			{
				var data = JsonConvert.SerializeObject(All.Roster, _jsonSerializerSettings);
				var metaData = JsonConvert.SerializeObject(new TrackMetaData
				{
					SchemaVersion = _playerTrackPlugin.Configuration.SchemaVersion,
					Compressed = _playerTrackPlugin.Configuration.Compressed
				}, _jsonSerializerSettings);
				if (_playerTrackPlugin.Configuration.Compressed) data = data.Compress();
				_playerTrackPlugin.GetDataManager().SaveData("players.dat", data);
				_playerTrackPlugin.GetDataManager().SaveData("data.meta", metaData);
				BackupRoster();
			}
			catch (Exception ex)
			{
				_playerTrackPlugin.LogError(ex, "Failed to save player data - will try again soon.");
			}
		}

		public Dictionary<string, TrackPlayer> GetPlayersByName(string name)
		{
			return All.FilterByName(name);
		}

		public void SortRosters()
		{
			Current.SortByNameAndPriority();
			All.SortByNameAndPriority();
			Recent = All.FilterByLastUpdate(_playerTrackPlugin.Configuration.RecentPlayerThreshold);
		}

		public TrackCategory GetCategory(string playerKey)
		{
			try
			{
				var player = All.GetPlayer(playerKey);
				return player.CategoryId == 0
					? _playerTrackPlugin.GetCategoryService().GetDefaultCategory()
					: _playerTrackPlugin.GetCategoryService().GetCategory(player.CategoryId);
			}
			catch
			{
				return null;
			}
		}

		private bool IsTimeForAlert(TrackPlayer player)
		{
			return player.Alert.LastSent != 0 &&
			       (DateTime.UtcNow - player.Alert.LastSent.ToDateTime()).TotalMilliseconds >
			       _playerTrackPlugin.Configuration.AlertFrequency;
		}

		public void SendAlerts()
		{
			if (!_playerTrackPlugin.Configuration.EnableAlerts) return;
			foreach (var player in Current.Roster)
			{
				var category = _playerTrackPlugin.GetCategoryService().GetCategory(player.Value.CategoryId);
				if (player.Value.Alert.State == TrackAlertState.Enabled || category.EnableAlerts)
				{
					if (IsTimeForAlert(player.Value))
					{
						if (_playerTrackPlugin.Configuration.IncludeNotesInAlert &&
						    !string.IsNullOrEmpty(player.Value.Notes))
							_playerTrackPlugin.PrintMessage(string.Format(
								Loc.Localize("PlayerAlertWithNotes", "{0} last seen {1}: {2}"), player.Value.Name,
								player.Value.PreviouslyLastSeen, player.Value.AbbreviatedNotes));
						else
							_playerTrackPlugin.PrintMessage(string.Format(
								Loc.Localize("PlayerAlert", "{0} last seen {1}."), player.Value.Name,
								player.Value.PreviouslyLastSeen));
						player.Value.Alert.LastSent = DateUtil.CurrentTime();
						Thread.Sleep(_playerTrackPlugin.Configuration.AlertDelay);
					}
					else if (player.Value.Alert.LastSent == 0)
					{
						player.Value.Alert.LastSent = DateUtil.CurrentTime();
					}
				}
			}
		}

		public void AddPlayer(string name, string worldName)
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
						Id = _playerTrackPlugin.GetWorldId(worldName) ?? 0,
						Name = worldName
					}
				},
				FreeCompany = string.Empty,
				CategoryId = _playerTrackPlugin.GetCategoryService().GetDefaultCategory().Id,
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
			_addPlayerRequests.Enqueue(newPlayer);
		}

		public void ProcessRequests()
		{
			ProcessDeleteRequests();
			ProcessAddRequests();
			ProcessLodestoneRequests();
			MergeDuplicates();
		}

		private static void HandleFailure(TrackPlayer player)
		{
			if (player.Lodestone.Status == TrackLodestoneStatus.Failed)
			{
				player.Lodestone.LastFailed = player.Lodestone.LastUpdated;
				player.Lodestone.FailureCount++;
			}
			else
			{
				player.Lodestone.LastFailed = 0;
				player.Lodestone.FailureCount = 0;
				player.Lodestone.LastFailed = 0;
			}
		}

		private void SubmitLodestoneRequest(TrackPlayer player)
		{
			if (!_playerTrackPlugin.Configuration.SyncToLodestone) return;
			if (player.Lodestone.Status == TrackLodestoneStatus.Unverified)
			{
				_playerTrackPlugin.GetLodestoneService().AddRequest(new TrackLodestoneRequest
				{
					PlayerKey = player.Key,
					PlayerName = player.Name,
					WorldName = player.HomeWorld
				});
				player.Lodestone.Status = TrackLodestoneStatus.Verifying;
			}
		}

		private void ProcessLodestoneRequests()
		{
			var responses = _playerTrackPlugin.GetLodestoneService().GetResponses();
			foreach (var response in responses)
			{
				var player = All.GetPlayer(response.PlayerKey);
				player.Lodestone.Id = response.LodestoneId;
				player.Lodestone.Status = response.Status;
				player.Lodestone.LastUpdated = DateUtil.CurrentTime();
				HandleFailure(player);
			}
		}

		private void MergeDuplicates()
		{
			var lodestoneIds = All.Roster.Select(pair => pair.Value.Lodestone.Id).ToList();
			var duplicateLodestoneIds = lodestoneIds.GroupBy(x => x)
				.Where(g => g.Count() > 1)
				.Select(y => y.Key)
				.Distinct().ToList();
			if (duplicateLodestoneIds.Any())
				foreach (var lodestoneId in lodestoneIds)
				{
					var players = All.Roster
						.Where(pair =>
							pair.Value.Lodestone.Status == TrackLodestoneStatus.Verified &&
							pair.Value.Lodestone.Id == lodestoneId).OrderBy(pair => pair.Value.Created).ToList();
					if (players.Count < 2) continue;
					var originalPlayer = players[0].Value;
					var newPlayer = players[1].Value;
					newPlayer.Merge(originalPlayer);
					All.DeletePlayer(originalPlayer.Key);
				}
		}

		private void ProcessDeleteRequests()
		{
			while (_deletePlayerRequests.Count > 0)
			{
				var playerKey = _deletePlayerRequests.Dequeue();
				All.DeletePlayer(playerKey);
			}
		}

		private void ProcessAddRequests()
		{
			while (_addPlayerRequests.Count > 0)
			{
				var player = _addPlayerRequests.Dequeue();
				All.AddPlayer(player);
				SubmitLodestoneRequest(player);
			}
		}

		private void InitRoster()
		{
			try
			{
				_playerTrackPlugin.GetDataManager().InitDataFiles(new[] {"players.dat", "data.meta"});
			}
			catch
			{
				_playerTrackPlugin.LogInfo("Failed to properly initialize but probably will be fine.");
			}
		}

		private void LoadRoster()
		{
			TrackRoster loadedRoster;
			var currentTime = DateUtil.CurrentTime();
			try
			{
				var data = _playerTrackPlugin.GetDataManager().ReadData("players.dat");
				var meta = _playerTrackPlugin.GetDataManager().ReadData("data.meta");
				var metaData = JsonConvert.DeserializeObject<TrackMetaData>(meta, _jsonSerializerSettings);
				if (metaData.Compressed) data = data.Decompress();
				loadedRoster = new TrackRoster(
					JsonConvert.DeserializeObject<Dictionary<string, TrackPlayer>>(data, _jsonSerializerSettings),
					_playerTrackPlugin);

				foreach (var player in loadedRoster.Roster)
				{
					foreach (var world in player.Value.HomeWorlds)
						world.Name = _playerTrackPlugin.GetWorldName(world.Id);
					foreach (var encounter in player.Value.Encounters)
					{
						encounter.Location.PlaceName =
							_playerTrackPlugin.GetPlaceName(encounter.Location.TerritoryType);
						encounter.Location.ContentName =
							_playerTrackPlugin.GetContentName(
								_playerTrackPlugin.GetContentId(encounter.Location.TerritoryType));
						encounter.Job.Code = _playerTrackPlugin.GetJobCode(encounter.Job.Id);
					}

					var lode = player.Value.Lodestone;
					if (lode?.Status != null)
					{
						// refresh lodestone status
						if (lode.Status == TrackLodestoneStatus.Verifying)
							lode.Status = TrackLodestoneStatus.Unverified;
						else if (lode.Status == TrackLodestoneStatus.Updating ||
						         lode.Status == TrackLodestoneStatus.Updated)
							lode.Status = TrackLodestoneStatus.Verified;

						// submit pending verification requests
						if (lode.Status == TrackLodestoneStatus.Unverified)
						{
							SubmitLodestoneRequest(player.Value);
						}
						else if (lode.Status == TrackLodestoneStatus.Failed &&
						         lode.FailureCount < _playerTrackPlugin.Configuration.LodestoneMaxFailure &&
						         currentTime < lode.LastFailed + _playerTrackPlugin.Configuration.LodestoneFailureDelay)
						{
							player.Value.Lodestone.Status = TrackLodestoneStatus.Unverified;
							SubmitLodestoneRequest(player.Value);
						}
					}

					player.Value.PreviouslyLastSeen = player.Value.LastSeen;
				}
			}
			catch
			{
				_playerTrackPlugin.LogInfo("Can't load data so starting fresh.");
				loadedRoster = new TrackRoster(new Dictionary<string, TrackPlayer>(), _playerTrackPlugin);
			}

			All = loadedRoster;
		}
	}
}