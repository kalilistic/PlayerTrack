using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Domain.Common;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using System.Threading.Tasks;
using PlayerTrack.Data;

namespace PlayerTrack.Domain;

public class CategoryService : CacheService<Category>
{
    private PlayerFilter PlayerCategoryFilter = new();
    private List<string> CategoryNames = [];
    private List<string> CategoryNamesWithBlank = [string.Empty];
    private List<string> CategoryNamesExclDynamic = [string.Empty];
    private List<string> CategoryNamesExclDynamicWithBlank  = [string.Empty];

    public CategoryService()
    {
        ReloadCategoryCache();
    }

    public static int GetDefaultCategory(LocationData loc)
    {
        Plugin.PluginLog.Verbose($"Entering CategoryService.GetDefaultCategory(): {loc.LocationType}");
        var config = ServiceContext.ConfigService.GetConfig().GetTrackingLocationConfig(loc.LocationType);
        return config.DefaultCategoryId != 0 ? config.DefaultCategoryId : 0;
    }

    public Category? GetCategory(int id) =>
        Cache.Values.FirstOrDefault(cat => cat.Id == id);

    public Category? GetSyncedCategory(int socialListId) =>
        Cache.Values.FirstOrDefault(cat => cat.SocialListId == socialListId);

    public List<Category> GetCategories(bool includeDynamic = true)
    {
        return includeDynamic
                   ? Cache.Values.OrderBy(cat => cat.Rank).ToList()
                   : Cache.Values.Where(cat => cat.SocialListId == 0).OrderBy(cat => cat.Rank).ToList();
    }

    public Dictionary<int, int> GetCategoryRanks() =>
        GetCategories().ToDictionary(cat => cat.Id, cat => cat.Rank);

    public PlayerFilter GetCategoryFilters() =>
        PlayerCategoryFilter;

    public void CreateCategory(string name, int socialListId = 0)
    {
        Plugin.PluginLog.Verbose($"Entering CategoryService.CreateCategory(): {name}");

        var rank = 1;
        var categories = GetCategories();
        if (categories.Count > 0)
        {
            var maxCategory = categories.Aggregate((max, cat) => cat.Id > max.Id || cat.Rank > max.Rank ? cat : max);
            rank += maxCategory.Rank;
        }

        var category = new Category
        {
            Rank = rank,
            Name = name,
            SocialListId = socialListId,
        };

        AddCategoryToCacheAndRepository(category);
    }

    public void UpdateCategory(Category category)
    {
        Plugin.PluginLog.Verbose($"Entering CategoryService.UpdateCategory(): {category.Name}");
        UpdateCategoryInCacheAndRepository(category);
    }

    public void DeleteCategory(Category category) => Task.Run(() =>
    {
        Plugin.PluginLog.Verbose($"Entering CategoryService.DeleteCategory(): {category.Name}");
        var categories = GetCategories();

        var filteredCategories = categories.Where(cat => cat.Rank > category.Rank).ToList();
        foreach (var cat in filteredCategories)
        {
            cat.Rank -= 1;
            UpdateCategory(cat);
        }

        DeleteCategoryFromCacheAndRepository(category);
    });

    public List<string> GetCategoryNames(bool includeBlank = true, bool includeDynamic = true)
    {
        if (includeDynamic)
            return includeBlank ? CategoryNamesWithBlank : CategoryNames;

        return includeBlank ? CategoryNamesExclDynamicWithBlank : CategoryNamesExclDynamic;
    }

    public void DecreaseCategoryRank(int id) =>
        SwapCategoryRanks(id, 1);

    public void IncreaseCategoryRank(int id) =>
        SwapCategoryRanks(id, -1);

