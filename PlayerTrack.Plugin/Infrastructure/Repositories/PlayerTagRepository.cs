using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerTagRepository : BaseRepository
{
    public PlayerTagRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public bool DeletePlayerTag(int tagId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagRepository.DeletePlayerTag(): {tagId}");
        try
        {
            const string sql = @"
            DELETE FROM player_tags
            WHERE tag_id = @tag_id";
            Connection.Execute(sql, new { tag_id = tagId });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete tags with TagID {tagId}.");
            return false;
        }
    }

    public bool DeletePlayerTag(int playerId, int tagId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagRepository.DeletePlayerTag(): {playerId}, {tagId}");
        try
        {
            const string sql = "DELETE FROM player_tags WHERE player_id = @player_id AND tag_id = @tag_id";
            Connection.Execute(sql, new { player_id = playerId, tag_id = tagId });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete tag with PlayerID {playerId} and TagID {tagId}.");
            return false;
        }
    }

    public int CreatePlayerTag(int playerId, int tagId)
    {
        const string sql = @"
    INSERT INTO player_tags (player_id, tag_id, created, updated)
    VALUES (@player_id, @tag_id, @created, @updated)
    RETURNING id";

        var playerTagDto = new PlayerTagDTO { player_id = playerId, tag_id = tagId };
        SetCreateTimestamp(playerTagDto);

        var newId = Connection.ExecuteScalar<int>(sql, playerTagDto);
        return newId;
    }

    public bool DeletePlayerTagByPlayerId(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagRepository.DeletePlayerTagByPlayerId(): {playerId}");
        try
        {
            const string sql = "DELETE FROM player_tags WHERE player_id = @player_id";
            Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete tag with PlayerID {playerId}.");
            return false;
        }
    }

    public bool CreatePlayerTags(List<PlayerTag> playerTags)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagRepository.CreatePlayerTags(): {playerTags.Count}.");
        using var transaction = Connection.BeginTransaction();
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

            Connection.Execute(sql, playerTagDTOs, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to create player tags.");
            transaction.Rollback();
            return false;
        }
    }
}
