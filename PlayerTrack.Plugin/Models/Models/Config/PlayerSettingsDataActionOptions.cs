namespace PlayerTrack.Models;

public class PlayerSettingsDataActionOptions
{
    public bool KeepSettingsForPlayersWithNotes { get; set; } = true;
    public bool KeepSettingsForPlayersWithCategories { get; set; } = true;
    public bool KeepSettingsForPlayersWithAnySettings { get; set; } = true;
    public bool KeepSettingsForPlayersWithEncounters { get; set; } = true;
    public bool KeepSettingsForPlayersSeenInLast90Days { get; set; } = true;
    public bool KeepSettingsForPlayersVerifiedOnLodestone { get; set; } = true;
}
