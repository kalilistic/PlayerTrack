using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

using Dalamud.DrunkenToad.Core;

public class LodestoneLookupRepository : BaseRepository
{
    public LodestoneLookupRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public List<LodestoneLookup>? GetAllLodestoneLookups()
    {
        DalamudContext.PluginLog.Verbose("Entering LodestoneLookupRepository.GetAlLodestoneLookups()");
        try
        {
            const string sql = "SELECT * FROM lodestone_lookups";
            var requestDTOs = this.Connection.Query<LodestoneLookupDTO>(sql).ToList();
            return requestDTOs.Select(dto => this.Mapper.Map<LodestoneLookup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to get all LodestoneLookups.");
            return null;
        }
    }
    
    public List<LodestoneLookup> GetLodestoneLookupsByPlayerId(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.GetLodestoneLookupByPlayerId(): {playerId}");
        try
        {
            const string sql = @"
            SELECT *
            FROM lodestone_lookups
            WHERE player_id = @player_id";

            var requestDTOs = this.Connection.Query<LodestoneLookupDTO>(sql, new { player_id = playerId }).ToList();
            return requestDTOs.Select(dto => this.Mapper.Map<LodestoneLookup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to retrieve LodestoneLookup by player id {playerId}.");
            return new List<LodestoneLookup>();
        }
    }

    public bool UpdateLodestoneLookup(LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.UpdateLodestoneLookup(): {lookup.Id}");
        try
        {
            const string sql = @"
            UPDATE lodestone_lookups
            SET
                created = @created,
                updated = @updated,
                player_id = @player_id,
                player_name = @player_name,
                world_name = @world_name,
                updated_player_name = @updated_player_name,
                updated_world_id = @updated_world_id,
                world_id = @world_id,
                lodestone_id = @lodestone_id,
                failure_count = @failure_count,
                lookup_status = @lookup_status,
                lookup_type = @lookup_type,
                prerequisite_lookup_id = @prerequisite_lookup_id,
                is_done = @is_done
            WHERE id = @id";

            var requestDTO = this.Mapper.Map<LodestoneLookupDTO>(lookup);
            SetUpdateTimestamp(requestDTO);

            this.Connection.Execute(sql, requestDTO);
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to update LodestoneLookup with id {lookup.Id}.", lookup);
            return false;
        }
    }

    public int CreateLodestoneLookup(LodestoneLookup lookup)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.CreateLodestoneLookup(): {lookup.PlayerId}");
        const string sql = @"
        INSERT INTO lodestone_lookups (
            created,
            updated,
            player_id,
            player_name,
            world_name,
            world_id,
            updated_player_name,
            updated_world_id,
            lodestone_id,
            failure_count,
            lookup_status,
            lookup_type,
            prerequisite_lookup_id,
            is_done
        )
        VALUES (
            @created,
            @updated,
            @player_id,
            @player_name,
            @world_name,
            @world_id,
            @updated_player_name,
            @updated_world_id,
            @lodestone_id,
            @failure_count,
            @lookup_status,
            @lookup_type,
            @prerequisite_lookup_id,
            @is_done
        ) RETURNING id";

        var requestDTO = this.Mapper.Map<LodestoneLookupDTO>(lookup);
        SetCreateTimestamp(requestDTO);
        var newId = this.Connection.ExecuteScalar<int>(sql, requestDTO);
        return newId;
    }

    public IEnumerable<LodestoneLookup>? GetRequestsByStatus(LodestoneStatus status)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.GetRequestsByStatus(): {status}");
        try
        {
            const string sql = "SELECT * FROM lodestone_lookups WHERE lookup_status = @lookup_status";
            var requestDTOs = this.Connection
                .Query<LodestoneLookupDTO>(sql, new { lookup_status = (int)status }).ToList();
            return requestDTOs.Select(dto => this.Mapper.Map<LodestoneLookup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get LodestoneLookups by status {status}.");
            return null;
        }
    }

    public bool DeleteLodestoneRequestByPlayerId(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.DeleteLodestoneRequestByPlayerId(): {playerId}");
        try
        {
            const string sql = "DELETE FROM lodestone_lookups WHERE player_id = @player_id";
            this.Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to delete LodestoneRequests by player id {playerId}.");
            return false;
        }
    }

    public LodestoneLookup? GetLodestoneLookupById(int lookupPrerequisiteLookupId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.GetLodestoneLookupById(): {lookupPrerequisiteLookupId}");
        try
        {
            const string sql = "SELECT * FROM lodestone_lookups WHERE id = @id";
            var requestDTO = this.Connection.QueryFirstOrDefault<LodestoneLookupDTO>(sql, new { id = lookupPrerequisiteLookupId });
            return this.Mapper.Map<LodestoneLookup>(requestDTO);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get LodestoneLookup by id {lookupPrerequisiteLookupId}.");
            return null;
        }
    }

    public List<LodestoneLookup> GetLodestoneLookupsByPrerequisiteLookupId(int lookupId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.GetLodestoneLookupsByPrerequisiteLookupId(): {lookupId}");
        try
        {
            const string sql = @"
            SELECT *
            FROM lodestone_lookups
            WHERE prerequisite_lookup_id = @prerequisite_lookup_id";

            var requestDTOs = this.Connection.Query<LodestoneLookupDTO>(sql, new { prerequisite_lookup_id = lookupId }).ToList();
            return requestDTOs.Select(dto => this.Mapper.Map<LodestoneLookup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to retrieve LodestoneLookup by prerequisite lookup id {lookupId}.");
            return new List<LodestoneLookup>();
        }
    }
}
