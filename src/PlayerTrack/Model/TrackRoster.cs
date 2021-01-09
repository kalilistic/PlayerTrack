using System.Collections.Generic;
using System.Linq;

namespace PlayerTrack
{
	public class TrackRoster
	{
		private readonly IPlayerTrackPlugin _playerTrackPlugin;

		public TrackRoster(Dictionary<string, TrackPlayer> roster, IPlayerTrackPlugin plugin)
		{
			_playerTrackPlugin = plugin;
			Roster = roster;
		}

		public Dictionary<string, TrackPlayer> Roster { get; set; }

		public bool IsNewPlayer(string playerKey)
		{
			return !Roster.ContainsKey(playerKey);
		}

		public TrackPlayer GetPlayer(string playerKey)
		{
			return Roster[playerKey];
		}

		public void AddPlayer(TrackPlayer player)
		{
			Roster.Add(player.Key, player);
		}

		public void DeletePlayer(string playerKey)
		{
			if (Roster.ContainsKey(playerKey)) Roster.Remove(playerKey);
		}

		public void MergePlayer(TrackPlayer playerToMerge)
		{
			Roster[playerToMerge.Key].Merge(playerToMerge);
		}

		public void UpdatePlayer(TrackPlayer player)
		{
			if (player.Lodestone.Id != 0 && Roster[player.Key].Lodestone.Id == 0)
				Roster[player.Key].Lodestone.Id = player.Lodestone.Id;
			if (Roster[player.Key].FreeCompany.Equals("N/A")) Roster[player.Key].FreeCompany = player.FreeCompany;
		}

		public void AddEncounter(string playerKey, TrackEncounter encounter)
		{
			Roster[playerKey].Encounters.Add(encounter);
		}

		public bool IsNewEncounter(string playerKey, TrackEncounter encounter)
		{
			return Roster[playerKey].Encounters.Last().Location.TerritoryType !=
			       encounter.Location.TerritoryType ||
			       DateUtil.CurrentTime() - Roster[playerKey].Encounters.Last().Updated >=
			       _playerTrackPlugin.Configuration.NewEncounterThreshold;
		}

		public void UpdateEncounter(string playerKey, TrackEncounter encounter)
		{
			encounter.Created = Roster[playerKey].Encounters[Roster[playerKey].Encounters.Count - 1].Created;
			Roster[playerKey].Encounters[Roster[playerKey].Encounters.Count - 1] = encounter;
		}

		public void SortByName()
		{
			Roster = Roster.OrderBy(entry => entry.Value.Name)
				.ToDictionary(entry => entry.Key, entry => entry.Value);
		}

		public Dictionary<string, TrackPlayer> FilterByLastUpdate(long time)
		{
			var currentTime = DateUtil.CurrentTime();
			return Roster.Where(entry =>
					currentTime - entry.Value.Encounters[entry.Value.Encounters.Count - 1].Updated < time)
				.ToDictionary(entry => entry.Key, entry => entry.Value);
		}

		public Dictionary<string, TrackPlayer> FilterByName(string name)
		{
			return Roster.Where(entry => entry.Value.Names.Any(s => s.Contains(name)))
				.ToDictionary(entry => entry.Key, entry => entry.Value);
		}
	}
}