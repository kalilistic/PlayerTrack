namespace PlayerTrack.Models;

public class PlayerEncounter
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public int PlayerId { get; set; }

    public int EncounterId { get; init; }

    public uint JobId { get; init; }

    public byte JobLvl { get; init; }

    public long Ended { get; set; }
}
