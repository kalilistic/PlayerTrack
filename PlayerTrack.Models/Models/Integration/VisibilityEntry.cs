namespace PlayerTrack.Models.Integration;

public class VisibilityEntry
{
    public string Key { get; set; } = null!;

    public string Name { get; init; } = string.Empty;

    public uint HomeWorldId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
