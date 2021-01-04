using System.Collections.Generic;
using System.Linq;

namespace PlayerTrack
{
	public class TrackPlayerMode
	{
		public static readonly List<TrackPlayerMode> Views = new List<TrackPlayerMode>();
		public static readonly List<string> ViewNames = new List<string>();

		public static readonly TrackPlayerMode CurrentPlayers = new TrackPlayerMode(0, 0, "Current Players");
		public static readonly TrackPlayerMode RecentPlayers = new TrackPlayerMode(1, 1, "Recent Players");
		public static readonly TrackPlayerMode AllPlayers = new TrackPlayerMode(2, 2, "All Players");
		public static readonly TrackPlayerMode SearchForPlayers = new TrackPlayerMode(3, 3, "Search for Players");

		private TrackPlayerMode(int index, int code, string name)
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

		public static TrackPlayerMode GetPlayerModeByIndex(int index)
		{
			return Views.FirstOrDefault(view => view.Index == index);
		}

		public override string ToString()
		{
			return Name;
		}
	}
}