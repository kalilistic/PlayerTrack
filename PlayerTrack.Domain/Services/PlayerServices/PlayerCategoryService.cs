using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Threading.Tasks;
using Dalamud.Logging;

public class PlayerCategoryService
{
    public static void AssignCategoryToPlayer(int playerId, int categoryId) => Task.Run(() =>
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryService.AssignCategoryToPlayer(): {playerId}, {categoryId}");
        var category = ServiceContext.CategoryService.GetCategoryById(categoryId);
        if (category == null)
        {
            PluginLog.LogWarning($"Category not found, categoryId: {categoryId}");
            return;
        }

        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            PluginLog.LogWarning($"Player not found, playerId: {playerId}");
            return;
        }

        var assignedCategories = player.AssignedCategories;
        if (assignedCategories.Any(c => c.Id == categoryId))
        {
            PluginLog.LogWarning($"Category already assigned to player, playerId: {playerId}, categoryId: {categoryId}");
            return;
        }

        assignedCategories.Add(category);
        var categoryRanks = ServiceContext.CategoryService.GetCategoryRanks();
        SetPrimaryCategoryId(player, categoryRanks);
        ServiceContext.PlayerDataService.UpdatePlayer(player);
        RepositoryContext.PlayerCategoryRepository.CreatePlayerCategory(playerId, categoryId);
        ServiceContext.PlayerDataService.RecalculatePlayerRankings();
    });

    public static void UnassignCategoryFromPlayer(int playerId, int categoryId) => Task.Run(() =>
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryService.UnassignCategoryFromPlayer(): {playerId}, {categoryId}");
        var category = ServiceContext.CategoryService.GetCategoryById(categoryId);
        if (category == null)
        {
            PluginLog.LogWarning($"Category not found, categoryId: {categoryId}");
            return;
        }

        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            PluginLog.LogWarning($"Player not found, playerId: {playerId}");
            return;
        }

        var assignedCategories = player.AssignedCategories;
        if (assignedCategories.All(c => c.Id != categoryId))
        {
            PluginLog.LogWarning($"Category not assigned to player, playerId: {playerId}, categoryId: {categoryId}");
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
            if (categoryRanks.TryGetValue(category.Id, out var rank))
            {
                if (rank < highestRank)
                {
                    highestRank = rank;
                    primaryCategoryId = category.Id;
                }
            }
        }

        return primaryCategoryId;
    }
}
