using System;
using System.Collections.Concurrent;
using System.Linq;

// ReSharper disable InvertIf

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public ConcurrentDictionary<string, TrackPlayer> SearchByName(string name)
		{
			try
			{
				return new ConcurrentDictionary<string, TrackPlayer>(AllPlayers
					.Where(entry => entry.Value.Names.Any(s => s.Contains(name)))
					.ToDictionary(entry => entry.Key, entry => entry.Value));
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to filter players by name: " + name);
				return new ConcurrentDictionary<string, TrackPlayer>();
			}
		}
	}
}