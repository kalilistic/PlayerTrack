namespace PlayerTrack.Models;

public class PlayerDataActionOptions
{
    public bool KeepPlayersWithNotes { get; set; } = true;

    public bool KeepPlayersWithCategories { get; set; } = true;

    public bool KeepPlayersWithAnySettings { get; set; } = true;

    public bool KeepPlayersWithEncounters { get; set; } = true;

    public bool KeepPlayersSeenInLast90Days { get; set; } = true;

    public bool KeepPlayersVerifiedOnLodestone { get; set; } = true;
}
