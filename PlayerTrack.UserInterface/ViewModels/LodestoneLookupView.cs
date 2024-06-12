using System.Numerics;

namespace PlayerTrack.UserInterface.ViewModels;

public class LodestoneLookupView
{
    public int Id { get; set; }
    public string RequestPlayer { get; set; } = string.Empty;
    public string ResponsePlayer { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Created { get; set; } = string.Empty;
    public string Updated { get; set; } = string.Empty;
    public string TypeIcon { get; set; } = string.Empty;
    public uint LodestoneId { get; set; }
    public bool ShowLodestoneButton { get; set; }
    public Vector4 Color { get; set; }
    public bool hasNameWorldChanged { get; set; }
    public string NextAttemptDisplay { get; set; } = string.Empty;
    public long NextAttempt { get; set; }
    public int Rank { get; set; }
}