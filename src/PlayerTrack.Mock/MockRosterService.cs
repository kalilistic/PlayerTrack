using System;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
	public class MockRosterService : IRosterService
	{
		public TrackRoster Current { get; set; }
		public TrackRoster All { get; set; }
		public TrackPlayer SelectedPlayer { get; set; }

		public void ClearPlayers()
		{
			throw new NotImplementedException();
		}

		public void ProcessPlayers(List<TrackPlayer> incomingPlayers)
		{
			throw new NotImplementedException();
		}

		public void DeletePlayer(string key)
		{
			throw new NotImplementedException();
		}

		public void ChangeSelectedPlayer(string key)
		{
			throw new NotImplementedException();
		}

		public void BackupRoster(bool forceBackup = false)
		{
			throw new NotImplementedException();
		}

		public void SaveData()
		{
			throw new NotImplementedException();
		}
	}
}