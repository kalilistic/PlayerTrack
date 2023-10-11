using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class PlayerCustomizeHistoryDTO : DTO
{
    public bool is_migrated { get; set; }

    public int player_id { get; set; }

    public byte[] customize { get; init; } = System.Array.Empty<byte>();
}
