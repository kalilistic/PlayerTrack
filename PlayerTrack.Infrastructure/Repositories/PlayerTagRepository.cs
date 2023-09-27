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

public class PlayerTagRepository : BaseRepository
{
    public PlayerTagRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public bool DeletePlayerTag(int tagId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagRepository.DeletePlayerTag(): {tagId}");
        try
        {
            const string sql = @"
            DELETE FROM player_tags
            WHERE tag_id = @tag_id";
            this.Connection.Execute(sql, new { tag_id = tagId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete tags with TagID {tagId}.");
            return false;
        }
    }

    public bool DeletePlayerTag(int playerId, int tagId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagRepository.DeletePlayerTag(): {playerId}, {tagId}");
        try
        {
            const string sql = "DELETE FROM player_tags WHERE player_id = @player_id AND tag_id = @tag_id";
            this.Connection.Execute(sql, new { player_id = playerId, tag_id = tagId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete tag with PlayerID {playerId} and TagID {tagId}.");
            return false;
        }
    }

    public int CreatePlayerTag(int playerId, int tagId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagRepository.CreatePlayerTag(): {playerId}, {tagId}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var playerTagDto = new PlayerTagDTO { player_id = playerId, tag_id = tagId };
            SetCreateTimestamp(playerTagDto);

            const string sql = @"
            INSERT INTO player_tags (player_id, tag_id, created, updated)
            VALUES (@player_id, @tag_id, @created, @updated)";

            this.Connection.Execute(sql, playerTagDto, transaction);

            var newId = this.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: transaction);

            transaction.Commit();
            return newId;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to create new tag with PlayerID {playerId} and TagID {tagId}.");
            transaction.Rollback();
            return 0;
        }
    }

    public bool DeletePlayerTagByPlayerId(int playerId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagRepository.DeletePlayerTagByPlayerId(): {playerId}");
        try
        {
            const string sql = "DELETE FROM player_tags WHERE player_id = @player_id";
            this.Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete tag with PlayerID {playerId}.");
            return false;
        }
    }

    public bool CreatePlayerTags(List<PlayerTag> playerTags)
    {
        PluginLog.LogVerbose($"Entering PlayerTagRepository.CreatePlayerTags(): {playerTags.Count}.");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
            INSERT INTO player_tags (player_id, tag_id, created, updated)
            VALUES (@player_id, @tag_id, @created, @updated)";

            var playerTagDTOs = playerTags.Select(tag => new PlayerTagDTO
            {
                player_id = tag.PlayerId,
                tag_id = tag.TagId,
                created = tag.Created,
                updated = tag.Updated,
            }).ToList();

            this.Connection.Execute(sql, playerTagDTOs, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to create player tags.");
            transaction.Rollback();
            return false;
        }
    }
}
