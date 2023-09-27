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

public class TagRepository : BaseRepository
{
    public TagRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public IEnumerable<Tag>? GetAllTags()
    {
        PluginLog.LogVerbose($"Entering TagRepository.GetAllTags().");
        try
        {
            const string sql = "SELECT * FROM tags";
            var tagDTOs = this.Connection.Query<TagDTO>(sql);
            return this.Mapper.Map<IEnumerable<Tag>>(tagDTOs);
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to get all tags from the database.");
            return null;
        }
    }

    public int CreateTag(Tag tag)
    {
        PluginLog.LogVerbose($"Entering TagRepository.CreateTag(): {tag.Name}.");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var tagDTO = this.Mapper.Map<TagDTO>(tag);
            SetCreateTimestamp(tagDTO);

            const string sql =
                "INSERT INTO tags (name, color, created, updated) VALUES (@name, @color, @created, @updated)";

            this.Connection.Execute(sql, tagDTO, transaction);

            var newId = this.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: transaction);

            transaction.Commit();
            return newId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            PluginLog.LogError(ex, $"Failed to create new tag {tag.Name}.", tag);
            return 0;
        }
    }

    public bool UpdateTag(Tag tag)
    {
        PluginLog.LogVerbose($"Entering TagRepository.UpdateTag(): {tag.Name}.");
        try
        {
            var tagDTO = this.Mapper.Map<TagDTO>(tag);
            SetUpdateTimestamp(tagDTO);
            const string sql = "UPDATE tags SET name = @name, color = @color, updated = @updated WHERE id = @id";
            this.Connection.Execute(sql, tagDTO);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to update tag {tag.Name}.", tag);
            return false;
        }
    }

    public bool DeleteTag(int id)
    {
        PluginLog.LogVerbose($"Entering TagRepository.DeleteTag(): {id}.");
        try
        {
            const string sql = "DELETE FROM tags WHERE id = @id";
            this.Connection.Execute(sql, new { id });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete tag by ID {id}.");
            return false;
        }
    }

    public bool CreateTags(List<Tag> tags)
    {
        PluginLog.LogVerbose($"Entering TagRepository.CreateTags(): {tags.Count}.");
        using var transaction = this.Connection.BeginTransaction();
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

            var tagDTOs = tags.Select(this.Mapper.Map<TagDTO>).ToList();
            this.Connection.Execute(sql, tagDTOs, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to create tags.");
            transaction.Rollback();
            return false;
        }
    }
}
