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

public class PlayerNameWorldHistoryRepository : BaseRepository
{
    public PlayerNameWorldHistoryRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public int CreatePlayerNameWorldHistory(PlayerNameWorldHistory playerNameWorldHistory)
    {
        const string sql = @"
        INSERT INTO player_name_world_histories (created, updated, is_migrated, player_name, world_id, player_id)
        VALUES (@created, @updated, @is_migrated, @player_name, @world_id, @player_id)
        RETURNING id";

        var historyDto = this.Mapper.Map<PlayerNameWorldHistoryDTO>(playerNameWorldHistory);
        SetCreateTimestamp(historyDto);

        var newId = this.Connection.ExecuteScalar<int>(sql, historyDto);
        return newId;
    }

    public int UpdatePlayerId(int originalPlayerId, int newPlayerId)
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerNameWorldHistoryRepository.UpdatePlayerId()");
        try
        {
            const string updateSql = "UPDATE player_name_world_histories SET player_id = @newPlayerId WHERE player_id = @originalPlayerId";

            var numberOfUpdatedRecords = this.Connection.Execute(updateSql, new { newPlayerId, originalPlayerId });
            return numberOfUpdatedRecords;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to update playerIds from {originalPlayerId} to {newPlayerId}.");
            return 0;
        }
    }

    public string[]? GetHistoricalNames(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.GetHistoricalNames(): {playerId}");
        try
        {
            const string sql = "SELECT player_name FROM player_name_world_histories WHERE player_id = @player_id ORDER BY updated DESC";
            return this.Connection.Query<string>(sql, new { player_id = playerId }).ToArray();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get historical names for PlayerID {playerId}.");
            return null;
        }
    }

    public IEnumerable<uint>? GetHistoricalWorlds(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.GetHistoricalWorlds(): {playerId}");
        try
        {
            const string sql = "SELECT world_id FROM player_name_world_histories WHERE player_id = @player_id ORDER BY updated DESC";
            return this.Connection.Query<uint>(sql, new { player_id = playerId }).ToArray();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get historical worlds for PlayerID {playerId}.");
            return null;
        }
    }
    
    public IEnumerable<PlayerNameWorldHistory> GetPlayerNameWorldHistories(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.GetPlayerNameWorldHistories(): {playerId}");
        try
        {
            const string sql = "SELECT * FROM player_name_world_histories WHERE player_id = @player_id ORDER BY updated DESC";
            return this.Connection.Query<PlayerNameWorldHistoryDTO>(sql, new { player_id = playerId }).Select(x => Mapper.Map<PlayerNameWorldHistory>(x));
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get player name world history for PlayerID {playerId}");
            return new List<PlayerNameWorldHistory>();
        }
    }

    public IEnumerable<PlayerNameWorldHistory>? GetPlayerNameWorldHistories(int[] playerIds) 
    {
        const int maxPerQuery = 750;
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.GetPlayerNameWorldHistories()");
        try 
        {
            List<PlayerNameWorldHistory> results = new();
            string sqlBase = "SELECT * FROM player_name_world_histories ";
            for (int pageStart = 0; pageStart < playerIds.Length; pageStart += maxPerQuery) 
            {
                string whereClause = $"WHERE player_id = {playerIds[pageStart]}";
                for (int i = pageStart + 1; i < pageStart + maxPerQuery && i < playerIds.Length; i++) 
                {
                    whereClause += $" OR player_id = {playerIds[i]}";
                }
                string sql = sqlBase + whereClause;
                var pageResults = this.Connection.Query<PlayerNameWorldHistoryDTO>(sql).Select(x => Mapper.Map<PlayerNameWorldHistory>(x));
                results = results.Concat(pageResults).ToList();
            }
            return results;
        }
        catch (Exception ex) 
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get bulk player name world history");
            return null;
        }
    }

    public IEnumerable<PlayerNameWorldHistory>? GetAllPlayerNameWorldHistories() {
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.GetPlayerNameWorldHistories()");
        try {
            const string sql = "SELECT * from player_name_world_histories";
            return this.Connection.Query<PlayerNameWorldHistoryDTO>(sql).Select(x => Mapper.Map<PlayerNameWorldHistory>(x));
        }
        catch (Exception ex) {
            DalamudContext.PluginLog.Error(ex, $"Failed to get all player name world histories");
            return null;
        }
    }

    public bool DeleteNameWorldHistory(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerNameWorldHistoryRepository.DeleteNameWorldHistory(): {playerId}");
        try
        {
            const string sql = "DELETE FROM player_name_world_histories WHERE player_id = @player_id";
            this.Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to delete NameWorldHistory for PlayerID {playerId}");
            return false;
        }
    }

    public bool CreatePlayerNameWorldHistories(IEnumerable<PlayerNameWorldHistory> playerNameWorldHistoriesList)
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistories()");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
        INSERT INTO player_name_world_histories (created, updated, is_migrated, player_name, world_id, player_id)
        VALUES (@created, @updated, @is_migrated, @player_name, @world_id, @player_id)";

            var historyDTOs = playerNameWorldHistoriesList.Select(history => new PlayerNameWorldHistoryDTO
            {
                player_id = history.PlayerId,
                player_name = history.PlayerName,
                world_id = history.WorldId,
                is_migrated = history.IsMigrated,
                created = history.Created,
                updated = history.Updated,
            }).ToList();

            this.Connection.Execute(sql, historyDTOs, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to create PlayerNameWorldHistories.");
            transaction.Rollback();
            return false;
        }
    }
}
