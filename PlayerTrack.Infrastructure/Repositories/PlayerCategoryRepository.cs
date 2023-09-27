using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dalamud.Logging;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerCategoryRepository : BaseRepository
{
    public PlayerCategoryRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public bool DeletePlayerCategoryByCategoryId(int categoryId)
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryRepository.DeletePlayerCategoryByCategoryId(): {categoryId}");
        try
        {
            const string sql = "DELETE FROM player_categories WHERE category_id = @category_id";
            this.Connection.Execute(sql, new { category_id = categoryId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete player categories by category id {categoryId}.");
            return false;
        }
    }

    public bool DeletePlayerCategory(int playerId, int categoryId)
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryRepository.DeletePlayerCategory(): {playerId}, {categoryId}");
        try
        {
            const string sql = "DELETE FROM player_categories WHERE player_id = @player_id AND category_id = @category_id";
            this.Connection.Execute(sql, new { player_id = playerId, category_id = categoryId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete player category by player id {playerId} and category id {categoryId}.");
            return false;
        }
    }

    public int CreatePlayerCategory(int playerId, int categoryId)
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryRepository.CreatePlayerCategory(): {playerId}, {categoryId}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var playerCategoryDto = new PlayerCategoryDTO { player_id = playerId, category_id = categoryId };
            SetCreateTimestamp(playerCategoryDto);

            const string insertSql = "INSERT INTO player_categories (player_id, category_id, created, updated) VALUES (@player_id, @category_id, @created, @updated)";
            this.Connection.Execute(insertSql, playerCategoryDto, transaction);

            var newId = this.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: transaction);

            transaction.Commit();
            return newId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            PluginLog.LogError(ex, $"Failed to create and retrieve player category for player id {playerId} and category id {categoryId}.");
            return 0;
        }
    }

    public bool DeletePlayerCategoryByPlayerId(int playerId)
    {
        try
        {
            const string sql = "DELETE FROM player_categories WHERE player_id = @player_id";
            this.Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete player category by player id {playerId}.");
            return false;
        }
    }

    public bool CreatePlayerCategories(IEnumerable<PlayerCategory> playerCategories)
    {
        PluginLog.LogVerbose($"Entering PlayerCategoryRepository.CreatePlayerCategories()");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var playerCategoryDTOs = playerCategories.Select(this.Mapper.Map<PlayerCategoryDTO>).ToList();
            const string sql = @"
                INSERT INTO player_categories (player_id, category_id, created, updated)
                VALUES (@player_id, @category_id, @created, @updated)";
            this.Connection.Execute(sql, playerCategoryDTOs, transaction);
            transaction.Commit();

            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to migrate players.");
            transaction.Rollback();
            return false;
        }
    }
}
