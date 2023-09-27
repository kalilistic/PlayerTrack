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

    public LodestoneLookup? GetLodestoneLookupByPlayerId(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering LodestoneLookupRepository.GetLodestoneLookupByPlayerId(): {playerId}");
        try
        {
            const string sql = @"
            SELECT *
            FROM lodestone_lookups
            WHERE player_id = @player_id";

            var requestDTO = this.Connection.QuerySingleOrDefault<LodestoneLookupDTO>(
                sql,
                new { player_id = playerId });

            if (requestDTO == null)
            {
                return null;
            }

            return this.Mapper.Map<LodestoneLookup>(requestDTO);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to retrieve LodestoneLookup by player id {playerId}.");
            return null;
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
                lodestone_id = @lodestone_id,
                failure_count = @failure_count,
                lookup_status = @lookup_status
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
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
            INSERT INTO lodestone_lookups (
                created,
                updated,
                player_id,
                player_name,
                world_name,
                lodestone_id,
                failure_count,
                lookup_status
            )
            VALUES (
                @created,
                @updated,
                @player_id,
                @player_name,
                @world_name,
                @lodestone_id,
                @failure_count,
                @lookup_status
            )";

            var requestDTO = this.Mapper.Map<LodestoneLookupDTO>(lookup);
            SetCreateTimestamp(requestDTO);

            this.Connection.Execute(sql, requestDTO, transaction);

            var newId = this.Connection.ExecuteScalar<int>("SELECT last_insert_rowid()", transaction: transaction);

            transaction.Commit();
            return newId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            DalamudContext.PluginLog.Error(ex, $"Failed to create LodestoneLookup for player id {lookup.PlayerId}.", lookup);
            return 0;
        }
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
}
