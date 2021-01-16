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
		private readonly Queue<KeyValuePair<int, CategoryModification>> _categoryModification =
			new Queue<KeyValuePair<int, CategoryModification>>();

		private readonly JsonSerializerSettings _jsonSerializerSettings;
		private readonly IPlayerTrackPlugin _playerTrackPlugin;

		public List<TrackCategory> Categories;

		public CategoryService(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
			_jsonSerializerSettings = SerializerUtil.CamelCaseJsonSerializer();
			InitCategories();
			LoadCategories();
			ClearDeletedCategories();
			SetPlayerPriority();
		}

		public void ClearDeletedCategories()
		{
			try
			{
				var categoryIds = Categories.Select(category => category.Id).ToList();
				foreach (var player in _playerTrackPlugin.RosterService.All.Roster)
					if (player.Value.CategoryId != 0 && !categoryIds.Contains(player.Value.CategoryId))
					{
						player.Value.CategoryId = 0;
					}
			}
			catch (Exception ex)
			{
				_playerTrackPlugin.LogError(ex, "Failed to clear deleted categories.");
			}
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
			_categoryModification.Enqueue(
				new KeyValuePair<int, CategoryModification>(id, CategoryModification.MoveUpCategory));
		}

		public void ProcessCategoryModifications()
		{
			var modifiedCategory = false;
			while (_categoryModification.Count > 0)
			{
				modifiedCategory = true;
				var categoryMod = _categoryModification.Dequeue();
				if (categoryMod.Value == CategoryModification.MoveUpCategory)
				{
					var category = Categories.FirstOrDefault(trackCategory => trackCategory.Id == categoryMod.Key);
					if (category == null) return;
					var categoryIndex = Categories.IndexOf(category);
					var tradeCategoryIndex = categoryIndex - 1;
					var tradeCategory = Categories[tradeCategoryIndex];
					Categories[tradeCategoryIndex] = category;
					Categories[categoryIndex] = tradeCategory;
				}
				else if (categoryMod.Value == CategoryModification.MoveDownCategory)
				{
					var category = Categories.FirstOrDefault(trackCategory => trackCategory.Id == categoryMod.Key);
					if (category == null) return;
					var categoryIndex = Categories.IndexOf(category);
					var tradeCategoryIndex = categoryIndex + 1;
					var tradeCategory = Categories[tradeCategoryIndex];
					Categories[tradeCategoryIndex] = category;
					Categories[categoryIndex] = tradeCategory;
				}
				else if (categoryMod.Value == CategoryModification.AddCategory)
				{
					Categories.Insert(0, new TrackCategory
					{
						Id = Categories.Max(category => category.Id) + 1,
						Name = "New Category",
						Color = UIColor.White
					});
				}
				else if (categoryMod.Value == CategoryModification.DeleteCategory)
				{
					_playerTrackPlugin.GetCategoryService().Categories.RemoveAt(categoryMod.Key);
					ClearDeletedCategories();
				}
			}

			if (modifiedCategory)
			{
				SetPlayerPriority();
				SaveCategories();
			}
		}

		public void SetPlayerPriority()
		{
			try
			{
				var categoryPriorities = Categories.Select((t, i) => new KeyValuePair<int, int>(t.Id, i + 1)).ToList();
				foreach (var player in _playerTrackPlugin.RosterService.All.Roster.ToList())
					player.Value.Priority = categoryPriorities
						.FirstOrDefault(pair => pair.Key == player.Value.CategoryId).Value;
			}
			catch (Exception ex)
			{
				_playerTrackPlugin.LogError(ex, "Failed to set priority");
			}
		}

		public void MoveDownList(int id)
		{
			_categoryModification.Enqueue(
				new KeyValuePair<int, CategoryModification>(id, CategoryModification.MoveDownCategory));
		}

		public void AddCategory()
		{
			_categoryModification.Enqueue(
				new KeyValuePair<int, CategoryModification>(0, CategoryModification.AddCategory));
		}

		public void DeleteCategory(int index)
		{
			_categoryModification.Enqueue(
				new KeyValuePair<int, CategoryModification>(index, CategoryModification.DeleteCategory));
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
					Id = 1,
					Name = "Favorites",
					Color = UIColor.Violet,
					Icon = (int) FontAwesomeIcon.GrinBeam
				},
				new TrackCategory
				{
					Id = 0,
					Name = "Default",
					IsDefault = true,
					Color = new Vector4(255, 255, 255, 1)
				}
			};
		}

		private enum CategoryModification
		{
			AddCategory,
			DeleteCategory,
			MoveUpCategory,
			MoveDownCategory
		}
	}
}