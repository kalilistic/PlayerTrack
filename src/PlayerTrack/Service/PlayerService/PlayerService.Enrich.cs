using System;
using System.Linq;

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public void EnrichPlayerData(TrackPlayer player)
		{
			AddWorldData(player);
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

		private void AddLocationData(TrackPlayer player)
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