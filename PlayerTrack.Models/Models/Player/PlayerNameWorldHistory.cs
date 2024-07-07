namespace PlayerTrack.Models;

public class PlayerNameWorldHistory
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public bool IsMigrated { get; set; }
    
    public PlayerHistorySource Source { get; set; }

    public int PlayerId { get; set; }

    public string PlayerName { get; init; } = string.Empty;

    public uint WorldId { get; set; }
}
