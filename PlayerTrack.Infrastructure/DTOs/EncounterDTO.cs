using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class EncounterDTO : DTO
{
    public ushort territory_type_id { get; set; }

    public long ended { get; set; }
}
