using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class PlayerTagDTO : DTO
{
    public int player_id { get; set; }

    public int tag_id { get; set; }
}
