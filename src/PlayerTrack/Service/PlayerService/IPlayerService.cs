using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PlayerTrack
{
	public interface IPlayerService
	{
		void ProcessLodestoneRequests();
		void SaveData();
		void BackupPlayers(bool forceBackup = false);
		ConcurrentDictionary<string, TrackPlayer> RecentPlayers { get; set; }
		ConcurrentDictionary<string, TrackPlayer> CurrentPlayers { get; set; }
		ConcurrentDictionary<string, TrackPlayer> AllPlayers { get; set; }
		TrackPlayer GetPlayer(string playerKey);
		void UpdatePlayer(TrackPlayer player);
		bool DeletePlayer(string playerKey);
		bool ResetPlayer(string playerKey);
		bool AddPlayer(string name, string worldName);
		void EnrichPlayerData(TrackPlayer player);
		ConcurrentDictionary<string, TrackPlayer> SearchByName(string name);
		event PlayerService.ProcessPlayersEventHandler PlayersProcessed;
		void ProcessPlayers(List<TrackPlayer> incomingPlayers);
	}
}