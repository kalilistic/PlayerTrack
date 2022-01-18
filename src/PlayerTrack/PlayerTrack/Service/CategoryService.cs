using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <summary>
    /// Manage categories.
    /// </summary>
    public class CategoryService : BaseRepository
    {
        private readonly object locker = new ();
        private readonly PlayerTrackPlugin plugin;
        private SortedList<int, Category> categories = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryService"/> class.
        /// </summary>
        /// <param name="plugin">base plugin.</param>
        public CategoryService(PlayerTrackPlugin plugin)
            : base(PlayerTrackPlugin.GetPluginFolder())
        {
            this.plugin = plugin;
            this.LoadCategories();
        }

        /// <summary>
        /// Load categories.
        /// </summary>
        public void LoadCategories()
        {
            lock (this.locker)
            {
                var trackCategories = this.GetItems<Category>().ToList();
                foreach (var category in trackCategories)
                {
                    category.SetSeName();
                    this.categories.Add(category.Id, category);
                }

                // remove extra default categories
                var defaultCategories = trackCategories.Where(category => category.IsDefault).ToList();
                if (defaultCategories.Count > 1)
                {
                    var updateCategories = defaultCategories.Skip(1).ToList();
                    foreach (var category in updateCategories)
                    {
                        category.IsDefault = false;
                        this.SaveCategory(category);
                    }
                }
            }
        }

        /// <summary>
        /// Get category by Id.
        /// </summary>
        /// <param name="categoryId">category id to lookup.</param>
        /// <returns>category or default category if not found.</returns>
        public Category GetCategory(int categoryId)
        {
            try
            {
                lock (this.locker)
                {
                    return this.categories.FirstOrDefault(pair => pair.Value.Id == categoryId).Value;
                }
            }
            catch (Exception)
            {
                return this.GetDefaultCategory();
            }
        }

        /// <summary>
        /// Get categories.
        /// </summary>
        /// <returns>categories.</returns>
        public KeyValuePair<int, Category>[] GetCategories()
        {
            lock (this.locker)
            {
                return this.categories.OrderBy(pair => pair.Value.Rank).ToArray();
            }
        }

        /// <summary>
        /// Get category IDs by visibility type.
        /// </summary>
        /// <param name="visibilityType">visibility type to filter by.</param>
        /// <returns>categories with visibility type.</returns>
        public int[] GetCategoryIdsByVisibilityType(VisibilityType visibilityType)
        {
            lock (this.locker)
            {
                return this.categories.Where(pair => pair.Value.VisibilityType == visibilityType).Select(pair => pair.Key).ToArray();
            }
        }

        /// <summary>
        /// Get category IDs by OverrideFCNameColor state.
        /// </summary>
        /// <param name="isOverriden">isOverriden by FCNameColor.</param>
        /// <returns>categories with visibility type.</returns>
        public int[] GetCategoryIdsByOverrideFCNameColor(bool isOverriden)
        {
            lock (this.locker)
            {
                return this.categories.Where(pair => pair.Value.OverrideFCNameColor == isOverriden).Select(pair => pair.Key).ToArray();
            }
        }

        /// <summary>
        /// Get category ID by lodestone id.
        /// </summary>
        /// <param name="lodestoneId">lodestone id to filter by.</param>
        /// <returns>category with matching lodestone id.</returns>
        public int? GetCategoryIdByFCLodestoneId(string lodestoneId)
        {
            lock (this.locker)
            {
                try
                {
                    lock (this.locker)
                    {
                        return this.categories.FirstOrDefault(pair => pair.Value.FCLodestoneId == lodestoneId).Key;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get default category.
        /// </summary>
        /// <returns>default category.</returns>
        public Category GetDefaultCategory()
        {
            lock (this.locker)
            {
                try
                {
                    return this.categories.First(pair => pair.Value.IsDefault).Value;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to find default category so making one");
                    var defaultCategory = new Category(this.NextID()) { Name = "Default", IsDefault = true };
                    defaultCategory.SetSeName();
                    this.AddCategory(defaultCategory);
                    return defaultCategory;
                }
            }
        }

        /// <summary>
        /// Get category names.
        /// </summary>
        /// <returns>string array with category names.</returns>
        public IEnumerable<string> GetCategoryNames()
        {
            lock (this.locker)
            {
                return this.categories.OrderBy(pair => pair.Value.Rank).Select(cat2 => cat2.Value.Name).ToArray();
            }
        }

        /// <summary>
        /// Get category ids.
        /// </summary>
        /// <returns>string array with category ids.</returns>
        public IEnumerable<int> GetCategoryIds()
        {
            lock (this.locker)
            {
                return this.categories.OrderBy(pair => pair.Value.Rank).Select(cat2 => cat2.Value.Id).ToArray();
            }
        }

        /// <summary>
        /// Decrease category rank to show lower in list.
        /// </summary>
        /// <param name="categoryId">category id for category to decrease in rank..</param>
        public void DecreaseCategoryRank(int categoryId)
        {
            lock (this.locker)
            {
                var currentCategory = this.categories[categoryId];
                var swapCategory = this.categories.FirstOrDefault(pair => pair.Value.Rank == currentCategory.Rank + 1).Value;
                currentCategory.Rank += 1;
                swapCategory.Rank -= 1;
                this.UpdateItem(currentCategory);
                this.UpdateItem(swapCategory);
                this.plugin.PlayerService.SetDerivedFieldsForAllPlayers();
            }
        }

        /// <summary>
        /// Increase category rank to show higher in list.
        /// </summary>
        /// <param name="categoryId">category id for category to increase in rank..</param>
        public void IncreaseCategoryRank(int categoryId)
        {
            lock (this.locker)
            {
                var currentCategory = this.categories[categoryId];
                var swapCategory = this.categories.FirstOrDefault(pair => pair.Value.Rank == currentCategory.Rank - 1).Value;
                currentCategory.Rank -= 1;
                swapCategory.Rank += 1;
                this.UpdateItem(currentCategory);
                this.UpdateItem(swapCategory);
                this.plugin.PlayerService.SetDerivedFieldsForAllPlayers();
            }
        }

        /// <summary>
        /// Add category.
        /// </summary>
        /// <returns>category id.</returns>
        public int AddCategory()
        {
            var id = this.NextID();
            var rank = this.MaxRank() + 1;
            var newCategory = new Category(id)
            {
                Rank = rank,
            };
            newCategory.SetSeName();
            lock (this.locker)
            {
                this.categories.Add(id, newCategory);
            }

            this.InsertItem(newCategory);

            return id;
        }

        /// <summary>
        /// Add category.
        /// </summary>
        /// <param name="category">category to add.</param>
        public void AddCategory(Category category)
        {
            lock (this.locker)
            {
                if (!this.categories.ContainsKey(category.Id))
                {
                    category.SetSeName();
                    this.categories.Add(category.Id, category);
                    this.InsertItem(category);
                }
            }
        }

        /// <summary>
        /// Delete category.
        /// </summary>
        /// <param name="categoryId">category id for category to delete.</param>
        public void DeleteCategory(int categoryId)
        {
            var deletedCategory = this.GetCategory(categoryId);

            lock (this.locker)
            {
                this.categories.Remove(categoryId);
                foreach (var category in this.categories)
                {
                    if (category.Value.Rank > deletedCategory.Rank)
                    {
                        category.Value.Rank -= 1;
                        this.UpdateItem(category.Value);
                    }
                }
            }

            this.plugin.PlayerService.RemoveDeletedCategory(categoryId, this.GetDefaultCategory().Id);
            this.DeleteItem<Category>(deletedCategory.Id);
            this.plugin.PlayerService.SetDerivedFieldsForAllPlayers();
            this.plugin.VisibilityService.SyncWithVisibility();
        }

        /// <summary>
        /// Save category.
        /// </summary>
        /// <param name="category">category to save.</param>
        public void SaveCategory(Category category)
        {
            category.SetSeName();
            this.categories[category.Id] = category;
            this.UpdateItem(category);

            // ReSharper disable once ConstantConditionalAccessQualifier
            this.plugin.NamePlateManager?.ForceRedraw();
        }

        /// <summary>
        /// Reset categories to only default.
        /// </summary>
        public void ResetCategories()
        {
            lock (this.locker)
            {
                var cats = new SortedList<int, Category>();
                var defaultCategory = new Category(1) { Name = "Default", IsDefault = true };
                defaultCategory.SetSeName();
                cats.Add(0, defaultCategory);
                foreach (var category in this.categories)
                {
                    this.DeleteItem<Category>(category.Key);
                }

                this.InsertItem(defaultCategory);
                this.categories = cats;
            }

            this.plugin.NamePlateManager.ForceRedraw();
            this.plugin.VisibilityService.SyncWithVisibility();
            this.plugin.FCNameColorService.SyncWithFCNameColor();
        }

        /// <summary>
        /// Get max rank of categories.
        /// </summary>
        /// <returns>max rank.</returns>
        public int MaxRank()
        {
            lock (this.locker)
            {
                return this.categories.Max(category => category.Value.Rank);
            }
        }

        /// <summary>
        /// Get next unique ID for new category.
        /// </summary>
        /// <returns>next unique id.</returns>
        public int NextID()
        {
            lock (this.locker)
            {
                try
                {
                    return this.categories.Max(category => category.Value.Id) + 1;
                }
                catch (Exception err)
                {
                    Logger.LogError(err, "Failed to get next ID so using 1.");
                    return 1;
                }
            }
        }

        /// <summary>
        /// Get categories by rank.
        /// </summary>
        /// <returns>sorted categories.</returns>
        public IEnumerable<KeyValuePair<int, Category>> GetSortedCategories()
        {
            return this.categories.ToArray().OrderBy(pair => pair.Value.Rank);
        }
    }
}
