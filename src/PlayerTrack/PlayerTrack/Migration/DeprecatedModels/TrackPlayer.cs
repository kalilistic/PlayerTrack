#pragma warning disable CS1591
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable SA1003
#pragma warning disable SA1009
#pragma warning disable SA1101
#pragma warning disable SA1134
#pragma warning disable SA1204
#pragma warning disable SA1309
#pragma warning disable SA1413
#pragma warning disable SA1516
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Newtonsoft.Json;

// ReSharper disable All
namespace PlayerTrack
{
    [Obsolete]
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackPlayer
    {
        private string _abbreviatedNotes;
        private int? _displayIcon;
        private string _firstSeen;
        private string _homeWorld;
        private string _key;
        private string _lastSeen;
        private string _name;
        private string _previousNames;
        private string _previousWorlds;
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
        [JsonProperty] [DefaultValue(0)] public int CategoryId { get; set; }
        [JsonProperty] [DefaultValue(null)] public int? Gender { get; set; }
        [JsonProperty] [DefaultValue(0)] public int Race { get; set; }
        [JsonProperty] [DefaultValue(0)] public int Tribe { get; set; }
        [JsonProperty] [DefaultValue(0)] public int Height { get; set; }
        public int CategoryIndex { get; set; }
        public string PreviouslyLastSeen { get; set; } = string.Empty;
        public int Priority { get; set; }
        public TrackCategory Category { get; set; }
        public int IconIndex { get; set; }
        public string GenderDisplay { get; set; } = "N/A";
        public string RaceDisplay { get; set; } = "N/A";
        public string TribeDisplay { get; set; } = "N/A";
        public string HeightDisplay { get; set; } = "N/A";

        public long Created => Encounters.First().Created;

        public bool AlertEnabled
        {
            get
            {
                if (Alert.State == TrackAlertState.NotSet)
                    return Category.EnableAlerts;
                return Alert.State == TrackAlertState.Enabled;
            }
        }

        public Vector4 DisplayColor => Color ?? Category.Color;

        public int DisplayIcon
        {
            get
            {
                if (_displayIcon == null)
                {
                    if (Icon != 0)
                        _displayIcon = Icon;
                    else if (Category.Icon != 0)
                        _displayIcon = Category.Icon;
                    else
                        _displayIcon = FontAwesomeIcon.User.ToIconChar();
                }

                return (int) _displayIcon;
            }
        }

        public string PreviousNames
        {
            get
            {
                if (_previousNames == null)
                    _previousNames = Names.Count < 2 ? string.Empty : string.Join(", ", Names.Skip(1));

                return _previousNames;
            }
        }

        public string PreviousWorlds
        {
            get
            {
                if (_previousWorlds == null)
                {
                    var homeWorldNames = HomeWorlds.Select(world => world.Name).ToList();
                    _previousWorlds = homeWorldNames.Count < 2
                        ? string.Empty
                        : string.Join(", ", homeWorldNames.Skip(1));
                }

                return _previousWorlds;
            }
        }

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
                if (_seenCount == null) _seenCount = Encounters.Count + "x";
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
                    {
                        _abbreviatedNotes = "None";
                    }
                    else
                    {
                        _abbreviatedNotes = Notes.Length < 30
                            ? Notes.Replace('\n', ' ').EnsureEndsWithDot()
                            : Notes.Replace('\n', ' ').Substring(0, 30) + "...";
                    }
                }

                return _abbreviatedNotes;
            }
        }

        public string FreeCompanyDisplay(bool inContent)
        {
            if (!string.IsNullOrEmpty(FreeCompany)) return FreeCompany;
            return inContent ? "N/A" : "None";
        }

        public static string CreateKey(string name, uint worldId)
        {
            return string.Concat(name.Replace(' ', '_').ToUpper(), "_", worldId);
        }

        public void NonDestructiveMerge(TrackPlayer originalPlayer)
        {
            if (Color == null) Color = originalPlayer.Color;
            if (Icon == 0) Icon = originalPlayer.Icon;
            if (CategoryId == 0) CategoryId = originalPlayer.CategoryId;
            Notes = originalPlayer.Notes + " " + Notes;
            ClearBackingFields();
        }

        public void Merge(TrackPlayer originalPlayer)
        {
            // simple fields just use original player
            FreeCompany = originalPlayer.FreeCompany;
            CategoryId = originalPlayer.CategoryId;
            Icon = originalPlayer.Icon;
            Color = originalPlayer.Color;
            Alert = originalPlayer.Alert;
            CategoryId = originalPlayer.CategoryId;

            // names
            if (Names == null) Names = new List<string>();
            foreach (var name in originalPlayer.Names)
            {
                if (!Names.Contains(name))
                    Names.Add(name);
            }

            // home worlds
            if (HomeWorlds == null) HomeWorlds = new List<TrackWorld>();
            var newPlayerWorldIds = HomeWorlds.Select(world => world.Id).ToList();
            var originalPlayerWorldIds = originalPlayer.HomeWorlds.Select(world => world.Id).ToList();
            var originalPlayerWorldNames = originalPlayer.HomeWorlds.Select(world => world.Name).ToList();
            foreach (var worldId in originalPlayer.HomeWorlds.Select(world => world.Id).ToList())
            {
                if (!newPlayerWorldIds.Contains(worldId))
                {
                    HomeWorlds.Add(new TrackWorld
                    {
                        Id = worldId,
                        Name = originalPlayerWorldNames[originalPlayerWorldIds.IndexOf(worldId)]
                    });
                }
            }

            // notes
            Notes = originalPlayer.Notes + " " + Notes;

            // encounters
            if (Encounters == null) Encounters = new List<TrackEncounter>();
            Encounters = Encounters.Concat(originalPlayer.Encounters).ToList()
                .OrderBy(encounter => encounter.Created).ToList();

            // reset to ensure latest props
            ClearBackingFields();
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
            _previousNames = null;
            _previousWorlds = null;
            _displayIcon = null;
            foreach (var encounter in Encounters) encounter.ClearBackingFields();
        }
    }
}
