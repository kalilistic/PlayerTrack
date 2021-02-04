using System;
using System.Globalization;
using System.Linq;

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public void EnrichPlayerData(TrackPlayer player)
		{
			try
			{
				AddWorldData(player);
				AddCustomizeData(player);
				AddLocationData(player);
				AddCategoryData(player);
				AddIconData(player);
				player.PreviouslyLastSeen = player.LastSeen;
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to add data to player " + player.Key);
			}
		}

		private void AddWorldData(TrackPlayer player)
		{
			try
			{
				foreach (var world in player.HomeWorlds)
					world.Name = _plugin.GetWorldName(world.Id);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to add world data to player " + player.Key);
			}

		}

		private void AddCustomizeData(TrackPlayer player)
		{
			try
			{
				if (player.Gender == null) return;
				var gender = (int) player.Gender;
				player.GenderDisplay = _plugin.GetGender(gender);
				player.RaceDisplay = _plugin.GetRace(player.Race, gender);
				player.TribeDisplay = _plugin.GetTribe(player.Tribe, gender);
				player.HeightDisplay =
					_plugin.ConvertHeightToInches(player.Race, player.Tribe, gender, player.Height)
						.ToString(CultureInfo.CurrentCulture);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to add customize data to player " + player.Key);
			}

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
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to update encounters properly so reverting");
				player.Encounters = encounters;
			}
		}

		private void AddCategoryData(TrackPlayer player)
		{
			try
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
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to add category data to player " + player.Key);
			}
		}

		private void AddIconData(TrackPlayer player)
		{
			try
			{
				player.IconIndex = Array.IndexOf(_plugin.GetIconCodes(), player.DisplayIcon);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to add icon data to player " + player.Key);
			}

		}
	}
}