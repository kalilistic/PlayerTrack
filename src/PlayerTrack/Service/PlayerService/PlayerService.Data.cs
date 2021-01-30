using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;

// ReSharper disable InvertIf

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public void SaveData()
		{
			try
			{
				var data = JsonConvert.SerializeObject(AllPlayers, _jsonSerializerSettings);
				var metaData = JsonConvert.SerializeObject(new TrackMetaData
				{
					SchemaVersion = _plugin.Configuration.SchemaVersion,
					Compressed = _plugin.Configuration.Compressed
				}, _jsonSerializerSettings);
				if (_plugin.Configuration.Compressed) data = data.Compress();
				_plugin.DataManager.SaveData("players.dat", data);
				_plugin.DataManager.SaveData("data.meta", metaData);
				BackupPlayers();
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to save player data - will try again soon.");
			}
		}

		public void BackupPlayers(bool forceBackup = false)
		{
			if (forceBackup || (DateTime.UtcNow - _plugin.Configuration.LastBackup.ToDateTime())
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
			var currentTime = DateUtil.CurrentTime();
			try
			{
				var data = _plugin.DataManager.ReadData("players.dat");
				var meta = _plugin.DataManager.ReadData("data.meta");
				var metaData = JsonConvert.DeserializeObject<TrackMetaData>(meta, _jsonSerializerSettings);
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
			catch
			{
				_plugin.LogInfo("Can't load data so starting fresh.");
				AllPlayers = new ConcurrentDictionary<string, TrackPlayer>();
			}
		}
	}
}