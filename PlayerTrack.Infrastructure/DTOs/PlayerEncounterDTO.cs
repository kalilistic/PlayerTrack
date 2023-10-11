using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class PlayerEncounterDTO : DTO
{
    public int player_id { get; set; }

    public int encounter_id { get; set; }

    public uint job_id { get; set; }

    public byte job_lvl { get; set; }

    public long ended { get; set; }
}
