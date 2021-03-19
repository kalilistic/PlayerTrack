using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PlayerTrack.Mock
{
    public abstract class MockPlayerService : IPlayerService
    {
        public void ProcessLodestoneRequests()
        {
            throw new NotImplementedException();
        }

        public void SaveData()
        {
            throw new NotImplementedException();
        }

        public void BackupPlayers(bool forceBackup = false)
        {
            throw new NotImplementedException();
        }

        public ConcurrentDictionary<string, TrackPlayer> RecentPlayers { get; set; }
        public ConcurrentDictionary<string, TrackPlayer> CurrentPlayers { get; set; }
        public ConcurrentDictionary<string, TrackPlayer> AllPlayers { get; set; }

        public TrackPlayer GetPlayer(string playerKey)
        {
            throw new NotImplementedException();
        }

        public void UpdatePlayer(TrackPlayer player)
        {
            throw new NotImplementedException();
        }

        public bool DeletePlayer(string playerKey)
        {
            throw new NotImplementedException();
        }

        public bool ResetPlayer(string playerKey)
        {
            throw new NotImplementedException();
        }

        public bool AddPlayer(string name, string worldName)
        {
            throw new NotImplementedException();
        }

        public void EnrichPlayerData(TrackPlayer player)
        {
            throw new NotImplementedException();
        }

        public ConcurrentDictionary<string, TrackPlayer> SearchByName(string name)
        {
            throw new NotImplementedException();
        }

        public abstract event PlayerService.ProcessPlayersEventHandler PlayersProcessed;
        public void ProcessPlayers(List<TrackPlayer> incomingPlayers)
        {
            throw new NotImplementedException();
        }
    }
}