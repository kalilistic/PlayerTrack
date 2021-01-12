// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;

namespace PlayerTrack
{
	public class CategoryService
	{
		private readonly JsonSerializerSettings _jsonSerializerSettings;
		private readonly IPlayerTrackPlugin _playerTrackPlugin;

		public List<TrackCategory> Categories;

		public CategoryService(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
			_jsonSerializerSettings = SerializerUtil.CamelCaseJsonSerializer();
			InitCategories();
			LoadCategories();
		}

		public TrackCategory GetCategory(int categoryId)
		{
			return Categories.FirstOrDefault(category => category.Id == categoryId);
		}

		public int GetCategoryIndex(string categoryName)
		{
			var categoryIndex = Array.IndexOf(GetCategoryNames(), categoryName);
			if (categoryIndex == -1)
				for (var i = 0; i < Categories.Count; i++)
					if (Categories[i].IsDefault)
						return i;
			return categoryIndex;
		}

		public TrackCategory GetDefaultCategory()
		{
			return Categories.First(category => category.IsDefault);
		}

		public string[] GetCategoryNames()
		{
			return Categories.Select(category => category.Name).ToArray();
		}

		public void MoveUpList(int id)
		{
			try
			{
				var category = Categories.FirstOrDefault(trackCategory => trackCategory.Id == id);
				if (category == null) return;
				var categoryIndex = Categories.IndexOf(category);
				var tradeCategoryIndex = categoryIndex - 1;
				var tradeCategory = Categories[tradeCategoryIndex];
				Categories[tradeCategoryIndex] = category;
				Categories[categoryIndex] = tradeCategory;
			}
			catch
			{
				// ignored
			}
		}

		public void MoveDownList(int id)
		{
			try
			{
				var category = Categories.FirstOrDefault(trackCategory => trackCategory.Id == id);
				if (category == null) return;
				var categoryIndex = Categories.IndexOf(category);
				var tradeCategoryIndex = categoryIndex + 1;
				var tradeCategory = Categories[tradeCategoryIndex];
				Categories[tradeCategoryIndex] = category;
				Categories[categoryIndex] = tradeCategory;
			}
			catch
			{
				// ignored
			}
		}

		public void AddCategory()
		{
			Categories.Insert(0, new TrackCategory
			{
				Id = Categories.Max(category => category.Id) + 1,
				Name = "New Category",
				Color = UIColor.White
			});
		}

		public void DeleteCategory(int index)
		{
			_playerTrackPlugin.GetCategoryService().Categories.RemoveAt(index);
			SaveCategories();
			try
			{
				var categoryIds = _playerTrackPlugin.GetCategoryService().Categories.Select(category => category.Id)
					.ToList();
				foreach (var player in _playerTrackPlugin.RosterService.All.Roster.ToList())
					if (!categoryIds.Contains(player.Value.CategoryId))
						player.Value.CategoryId = 0;
			}
			catch
			{
				// ignored
			}
		}

		private void InitCategories()
		{
			try
			{
				_playerTrackPlugin.GetDataManager().InitDataFiles(new[] {"categories.dat"});
			}
			catch
			{
				_playerTrackPlugin.LogInfo("Failed to properly initialize but probably will be fine.");
			}
		}

		private void LoadCategories()
		{
			try
			{
				var data = _playerTrackPlugin.GetDataManager().ReadData("categories.dat");
				Categories =
					JsonConvert.DeserializeObject<List<TrackCategory>>(data, _jsonSerializerSettings);
				if (Categories == null || Categories.Count == 0) Categories = GetDefaultCategories();
			}
			catch
			{
				_playerTrackPlugin.LogInfo("Can't load category data so starting fresh.");
				Categories = GetDefaultCategories();
			}
		}

		public void SaveCategories()
		{
			try
			{
				var data = JsonConvert.SerializeObject(Categories, _jsonSerializerSettings);
				_playerTrackPlugin.GetDataManager().SaveData("categories.dat", data);
			}
			catch (Exception ex)
			{
				_playerTrackPlugin.LogError(ex, "Failed to save player data - will try again soon.");
			}
		}

		public void ResetCategories()
		{
			Categories = GetDefaultCategories();
			var defaultCategory = Categories.First(category => category.IsDefault);
			defaultCategory.Color = new Vector4(255, 255, 255, 1);
			SaveCategories();
			_playerTrackPlugin.SaveConfig();
		}

		private static List<TrackCategory> GetDefaultCategories()
		{
			return new List<TrackCategory>
			{
				new TrackCategory
				{
					Id = 2,
					Name = "Favorites",
					Color = UIColor.Violet,
					Icon = (int) FontAwesomeIcon.GrinBeam
				},
				new TrackCategory
				{
					Id = 1,
					Name = "Default",
					IsDefault = true,
					Color = new Vector4(255, 255, 255, 1)
				}
			};
		}
	}
}