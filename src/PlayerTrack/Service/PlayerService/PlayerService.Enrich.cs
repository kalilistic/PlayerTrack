using System;
using System.Globalization;
using System.Linq;

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public void EnrichPlayerData(TrackPlayer player)
		{
			AddWorldData(player);
			AddCustomizeData(player);
			AddLocationData(player);
			AddCategoryData(player);
			AddIconData(player);
			player.PreviouslyLastSeen = player.LastSeen;
		}

		private void AddWorldData(TrackPlayer player)
		{
			foreach (var world in player.HomeWorlds)
				world.Name = _plugin.GetWorldName(world.Id);
		}

		private void AddCustomizeData(TrackPlayer player)
		{
			if (player.Gender == null) return;
			var gender = (int)player.Gender;
			player.GenderDisplay = _plugin.GetGender(gender);
			player.RaceDisplay = _plugin.GetRace(player.Race, gender);
			player.TribeDisplay = _plugin.GetTribe(player.Tribe, gender);
			player.HeightDisplay =
				_plugin.ConvertHeightToInches(player.Race, player.Tribe, gender, player.Height).ToString(CultureInfo.CurrentCulture);
		}

		private void AddLocationData(TrackPlayer player)
		{
			var encounters = player.Encounters.ToList();
			try
			{
				foreach (var encounter in player.Encounters)
				{
					encounter.Location.PlaceName =
						_plugin.GetPlaceName(encounter.Location.TerritoryType);
					encounter.Location.ContentName =
						_plugin.GetContentName(
							_plugin.GetContentId(encounter.Location.TerritoryType));
					encounter.Job.Code = _plugin.GetJobCode(encounter.Job.Id);
				}
			}
			catch(Exception ex)
			{
				_plugin.LogError(ex, "Failed to update encounters properly so reverting");
				player.Encounters = encounters;
			}

		}

		private void AddCategoryData(TrackPlayer player)
		{
			var categoryIds = _plugin.CategoryService.CategoryIds;
			if (player.CategoryId != 0 && !categoryIds.Contains(player.CategoryId))
				player.CategoryId = 0;
			player.Category = player.CategoryId == 0
				? _plugin.CategoryService.GetDefaultCategory()
				: _plugin.CategoryService.GetCategory(player.CategoryId);
			player.CategoryIndex = _plugin.CategoryService.GetCategoryIndex(player.CategoryId);
			var categoryPriorities = _plugin.CategoryService.CategoryPriorities;
			player.Priority = categoryPriorities.FirstOrDefault(pair => pair.Key == player.CategoryId).Value;
		}

		private void AddIconData(TrackPlayer player)
		{
			player.IconIndex = Array.IndexOf(_plugin.GetIconCodes(), player.DisplayIcon);
		}
	}
}