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

public class PlayerEncounterRepository : BaseRepository
{
    public PlayerEncounterRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public List<int> GetPlayersWithEncounters()
    {
        try
        {
            const string sql = "SELECT DISTINCT player_id FROM player_encounters";
            return this.Connection.Query<int>(sql).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to get list of players with encounters.");
            return new List<int>();
        }
    }

    public List<PlayerEncounter>? GetAllByPlayerId(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.GetAllByPlayerId(): {playerId}");
        try
        {
            const string sql = "SELECT * FROM player_encounters WHERE player_id = @player_id ORDER BY created DESC";
            var playerEncounterDTOs = this.Connection.Query<PlayerEncounterDTO>(sql, new { player_id = playerId }).ToList();
            return playerEncounterDTOs.Select(dto => this.Mapper.Map<PlayerEncounter>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get all player encounters by player id {playerId}.");
            return null;
        }
    }

    public List<PlayerEncounter>? GetAllByEncounterId(int encounterId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.GetAllByEncounterId(): {encounterId}");
        try
        {
            const string sql = "SELECT * FROM player_encounters WHERE encounter_id = @encounter_id ORDER BY created DESC";
            var playerEncounterDTOs = this.Connection.Query<PlayerEncounterDTO>(sql, new { encounter_id = encounterId }).ToList();
            return playerEncounterDTOs.Select(dto => this.Mapper.Map<PlayerEncounter>(dto)).ToList();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get all player encounters by encounter id {encounterId}.");
            return null;
        }
    }

    public bool DeleteAllByPlayerId(int playerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.DeleteAllByPlayerId(): {playerId}");
        try
        {
            const string sql = "DELETE FROM player_encounters WHERE player_id = @player_id";
            this.Connection.Execute(sql, new { player_id = playerId });
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to delete all player encounters by player id {playerId}.");
            return false;
        }
    }

    public bool UpdatePlayerEncounter(PlayerEncounter playerEncounter)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.UpdatePlayerEncounter(): {playerEncounter.Id}");
        try
        {
            var playerEncounterDTO = this.Mapper.Map<PlayerEncounterDTO>(playerEncounter);
            SetUpdateTimestamp(playerEncounterDTO);
            const string sql = @"
            UPDATE player_encounters
            SET
                job_id = @job_id,
                job_lvl = @job_lvl,
                player_id = @player_id,
                encounter_id = @encounter_id,
                updated = @updated,
                ended = @ended
            WHERE id = @id";
            this.Connection.Execute(sql, playerEncounterDTO);
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to update player encounter with ID {playerEncounter.Id}.", playerEncounter);
            return false;
        }
    }

    public int CreatePlayerEncounter(PlayerEncounter playerEncounter)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.CreatePlayerEncounter(): {playerEncounter.Id}");
        var playerEncounterDTO = this.Mapper.Map<PlayerEncounterDTO>(playerEncounter);
        SetCreateTimestamp(playerEncounterDTO);
        const string sql = @"
        INSERT INTO player_encounters
        (job_id, job_lvl, player_id, encounter_id, created, updated, ended)
        VALUES
        (@job_id, @job_lvl, @player_id, @encounter_id, @created, @updated, @ended) RETURNING id";

        var newId = this.Connection.ExecuteScalar<int>(sql, playerEncounterDTO);
        return newId;
    }

    public PlayerEncounter? GetByPlayerIdAndEncId(int playerId, int encounterId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.GetByPlayerIdAndEncId(): {playerId}, {encounterId}");
        try
        {
            const string sql = "SELECT * FROM player_encounters WHERE player_id = @player_id AND encounter_id = @encounter_id LIMIT 1";
            var playerEncounterDTO = this.Connection.QueryFirstOrDefault<PlayerEncounterDTO>(
                sql,
                new { player_id = playerId, encounter_id = encounterId });
            return playerEncounterDTO == null ? null : this.Mapper.Map<PlayerEncounter>(playerEncounterDTO);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to get player encounter for player ID {playerId} and encounter ID {encounterId}.");
            return null;
        }
    }

    public int UpdatePlayerId(int originalPlayerId, int newPlayerId)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.UpdatePlayerId(): {originalPlayerId}, {newPlayerId}");
        try
        {
            const string updateSql = "UPDATE player_encounters SET player_id = @newPlayerId WHERE player_id = @originalPlayerId";

            var numberOfUpdatedRecords = this.Connection.Execute(updateSql, new { newPlayerId, originalPlayerId });
            return numberOfUpdatedRecords;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Failed to update playerIds from {originalPlayerId} to {newPlayerId}.");
            return 0;
        }
    }

    public bool CreatePlayerEncounters(List<PlayerEncounter> playerEncounters)
    {
        DalamudContext.PluginLog.Verbose($"Entering PlayerEncounterRepository.CreatePlayerEncounters(): {playerEncounters}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            const string sql = @"
            INSERT INTO player_encounters
            (job_id, job_lvl, player_id, encounter_id, created, updated, ended)
            VALUES
            (@job_id, @job_lvl, @player_id, @encounter_id, @created, @updated, @ended)";

            var playerEncounterDTOs = playerEncounters.Select(this.Mapper.Map<PlayerEncounterDTO>).ToList();

            this.Connection.Execute(sql, playerEncounterDTOs, transaction);
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to insert player encounters batch.");
            transaction.Rollback();
            return false;
        }
    }
}
