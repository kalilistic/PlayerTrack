using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class TagRepository : BaseRepository
{
    public TagRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public IEnumerable<Tag>? GetAllTags()
    {
        Plugin.PluginLog.Verbose("Entering TagRepository.GetAllTags().");
        try
        {
            const string sql = "SELECT * FROM tags";
            var tagDTOs = Connection.Query<TagDTO>(sql);
            return Mapper.Map<IEnumerable<Tag>>(tagDTOs);
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to get all tags from the database.");
            return null;
        }
    }

    public int CreateTag(Tag tag)
    {
        const string sql =
            "INSERT INTO tags (name, color, created, updated) VALUES (@name, @color, @created, @updated) RETURNING id";

        var tagDTO = Mapper.Map<TagDTO>(tag);
        SetCreateTimestamp(tagDTO);

        var newId = Connection.ExecuteScalar<int>(sql, tagDTO);
        return newId;
    }

    public bool UpdateTag(Tag tag)
    {
        Plugin.PluginLog.Verbose($"Entering TagRepository.UpdateTag(): {tag.Name}.");
        try
        {
            var tagDTO = Mapper.Map<TagDTO>(tag);
            SetUpdateTimestamp(tagDTO);
            const string sql = "UPDATE tags SET name = @name, color = @color, updated = @updated WHERE id = @id";
            Connection.Execute(sql, tagDTO);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to update tag {tag.Name}.", tag);
            return false;
        }
    }

    public bool DeleteTag(int id)
    {
        Plugin.PluginLog.Verbose($"Entering TagRepository.DeleteTag(): {id}.");
        try
        {
            const string sql = "DELETE FROM tags WHERE id = @id";
            Connection.Execute(sql, new { id });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete tag by ID {id}.");
            return false;
        }
    }

    public bool CreateTags(List<Tag> tags)
    {
        Plugin.PluginLog.Verbose($"Entering TagRepository.CreateTags(): {tags.Count}.");
        using var transaction = Connection.BeginTransaction();
        try
        {
            const string sql = @"
            INSERT INTO tags (
                id,
                name,
                color,
                created,
                updated)
            VALUES (
                @id,
                @name,
                @color,
                @created,
                @updated)";

            var tagDTOs = tags.Select(Mapper.Map<TagDTO>).ToList();
            Connection.Execute(sql, tagDTOs, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to create tags.");
            transaction.Rollback();
            return false;
        }
    }
}