    public bool IsMaxRankCategory(Category category)
    {
        try
        {
            var categories = GetCategories();
            return category.Rank == categories.Max(cat => cat.Rank);
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
            var categories = GetCategories();
            return category.Rank == categories.Min(cat => cat.Rank);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public void RefreshCategories()
    {
        Plugin.PluginLog.Verbose("CategoryService.RefreshCategories()");
        ReloadCategoryCache();
    }

    public Category? GetCategoryByName(string name) =>
        Cache.Values.FirstOrDefault(category => category.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? null;

    public Category? GetCategoryById(int categoryId)
    {
        Cache.TryGetValue(categoryId, out var value);
        return value;
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
        Plugin.PluginLog.Verbose("Entering CategoryService.BuildCategoryNames()");
        var categories = GetCategories();
        CategoryNames = categories.Select(cat => cat.Name).ToList();
        CategoryNamesWithBlank = new List<string> { string.Empty }.Concat(CategoryNames).ToList();
        CategoryNamesExclDynamic = categories.Where(cat => cat.SocialListId == 0).Select(cat => cat.Name).ToList();
        CategoryNamesExclDynamicWithBlank = new List<string> { string.Empty }.Concat(CategoryNamesExclDynamic).ToList();
    }

    private void BuildCategoryFilters()
    {
        Plugin.PluginLog.Verbose("Entering CategoryService.BuildCategoryFilters()");
        var categoriesByRank = GetCategories();
        var totalCategories = categoriesByRank.Count;

        var categoryFilterIds = categoriesByRank.Select(category => category.Id).Prepend(0).ToList();
        var categoryFilterNames = categoriesByRank.Select(category => category.Name).Prepend(string.Empty).ToList();

        PlayerCategoryFilter = new PlayerFilter
        {
            FilterIds = categoryFilterIds,
            FilterNames = categoryFilterNames,
            TotalFilters = totalCategories,
        };
    }

    private void SwapCategoryRanks(int id, int rankOffset) => Task.Run(() =>
    {
        var categories = GetCategories();

        var currentCategory = categories.FirstOrDefault(cat => cat.Id == id);
        if (currentCategory == null)
            return;

        var swapCategory = categories.FirstOrDefault(cat => cat.Rank == currentCategory.Rank + rankOffset);
        if (swapCategory == null)
            return;

        currentCategory.Rank += rankOffset;
        swapCategory.Rank -= rankOffset;

        UpdateCategory(currentCategory);
        UpdateCategory(swapCategory);

        var expectedRank = 1;
        foreach (var category in categories.OrderBy(cat => cat.Rank))
        {
            if (category.Rank != expectedRank)
            {
                category.Rank = expectedRank;
                UpdateCategory(category);
            }
            expectedRank++;
        }

        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    });

    private void ReloadCategoryCache() => ExecuteReloadCache(() =>
    {
        var categories = RepositoryContext.CategoryRepository.GetAllCategories();
        if (categories == null)
            Cache = new ConcurrentDictionary<int, Category>();

        var collection = new ConcurrentDictionary<int, Category>(categories!.ToDictionary(cat => cat.Id));

        foreach (var category in collection.Values)
            PopulateDerivedFields(category);

        Cache = collection;
        BuildCategoryFilters();
        BuildCategoryNames();
    });

    private void UpdateCategoryInCacheAndRepository(Category category)
    {
        Plugin.PluginLog.Verbose($"Entering CategoryService.UpdateCategoryInCacheAndRepository(): {category.Name}");

        if (Cache.TryGetValue(category.Id, out var existingValue))
            Cache.TryUpdate(category.Id, category, existingValue);

        RepositoryContext.CategoryRepository.UpdateCategory(category);
        BuildCategoryFilters();
        BuildCategoryNames();
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
        Cache.TryAdd(category.Id, category);
        BuildCategoryFilters();
        BuildCategoryNames();
        ServiceContext.PlayerCacheService.AddCategory(category.Id);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
        // ServiceContext.PlayerCacheService.LoadPlayers(); // disable for now due to lock contention
    }

    private void DeleteCategoryFromCacheAndRepository(Category category)
    {
        PlayerCategoryService.DeletePlayerCategoryByCategoryId(category.Id);
        PlayerConfigService.DeletePlayerConfigByCategoryId(category.Id);
        RepositoryContext.CategoryRepository.DeleteCategory(category.Id);
        var config = ServiceContext.ConfigService.GetConfig();
        config.ClearCategoryIds(category.Id);
        ServiceContext.ConfigService.SaveConfig(config);
        Cache.TryRemove(category.Id, out _);
        ServiceContext.CategoryService.RefreshCategories();
        ServiceContext.PlayerDataService.ClearCategoryFromPlayers(category.Id);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    }
}
