using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class LodestoneLookupDTO : DTO
{
    public int player_id { get; set; }

    public string player_name { get; set; } = string.Empty;

    public uint world_id { get; set; }
    
    public string updated_player_name { get; set; } = string.Empty;

    public uint updated_world_id { get; set; }
    
    public string world_name { get; set; } = string.Empty;

    public uint lodestone_id { get; set; }

    public int failure_count { get; set; }
    
    public int? prerequisite_lookup_id { get; set; }
    
    public bool is_done { get; set; }

    public LodestoneStatus lookup_status { get; set; } = LodestoneStatus.Unverified;
    
    public LodestoneLookupType lookup_type { get; set; } = LodestoneLookupType.Batch;
}
