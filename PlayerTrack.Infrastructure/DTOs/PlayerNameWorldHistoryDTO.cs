using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class PlayerNameWorldHistoryDTO : DTO
{
    public bool is_migrated { get; set; }
    
    public PlayerHistorySource source { get; set; }

    public string player_name { get; set; } = string.Empty;

    public uint world_id { get; set; }

    public int player_id { get; set; }
}
