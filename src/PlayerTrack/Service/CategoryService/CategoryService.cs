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
	public class CategoryService : ICategoryService
	{
		private readonly object _categoryLock = new object();
		private readonly JsonSerializerSettings _jsonSerializerSettings;
		private readonly IPlayerTrackPlugin _plugin;

		private List<TrackCategory> _categories;
		public List<int> CategoryIds;

		public List<KeyValuePair<int, int>> CategoryPriorities;

		public CategoryService(IPlayerTrackPlugin plugin)
		{
			_plugin = plugin;
			_jsonSerializerSettings = SerializerUtil.CamelCaseJsonSerializer();
			InitCategories();
			LoadCategories();
			BuildCategoryLists();
		}

		public TrackCategory GetCategory(int categoryId)
		{
			try
			{
				var categories = GetCategoriesCopy();
				return categories.FirstOrDefault(category => category.Id == categoryId);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to get category: " + categoryId);
				return null;
			}
		}

		public TrackCategory GetDefaultCategory()
		{
			try
			{
				var categories = GetCategoriesCopy();
				return categories.First(category => category.IsDefault);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to get default category");
				return null;
			}
		}

		public string[] GetCategoryNames()
		{
			try
			{
				var categories = GetCategoriesCopy();
				return categories.Select(category => category.Name).ToArray();
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to get category names");
				return null;
			}
		}

		public void MoveDownList(int id)
		{
			try
			{
				var categories = GetCategoriesCopy();
				var category = categories.FirstOrDefault(trackCategory => trackCategory.Id == id);
				if (category == null) return;
				var categoryIndex = categories.IndexOf(category);
				if (categoryIndex == -1) return;
				var tradeCategoryIndex = categoryIndex + 1;
				if (tradeCategoryIndex >= categories.Count || tradeCategoryIndex == -1) return;
				var tradeCategory = categories[tradeCategoryIndex];
				categories[tradeCategoryIndex] = category;
				categories[categoryIndex] = tradeCategory;
				UpdateCategories(categories);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed move category down list for categoryId " + id);
			}
		}

		public void MoveUpList(int id)
		{
			try
			{
				var categories = GetCategoriesCopy();
				var category = categories.FirstOrDefault(trackCategory => trackCategory.Id == id);
				if (category == null) return;
				var categoryIndex = categories.IndexOf(category);
				if (categoryIndex == -1) return;
				var tradeCategoryIndex = categoryIndex - 1;
				if (tradeCategoryIndex >= categories.Count || tradeCategoryIndex == -1) return;
				var tradeCategory = categories[tradeCategoryIndex];
				categories[tradeCategoryIndex] = category;
				categories[categoryIndex] = tradeCategory;
				UpdateCategories(categories);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed move category up list for categoryId " + id);
			}
		}

		public void AddCategory()
		{
			var categories = GetCategoriesCopy();
			categories.Insert(0, new TrackCategory
			{
				Id = categories.Max(category => category.Id) + 1,
				Name = "New Category",
				Color = UIColor.White
			});
			UpdateCategories(categories);
		}

		public void DeleteCategory(int id)
		{
			try
			{
				var categories = GetCategoriesCopy();
				var category = categories.FirstOrDefault(trackCategory => trackCategory.Id == id);
				if (category == null) return;
				if (category.IsDefault) return;
				var categoryIndex = categories.IndexOf(category);
				if (categoryIndex == -1) return;
				categories.RemoveAt(categoryIndex);
				UpdateCategories(categories);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to delete categoryId " + id);
			}
		}

		public void ResetCategories()
		{
			var defaultCategories = GetDefaultCategories();
			UpdateCategories(defaultCategories);
			_plugin.SaveConfig();
		}

		public List<TrackCategory> GetCategoriesCopy()
		{
			lock (_categoryLock)
			{
				return _categories.Select(category => category.Copy()).ToList();
			}
		}

		public int GetCategoryId(int categoryIndex)
		{
			try
			{
				return _categories[categoryIndex].Id;
			}
			catch
			{
				return 0;
			}
		}

		public int GetCategoryIndex(int categoryId)
		{
			try
			{
				var categoryName = _categories.FirstOrDefault(category => category.Id == categoryId)?.Name;
				return categoryName == null ? 0 : GetCategoryIndex(categoryName);
			}
			catch
			{
				return 0;
			}
		}

		public int GetCategoryIndex(string categoryName)
		{
			try
			{
				var categoryIndex = Array.IndexOf(GetCategoryNames(), categoryName);
				if (categoryIndex == -1)
					for (var i = 0; i < _categories.Count; i++)
						if (_categories[i].IsDefault)
							return i;
				return categoryIndex;
			}
			catch
			{
				return 0;
			}
		}

		public void BuildCategoryLists()
		{
			var categories = GetCategoriesCopy();
			CategoryPriorities = categories.Select((t, i) => new KeyValuePair<int, int>(t.Id, i + 1)).ToList();
			CategoryIds = categories.Select(category => category.Id).ToList();
		}

		private void UpdateCategories(List<TrackCategory> categories)
		{
			lock (_categoryLock)
			{
				_categories = categories;
			}

			SaveCategories();
			BuildCategoryLists();
			CategoriesUpdated?.Invoke(this, true);
		}

		public int GetDefaultIcon()
		{
			var defaultIcon = GetDefaultCategory().Icon;
			return defaultIcon == 0 ? FontAwesomeIcon.User.ToIconChar() : defaultIcon;
		}

		public event EventHandler<bool> CategoriesUpdated;

		public void Dispose()
		{
			SaveCategories();
		}

		public void UpdateCategory(TrackCategory category)
		{
			if (category == null) return;
			var categories = GetCategoriesCopy();
			var currentCategory = categories.FirstOrDefault(trackCategory => category.Id == trackCategory.Id);
			if (currentCategory == null) return;
			currentCategory.Name = category.Name;
			currentCategory.IsDefault = category.IsDefault;
			currentCategory.Icon = category.Icon;
			currentCategory.Color = category.Color;
			currentCategory.EnableAlerts = category.EnableAlerts;
			_categories = categories;
			UpdateCategories(categories);
		}

		private void SaveCategories()
		{
			try
			{
				var categories = GetCategoriesCopy();
				var data = JsonConvert.SerializeObject(categories, _jsonSerializerSettings);
				_plugin.DataManager.SaveDataStr("categories.dat", data);
			}
			catch (Exception ex)
			{
				_plugin.LogError(ex, "Failed to save player data - will try again soon.");
			}
		}

		private void InitCategories()
		{
			try
			{
				_plugin.DataManager.InitDataFiles(new[] {"categories.dat"});
			}
			catch
			{
				_plugin.LogInfo("Failed to properly initialize but probably will be fine.");
			}
		}

		private void LoadCategories()
		{
			try
			{
				var data = _plugin.DataManager.ReadDataStr("categories.dat");
				var categories = JsonConvert.DeserializeObject<List<TrackCategory>>(data, _jsonSerializerSettings);
				if (categories == null || categories.Count == 0) categories = GetDefaultCategories();
				_categories = categories;
			}
			catch
			{
				_plugin.LogInfo("Can't load category data so starting fresh.");
				_categories = GetDefaultCategories();
			}
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
	}
}