using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class LocalPlayerRepository : BaseRepository
{
    public LocalPlayerRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }
    
    public List<LocalPlayer> GetAllLocalPlayers()
    {
        const string sql = "SELECT * FROM local_players";
        var localPlayerDTOs = this.Connection.Query<LocalPlayerDTO>(sql).ToList();
        return localPlayerDTOs.Select(dto => this.Mapper.Map<LocalPlayer>(dto)).ToList();
    }
    
    public LocalPlayer? GetLocalPlayer(ulong contentId)
    {
        const string sql = "SELECT * FROM local_players WHERE content_id = @contentId";
        var localPlayerDTO = this.Connection.QuerySingleOrDefault<LocalPlayerDTO>(sql, new { contentId });
        return localPlayerDTO == null ? null : this.Mapper.Map<LocalPlayer>(localPlayerDTO);
    }

    public int CreateLocalPlayer(LocalPlayer localPlayer)
    {
        var localPlayerDTO = this.Mapper.Map<LocalPlayerDTO>(localPlayer);
        SetCreateTimestamp(localPlayerDTO);
        const string sql = @"
            INSERT INTO local_players (content_id, name, world_id, customize, key, created, updated)
            VALUES (@content_id, @name, @world_id, @customize, @key, @created, @updated)
            RETURNING id;";
        return this.Connection.ExecuteScalar<int>(sql, localPlayerDTO);
    }

    public void UpdateLocalPlayer(LocalPlayer localPlayer)
    {
        var localPlayerDTO = this.Mapper.Map<LocalPlayerDTO>(localPlayer);
        SetUpdateTimestamp(localPlayerDTO);
        const string sql = @"
            UPDATE local_players
            SET name = @name,
                world_id = @world_id,
                customize = @customize,
                key = @key,
                updated = @updated
            WHERE id = @id;";
        this.Connection.Execute(sql, localPlayerDTO);
    }

    public void DeleteLocalPlayer(int id)
    {
        const string sql = "DELETE FROM local_players WHERE id = @id";
        this.Connection.Execute(sql, new { id });
    }
    
}