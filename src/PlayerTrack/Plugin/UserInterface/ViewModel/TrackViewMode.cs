using System.Collections.Generic;
using System.Linq;

namespace PlayerTrack
{
    public class TrackViewMode
    {
        public static readonly List<TrackViewMode> Views = new List<TrackViewMode>();
        public static readonly List<string> ViewNames = new List<string>();

        public static readonly TrackViewMode CurrentPlayers = new TrackViewMode(0, 0, "Current Players");
        public static readonly TrackViewMode RecentPlayers = new TrackViewMode(1, 1, "Recent Players");
        public static readonly TrackViewMode AllPlayers = new TrackViewMode(2, 2, "All Players");
        public static readonly TrackViewMode SearchForPlayers = new TrackViewMode(3, 3, "Search for Players");
        public static readonly TrackViewMode AddPlayer = new TrackViewMode(4, 4, "Add Player");
        public static readonly TrackViewMode PlayersByCategory = new TrackViewMode(5, 5, "Players By Category");

        private TrackViewMode(int index, int code, string name)
        {
            Index = index;
            Name = name;
            Code = code;
            Views.Add(this);
            ViewNames.Add(name);
        }

        public int Index { get; set; }
        public int Code { get; set; }
        public string Name { get; set; }

        public static TrackViewMode GetViewModeByIndex(int index)
        {
            return Views.FirstOrDefault(view => view.Index == index);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}