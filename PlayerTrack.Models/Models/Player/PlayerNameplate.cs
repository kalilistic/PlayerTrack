namespace PlayerTrack.Models;

public class PlayerNameplate
{
    public bool CustomizeNameplate { get; set; }

    public uint Color { get; set; }

    public bool DisableColorIfDead { get; set; }

    public bool HasCustomTitle { get; set; }

    public string? CustomTitle { get; set; }
}
