using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dalamud.DrunkenToad.Core.Models;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;

public class CategoryService
{
    private PlayerFilter playerCategoryFilter = new();
    private List<string> categoryNames = new();
    private List<string> categoryNamesWithBlank = new() { string.Empty };
    private List<string> categoryNamesExclDynamic = new() { string.Empty };
    private List<string> categoryNamesExclDynamicWithBlank  = new() { string.Empty };
    private Dictionary<int, Category> categories = new();
    private readonly ReaderWriterLockSlim setLock = new(LockRecursionPolicy.SupportsRecursion);

    public CategoryService() => this.ReloadCategoryCache();

    public static int GetDefaultCategory(ToadLocation loc)
    {
        DalamudContext.PluginLog.Verbose($"Entering CategoryService.GetDefaultCategory(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.DefaultCategoryId != 0 ? config.DefaultCategoryId : 0;
    }

    public Category? GetCategory(int id)
    {
        setLock.EnterReadLock();
        try
        {
            return this.categories.TryGetValue(id, out var category) ? category : null;
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }
    
    public Category? GetSyncedCategory(int socialListId)
    {
        setLock.EnterReadLock();
        try
        {
            return this.categories.Values.FirstOrDefault(cat => cat.SocialListId == socialListId);
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    public List<Category> GetCategories(bool includeDynamic = true)
    {
        setLock.EnterReadLock();
        try
        {
            var categoriesList = this.categories.Values;
            return includeDynamic ? categoriesList.OrderBy(cat => cat.Rank).ToList() : 
                categoriesList.Where(cat => cat.SocialListId == 0).OrderBy(cat => cat.Rank).ToList();
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    public Dictionary<int, int> GetCategoryRanks() => this.GetCategories().ToDictionary(cat => cat.Id, cat => cat.Rank);

    public PlayerFilter GetCategoryFilters() => this.playerCategoryFilter;

    public void CreateCategory(string name, int socialListId = 0)
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose($"Entering CategoryService.CreateCategory(): {name}");
            var rank = 1;
            var categoriesList = this.GetCategories();

            if (categories.Count > 0)
            {
                var maxCategory = categoriesList.Aggregate((max, cat) => cat.Id > max.Id || cat.Rank > max.Rank ? cat : max);
                rank += maxCategory.Rank;
            }

            var category = new Category
            {
                Rank = rank,
                Name = name,
                SocialListId = socialListId,
            };

            this.AddCategoryToCacheAndRepository(category);
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    public void UpdateCategory(Category category)
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose($"Entering CategoryService.UpdateCategory(): {category.Name}");
            this.UpdateCategoryInCacheAndRepository(category);
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    public void DeleteCategory(Category category)
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose($"Entering CategoryService.DeleteCategory(): {category.Name}");
            var categoriesList = this.GetCategories();

            var filteredCategories = categoriesList.Where(cat => cat.Rank > category.Rank).ToList();
            foreach (var cat in filteredCategories)
            {
                cat.Rank -= 1;
                this.UpdateCategory(cat);
            }

            this.DeleteCategoryFromCacheAndRepository(category);
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    public List<string> GetCategoryNames(bool includeBlank = true, bool includeDynamic = true)
    {
        setLock.EnterReadLock();
        try
        {
            if (includeDynamic)
            {
                return includeBlank ? this.categoryNamesWithBlank : this.categoryNames;
            }
            return includeBlank ? this.categoryNamesExclDynamicWithBlank : this.categoryNamesExclDynamic;
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }
    
    public void DecreaseCategoryRank(int id) => this.SwapCategoryRanks(id, 1);

    public void IncreaseCategoryRank(int id) => this.SwapCategoryRanks(id, -1);

    public bool IsMaxRankCategory(Category category)
    {
        try
        {
            var categoriesList = this.GetCategories();
            return category.Rank == categoriesList.Max(cat => cat.Rank);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public bool IsMinRankCategory(Category category)
    {
        try
        {
            var categoriesList = this.GetCategories();
            return category.Rank == categoriesList.Min(cat => cat.Rank);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void RefreshCategories()
    {
        DalamudContext.PluginLog.Verbose("CategoryService.RefreshCategories()");
        this.ReloadCategoryCache();
    }

    public Category? GetCategoryByName(string name)
    {
        setLock.EnterReadLock();
        try
        {
            return this.categories.Values.FirstOrDefault(category => 
                category.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    public Category? GetCategoryById(int categoryId)
    {
        setLock.EnterReadLock();
        try
        {
            this.categories.TryGetValue(categoryId, out var category);
            return category;
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    private static void PopulateDerivedFields(Category category)
    {
        if (category.Id == 0)
        {
            category.PlayerConfig = new PlayerConfig(PlayerConfigType.Category);
            return;
        }

        category.PlayerConfig = RepositoryContext.PlayerConfigRepository.GetPlayerConfigByCategoryId(category.Id) ?? new PlayerConfig(PlayerConfigType.Category);
    }

    private void BuildCategoryNames()
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose("Entering CategoryService.BuildCategoryNames()");
            var categoriesList = this.GetCategories();
            this.categoryNames = categoriesList.Select(cat => cat.Name).ToList();
            this.categoryNamesWithBlank = new List<string> { string.Empty }.Concat(this.categoryNames).ToList();
            this.categoryNamesExclDynamic = categoriesList.Where(cat => cat.SocialListId == 0).Select(cat => cat.Name).ToList();
            this.categoryNamesExclDynamicWithBlank = new List<string> { string.Empty }.Concat(this.categoryNamesExclDynamic).ToList();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void BuildCategoryFilters()
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose("Entering CategoryService.BuildCategoryFilters()");
            var categoriesByRank = this.GetCategories();
            var totalCategories = categoriesByRank.Count;

            var categoryFilterIds = categoriesByRank.Select(category => category.Id).ToList();
            var categoryFilterNames = categoriesByRank.Select(category => category.Name).ToList();

            categoryFilterIds.Insert(0, 0);
            categoryFilterNames.Insert(0, string.Empty);

            this.playerCategoryFilter = new PlayerFilter
            {
                FilterIds = categoryFilterIds,
                FilterNames = categoryFilterNames,
                TotalFilters = totalCategories,
            };
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void SwapCategoryRanks(int id, int rankOffset)
    {
        setLock.EnterWriteLock();
        try
        {
            var categoriesList = this.GetCategories();

            var currentCategory = categoriesList.FirstOrDefault(cat => cat.Id == id);
            if (currentCategory == null)
            {
                return;
            }

            var swapCategory = categoriesList.FirstOrDefault(cat => cat.Rank == currentCategory.Rank + rankOffset);
            if (swapCategory == null)
            {
                return;
            }

            currentCategory.Rank += rankOffset;
            swapCategory.Rank -= rankOffset;

            this.UpdateCategory(currentCategory);
            this.UpdateCategory(swapCategory);
            ServiceContext.PlayerDataService.RecalculatePlayerRankings();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void ReloadCategoryCache()
    {
        setLock.EnterWriteLock();
        try
        {
            var categoriesList = RepositoryContext.CategoryRepository.GetAllCategories()?.ToList() ?? new List<Category>();
            foreach (var category in categoriesList)
            {
                PopulateDerivedFields(category);
            }

            this.categories = categoriesList.ToDictionary(category => category.Id, category => category);
            this.BuildCategoryFilters();
            this.BuildCategoryNames();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void UpdateCategoryInCacheAndRepository(Category category)
    {
        DalamudContext.PluginLog.Verbose($"Entering CategoryService.UpdateCategoryInCacheAndRepository(): {category.Name}");
        this.categories[category.Id] = category;
        RepositoryContext.CategoryRepository.UpdateCategory(category);
        this.BuildCategoryFilters();
        this.BuildCategoryNames();
        var players = ServiceContext.PlayerCacheService.GetCategoryPlayers(category.Id);
        foreach (var player in players)
        {
            ServiceContext.PlayerCacheService.PopulateDerivedFields(player);
            ServiceContext.VisibilityService.SyncWithVisibility(player);
        }
    }

    private void AddCategoryToCacheAndRepository(Category category)
    {
        category.Id = RepositoryContext.CategoryRepository.CreateCategory(category);
        category.PlayerConfig = new PlayerConfig(PlayerConfigType.Category);
        PopulateDerivedFields(category);
        this.categories.Add(category.Id, category);
        this.BuildCategoryFilters();
        this.BuildCategoryNames();
        ServiceContext.PlayerCacheService.AddCategory(category.Id);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    }

    private void DeleteCategoryFromCacheAndRepository(Category category)
    {
        PlayerCategoryService.DeletePlayerCategoryByCategoryId(category.Id);
        PlayerConfigService.DeletePlayerConfigByCategoryId(category.Id);
        RepositoryContext.CategoryRepository.DeleteCategory(category.Id);
        var config = ServiceContext.ConfigService.GetConfig();
        config.ClearCategoryIds(category.Id);
        ServiceContext.ConfigService.SaveConfig(config);
        this.categories.Remove(category.Id);
        ServiceContext.CategoryService.RefreshCategories();
        ServiceContext.PlayerDataService.ClearCategoryFromPlayers(category.Id);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
        SocialListService.ClearCategoryFromSocialLists(category.Id);
    }
}
