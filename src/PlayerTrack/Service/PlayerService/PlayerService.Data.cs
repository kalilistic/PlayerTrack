// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable InvertIf

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PlayerTrack
{
	public partial class PlayerService
	{
		private bool upgradeAttempted;
		
		public void SaveData()
		{
			try
			{
				// save player data
				var data = new List<string> {string.Empty};
				if (_plugin.Configuration.Compressed)
					foreach (var entry in AllPlayers)
						data.Add(JsonConvert.SerializeObject(entry, _jsonSerializerSettings).Compress());
				else
					foreach (var entry in AllPlayers)
						data.Add(JsonConvert.SerializeObject(entry, _jsonSerializerSettings));
				_plugin.DataManager.SaveDataList("players.dat", data);

				// save meta data
				var metaData = JsonConvert.SerializeObject(new TrackMetaData
				{
					Compressed = _plugin.Configuration.Compressed
				});
				_plugin.DataManager.SaveDataStr("data.meta", metaData);
				BackupPlayers();
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to save player data - will try again soon.");
			}
		}

		public void BackupPlayers(bool forceBackup = false)
		{
			if (forceBackup || _plugin.Configuration.LastBackup == 0 ||
			    (DateTime.UtcNow - _plugin.Configuration.LastBackup.ToDateTime())
			    .TotalMilliseconds >
			    _plugin.Configuration.BackupFrequency)
			{
				_plugin.DataManager.CreateBackup();
				_plugin.DataManager.DeleteBackups(_plugin.Configuration.BackupRetention);
				_plugin.Configuration.LastBackup = DateUtil.CurrentTime();
				_plugin.SaveConfig();
			}
		}

		private void InitPlayers()
		{
			try
			{
				_plugin.DataManager.InitDataFiles(new[] {"players.dat", "data.meta"});
			}
			catch
			{
				_plugin.LogInfo("Failed to properly initialize but probably will be fine.");
			}
		}

		private void LoadPlayers()
		{
			try
			{
				var metaData = LoadMetaData();
				if (metaData.SchemaVersion == 1)
					LoadPlayersV1(metaData);
				else
					LoadPlayersV2(metaData);
				_plugin.LogInfo("Loaded players successfully.");
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Can't load data so starting fresh.");
				AllPlayers = new ConcurrentDictionary<string, TrackPlayer>();
			}
		}

		private void LoadPlayersV2(TrackMetaData metaData)
		{
			_plugin.LogInfo("Latest schema detected.");
			var currentTime = DateUtil.CurrentTime();
			var data = _plugin.DataManager.ReadDataList("players.dat");
			var players = new ConcurrentDictionary<string, TrackPlayer>();
            if (data != null && data.Count > 0)
			{
				if (metaData.Compressed)
					foreach (var entry in data)
					{
                        if (string.IsNullOrEmpty(entry)) continue;
						var player = JsonConvert.DeserializeObject<KeyValuePair<string, TrackPlayer>>(
							entry.Decompress(),
							_jsonSerializerSettings);
						players.TryAdd(player.Key, player.Value);
					}
				else
					foreach (var entry in data)
					{
						if (string.IsNullOrEmpty(entry)) continue;
						var player = JsonConvert.DeserializeObject<KeyValuePair<string, TrackPlayer>>(entry,
							_jsonSerializerSettings);
						players.TryAdd(player.Key, player.Value);
					}
			}

            AllPlayers = players;
            foreach (var player in AllPlayers)
			{
                EnrichPlayerData(player.Value);
				UpdateLodestoneStatus(player.Value);
				SubmitLodestoneRequest(player.Value, currentTime);
            }
        }

		private void LoadPlayersV1(TrackMetaData metaData)
		{
			try
			{
				_plugin.LogInfo("V1 schema detected...will load and upgrade to latest.");
				_plugin.LogInfo("Compression: " + metaData.Compressed);
				var currentTime = DateUtil.CurrentTime();
				var data = _plugin.DataManager.ReadDataStr("players.dat");
				if (metaData.Compressed) data = data.Decompress();

				AllPlayers = JsonConvert.DeserializeObject<ConcurrentDictionary<string, TrackPlayer>>(data,
					_jsonSerializerSettings);

				foreach (var player in AllPlayers)
				{
					EnrichPlayerData(player.Value);
					UpdateLodestoneStatus(player.Value);
					SubmitLodestoneRequest(player.Value, currentTime);
				}
			}
			catch (FormatException)
			{
				_plugin.LogInfo("FormatException so trying other compression.");
				if (upgradeAttempted) return;
				upgradeAttempted = true;
				metaData.Compressed = !metaData.Compressed;
				LoadPlayersV1(metaData);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to load v1 players.");
			}
		}

		private TrackMetaData LoadMetaData()
		{
			try
			{
				var meta = _plugin.DataManager.ReadDataStr("data.meta");
				if (!string.IsNullOrEmpty(meta)) return JsonConvert.DeserializeObject<TrackMetaData>(meta);
				_plugin.LogInfo("Failed to load meta data so starting fresh.");
				return new TrackMetaData
				{
					Compressed = _plugin.Configuration.Compressed
				};
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to load meta data so starting fresh.");
				return new TrackMetaData
				{
					Compressed = _plugin.Configuration.Compressed
				};
			}
		}
	}
}