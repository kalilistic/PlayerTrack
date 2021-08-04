using System;
using System.Collections.Generic;
using System.Linq;

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
            : base(plugin.PluginService)
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
                    this.categories.Add(category.Id, category);
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
        /// Get default category.
        /// </summary>
        /// <returns>default category.</returns>
        public Category GetDefaultCategory()
        {
            lock (this.locker)
            {
                return this.categories.First(pair => pair.Value.IsDefault).Value;
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
                return this.categories.OrderBy(cat1 => cat1.Value.Id).Select(cat2 => cat2.Value.Name).ToArray();
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
                if (currentCategory == null) return;
                var swapCategory = this.categories.FirstOrDefault(pair => pair.Value.Rank == currentCategory.Rank + 1).Value;
                currentCategory.Rank += 1;
                swapCategory.Rank -= 1;
                this.UpdateItem(currentCategory);
                this.UpdateItem(swapCategory);
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
                if (currentCategory == null) return;
                var swapCategory = this.categories.FirstOrDefault(pair => pair.Value.Rank == currentCategory.Rank - 1).Value;
                currentCategory.Rank -= 1;
                swapCategory.Rank += 1;
                this.UpdateItem(currentCategory);
                this.UpdateItem(swapCategory);
            }
        }

        /// <summary>
        /// Add category.
        /// </summary>
        public void AddCategory()
        {
            var id = this.NextID();
            var rank = this.MaxRank() + 1;
            var newCategory = new Category(id)
            {
                Rank = rank,
            };
            lock (this.locker)
            {
                this.categories.Add(id, newCategory);
            }

            this.InsertItem(newCategory);
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
        }

        /// <summary>
        /// Save category.
        /// </summary>
        /// <param name="category">category to save.</param>
        public void SaveCategory(Category category)
        {
            this.categories[category.Id] = category;
            this.UpdateItem(category);
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
                cats.Add(0, defaultCategory);
                foreach (var category in this.categories)
                {
                    this.DeleteItem<Category>(category.Key);
                }

                this.InsertItem(defaultCategory);
                this.categories = cats;
            }
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
                return this.categories.Max(category => category.Value.Id) + 1;
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
