namespace PlayerTrack.Models;

public class PlayerCustomizeHistory
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public bool IsMigrated { get; set; }

    public int PlayerId { get; set; }

    public byte[] Customize { get; init; } = [];
}
