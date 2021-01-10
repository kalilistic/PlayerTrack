// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToNullCoalescingExpression
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

namespace PlayerTrack
{
	[JsonObject(MemberSerialization.OptIn)]
	public class TrackPlayer
	{
		private string _abbreviatedNotes;
		private string _firstSeen;
		private string _homeWorld;
		private string _key;
		private string _lastSeen;
		private string _name;
		private string _seenCount;

		[JsonProperty] public List<string> Names { get; set; }
		[JsonProperty] public List<TrackWorld> HomeWorlds { get; set; }
		[JsonProperty] [DefaultValue(0)] public int Icon { get; set; }
		[JsonProperty] public Vector4? Color { get; set; }
		[JsonProperty] public string FreeCompany { get; set; }
		[JsonProperty] public List<TrackEncounter> Encounters { get; set; }
		[JsonProperty] [DefaultValue("")] public string Notes { get; set; } = string.Empty;
		[JsonProperty] public TrackLodestone Lodestone { get; set; } = new TrackLodestone();
		[JsonProperty] [DefaultValue(0)] public int ActorId { get; set; }
		[JsonProperty] [DefaultValue(false)] public bool IsManual { get; set; }
		[JsonProperty] public TrackAlert Alert { get; set; } = new TrackAlert();
		public string PreviouslyLastSeen { get; set; } = string.Empty;

		public long Created => Encounters.First().Created;

		public string FirstSeen
		{
			get
			{
				if (_firstSeen == null) _firstSeen = Encounters.First().Created.ToTimeSpan();
				return _firstSeen;
			}
		}

		public string LastSeen
		{
			get
			{
				if (_lastSeen == null) _lastSeen = Encounters.Last().Updated.ToTimeSpan();
				return _lastSeen;
			}
		}

		public string SeenCount
		{
			get
			{
				if (_seenCount == null) _seenCount = GetEncounterCount() + "x";
				return _seenCount;
			}
		}

		public string Key
		{
			get
			{
				if (_key == null) _key = CreateKey(Name, HomeWorlds[0]?.Id ?? 0);
				return _key;
			}
		}

		public string Name
		{
			get
			{
				if (_name == null) _name = string.IsNullOrEmpty(Names?[0]) ? "No One" : Names[0];
				;
				return _name;
			}
		}

		public string HomeWorld
		{
			get
			{
				if (_homeWorld == null)
					_homeWorld = string.IsNullOrEmpty(HomeWorlds?[0]?.Name) ? "Nowhere" : HomeWorlds[0].Name;
				return _homeWorld;
			}
		}

		public string AbbreviatedNotes
		{
			get
			{
				if (_abbreviatedNotes == null)
				{
					if (string.IsNullOrEmpty(Notes))
						_abbreviatedNotes = "None.";
					else
						_abbreviatedNotes = Notes.Length < 30
							? Notes.Replace('\n', ' ').EnsureEndsWithDot()
							: Notes.Replace('\n', ' ').Substring(0, 30) + "...";
				}

				return _abbreviatedNotes;
			}
		}

		public int GetEncounterCount()
		{
			if (IsManual) return Encounters.Count - 1;

			return Encounters.Count;
		}

		public static string CreateKey(string name, uint worldId)
		{
			return string.Concat(name.Replace(' ', '_').ToUpper(), "_", worldId);
		}

		public void Merge(TrackPlayer playerToMerge)
		{
			if (Names == null) Names = new List<string>();
			if (HomeWorlds == null) HomeWorlds = new List<TrackWorld>();

			if (Created < playerToMerge.Created)
			{
				playerToMerge.Names.Reverse();
				foreach (var name in playerToMerge.Names)
					if (!Names.Contains(name))
						Names.Insert(0, name);
				playerToMerge.HomeWorlds.Reverse();
				foreach (var homeWorld in playerToMerge.HomeWorlds)
					if (HomeWorlds.All(world => world.Id != homeWorld.Id))
						HomeWorlds.Insert(0, new TrackWorld
						{
							Id = homeWorld.Id,
							Name = homeWorld.Name
						});
				Notes += playerToMerge.Notes;
			}
			else
			{
				foreach (var name in playerToMerge.Names)
					if (!Names.Contains(name))
						Names.Add(name);
				playerToMerge.HomeWorlds.Reverse();
				foreach (var homeWorld in playerToMerge.HomeWorlds)
					if (HomeWorlds.All(world => world.Id != homeWorld.Id))
						HomeWorlds.Add(new TrackWorld
						{
							Id = homeWorld.Id,
							Name = homeWorld.Name
						});
				Notes = playerToMerge.Notes + Notes;
				Icon = playerToMerge.Icon;
				Color = playerToMerge.Color;
			}

			Encounters = Encounters.Concat(playerToMerge.Encounters).ToList()
				.OrderBy(encounter => encounter.Created).ToList();
		}

		public bool IsNewName(string newName)
		{
			if (string.IsNullOrEmpty(newName)) return false;
			return string.IsNullOrEmpty(Names?[0]) || !Names[0].Equals(newName);
		}

		public void UpdateName(string newName, int index = 0)
		{
			if (!IsNewName(newName)) return;
			if (Names == null) Names = new List<string>();
			Names.Insert(index, newName);
		}

		public bool IsNewHomeWorld(TrackWorld newWorld)
		{
			if (newWorld?.Id == null) return false;
			return HomeWorlds?[0].Id == null || HomeWorlds[0].Id != newWorld.Id;
		}

		public void UpdateHomeWorld(TrackWorld newWorld, int index = 0)
		{
			if (!IsNewHomeWorld(newWorld)) return;
			if (HomeWorlds == null) HomeWorlds = new List<TrackWorld>();
			HomeWorlds.Insert(index, newWorld);
		}

		public void ClearBackingFields()
		{
			_firstSeen = null;
			_lastSeen = null;
			_seenCount = null;
			_abbreviatedNotes = null;
			_key = null;
			_name = null;
			_homeWorld = null;
			foreach (var encounter in Encounters) encounter.ClearBackingFields();
		}
	}
}