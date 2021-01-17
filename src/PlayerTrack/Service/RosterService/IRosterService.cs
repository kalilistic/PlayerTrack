using System.Collections.Generic;

namespace PlayerTrack
{
	public interface IRosterService
	{
		TrackRoster Current { get; set; }
		TrackRoster All { get; set; }
		TrackPlayer SelectedPlayer { get; set; }
		void ClearPlayers();
		void ProcessPlayers(List<TrackPlayer> incomingPlayers);
		void DeletePlayer(string key);
		void ChangeSelectedPlayer(string key);
		void BackupRoster(bool forceBackup = false);
		void SaveData();
		bool IsNewPlayer(string name, string worldName);
	}
}