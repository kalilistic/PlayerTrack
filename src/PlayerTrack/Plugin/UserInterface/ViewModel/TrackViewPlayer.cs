using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PlayerTrack
{
	public class TrackViewPlayer
	{
		public int ActorId;
		public Vector4 Color;
		public string Icon;
		public string Key;
		public string Name;
		public int Priority;

		public static List<TrackViewPlayer> Map(
			ConcurrentDictionary<string, TrackPlayer> players)
		{
			if (players == null) return new List<TrackViewPlayer>();
			var listItems = new List<TrackViewPlayer>();
			try
			{
				listItems.AddRange(players.ToList()
					.Select(player => new TrackViewPlayer
					{
						Key = player.Key,
						Name = player.Value.Name,
						Color = player.Value.DisplayColor,
						Icon = ((FontAwesomeIcon) player.Value.DisplayIcon).ToIconString(),
						Priority = player.Value.Priority,
						ActorId = player.Value.ActorId
					}));
				listItems = new List<TrackViewPlayer>(listItems.OrderBy(player => player.Priority)
					.ThenBy(player => player.Name));
				return listItems;
			}
			catch
			{
				return listItems;
			}
		}
	}
}