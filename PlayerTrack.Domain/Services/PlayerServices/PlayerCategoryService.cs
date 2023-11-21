using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Threading.Tasks;
using Dalamud.DrunkenToad.Core;

public class PlayerCategoryService
{
    public static void AssignCategoryToPlayers(IEnumerable<Player> playerIds, int syncedCategoryId)
    {
        var enumerable = playerIds.ToList();
        DalamudContext.PluginLog.Verbose($"Entering PlayerCategoryService.AssignCategoryToPlayers(): {enumerable.Count}, {syncedCategoryId}");
        foreach (var playerId in enumerable)
        {
            AssignCategoryToPlayerSync(playerId.Id, syncedCategoryId);
        }
    }
    
    public static void AssignCategoryToPlayer(int playerId, int categoryId) => Task.Run(() =>
    {
        AssignCategoryToPlayerSync(playerId, categoryId);
    });

    public static void AssignCategoryToPlayerSync(int playerId, int categoryId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerCategoryService.AssignCategoryToPlayer(): {playerId}, {categoryId}");
        var category = ServiceContext.CategoryService.GetCategoryById(categoryId);
        if (category == null)
        {
            DalamudContext.PluginLog.Warning($"Category not found, categoryId: {categoryId}");
            return;
        }

        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning($"Player not found, playerId: {playerId}");
            return;
        }

        var assignedCategories = player.AssignedCategories;
        if (assignedCategories.Any(c => c.Id == categoryId))
        {
            DalamudContext.PluginLog.Warning($"Category already assigned to player, playerId: {playerId}, categoryId: {categoryId}");
            return;
        }

        assignedCategories.Add(category);
        var categoryRanks = ServiceContext.CategoryService.GetCategoryRanks();
        SetPrimaryCategoryId(player, categoryRanks);
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        RepositoryContext.PlayerCategoryRepository.CreatePlayerCategory(playerId, categoryId);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    }

    public static void UnassignCategoriesFromPlayer(int playerId) => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerCategoryService.UnassignCategoriesFromPlayer(): {playerId}");
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning($"Player not found, playerId: {playerId}");
            return;
        }

        var assignedCategories = player.AssignedCategories;
        if (!assignedCategories.Any())
        {
            DalamudContext.PluginLog.Warning($"No categories assigned to player, playerId: {playerId}");
            return;
        }

        player.AssignedCategories = new List<Category>();
        player.PrimaryCategoryId = 0;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        RepositoryContext.PlayerCategoryRepository.DeletePlayerCategoryByPlayerId(playerId);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    });

    public static void UnassignCategoryFromPlayer(int playerId, int categoryId) => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerCategoryService.UnassignCategoryFromPlayer(): {playerId}, {categoryId}");
        var category = ServiceContext.CategoryService.GetCategoryById(categoryId);
        if (category == null)
        {
            DalamudContext.PluginLog.Warning($"Category not found, categoryId: {categoryId}");
            return;
        }

        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            DalamudContext.PluginLog.Warning($"Player not found, playerId: {playerId}");
            return;
        }

        var assignedCategories = player.AssignedCategories;
        if (assignedCategories.All(c => c.Id != categoryId))
        {
            DalamudContext.PluginLog.Warning($"Category not assigned to player, playerId: {playerId}, categoryId: {categoryId}");
            return;
        }

        assignedCategories.RemoveAt(assignedCategories.FindIndex(c => c.Id == categoryId));
        var categoryRanks = ServiceContext.CategoryService.GetCategoryRanks();
        SetPrimaryCategoryId(player, categoryRanks);
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        RepositoryContext.PlayerCategoryRepository.DeletePlayerCategory(playerId, categoryId);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    });

    public static void DeletePlayerCategoryByCategoryId(int categoryId) => RepositoryContext.PlayerCategoryRepository.DeletePlayerCategoryByCategoryId(categoryId);

    public static void DeletePlayerCategoryByPlayerId(int playerId) => RepositoryContext.PlayerCategoryRepository.DeletePlayerCategoryByPlayerId(playerId);

    public static void SetPrimaryCategoryId(Player player, Dictionary<int, int> categoryRanks) => player.PrimaryCategoryId = GetPrimaryCategoryId(player, categoryRanks);

    private static int GetPrimaryCategoryId(Player player, IReadOnlyDictionary<int, int> categoryRanks)
    {
        var primaryCategoryId = 0;
        var highestRank = int.MaxValue;

        foreach (var category in player.AssignedCategories)
        {
            if (!categoryRanks.TryGetValue(category.Id, out var rank)) continue;
            if (rank >= highestRank) continue;
            highestRank = rank;
            primaryCategoryId = category.Id;
        }

        return primaryCategoryId;
    }
}
