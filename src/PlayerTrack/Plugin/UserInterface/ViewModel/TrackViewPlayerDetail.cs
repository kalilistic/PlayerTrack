using System.Collections.Generic;
using System.Numerics;

namespace PlayerTrack
{
	public class TrackViewPlayerDetail
	{
		public bool AlertEnabled;
		public int CategoryIndex;
		public string[] CategoryNames;
		public Vector4 Color;
		public List<TrackViewEncounter> Encounters;
		public string FirstSeen;
		public string FreeCompany;
		public string HomeWorld;
		public string Icon;
		public int[] IconCodes;
		public int IconIndex;
		public string[] IconNames;

		public string Key;
		public string LastSeen;
		public string LodestoneStatus;
		public string LodestoneUrl;
		public string Name;
		public string Notes;
		public string PreviousHomeWorlds;
		public string PreviousNames;
		public string SeenCount;
		public string Gender;
		public string Race;
		public string Tribe;

		public static TrackPlayer Map(TrackViewPlayerDetail player, IPlayerTrackPlugin plugin)
		{
			var originalPlayer = plugin.PlayerService.GetPlayer(player.Key);
			if (originalPlayer == null) return null;
			if (player.Color != originalPlayer.DisplayColor) originalPlayer.Color = player.Color;

			if (!((FontAwesomeIcon) originalPlayer.DisplayIcon).ToIconString().Equals(player.Icon))
			{
				originalPlayer.Icon = plugin.GetIconCodes()[player.IconIndex];
				originalPlayer.IconIndex = player.IconIndex;
			}

			originalPlayer.CategoryId = plugin.CategoryService.GetCategoryId(player.CategoryIndex);
			originalPlayer.Notes = player.Notes;
			originalPlayer.Alert.State = player.AlertEnabled ? TrackAlertState.Enabled : TrackAlertState.NotSet;
			return originalPlayer;
		}

		public static TrackViewPlayerDetail Map(TrackPlayer player, IPlayerTrackPlugin plugin)
		{
			return new TrackViewPlayerDetail
			{
				CategoryNames = plugin.CategoryService.GetCategoryNames(),
				IconNames = plugin.GetIconNames(),
				IconCodes = plugin.GetIconCodes(),

				Key = player.Key,
				Name = player.Name,
				Color = player.DisplayColor,
				Icon = ((FontAwesomeIcon) player.DisplayIcon).ToIconString(),
				PreviousNames = player.PreviousNames,
				LodestoneUrl = player.Lodestone.GetProfileUrl(plugin.Configuration.LodestoneLocale),
				FirstSeen = player.FirstSeen,
				HomeWorld = player.HomeWorld,
				PreviousHomeWorlds = player.PreviousWorlds,
				LastSeen = player.LastSeen,
				FreeCompany = player.FreeCompanyDisplay(plugin.InContent),
				SeenCount = player.SeenCount,
				Gender = player.GenderDisplay,
				Race = player.RaceDisplay,
				Tribe = player.TribeDisplay,
				LodestoneStatus = player.Lodestone.Status.ToString(),
				CategoryIndex = player.CategoryIndex,
				IconIndex = player.IconIndex,
				Notes = player.Notes,
				Encounters = TrackViewEncounter.Map(player.Encounters),
				AlertEnabled = player.Alert.State == TrackAlertState.Enabled
			};
		}
	}
}