namespace PlayerTrack.Models;

public class EncounterDataActionOptions
{
    public bool KeepEncountersInOverworld { get; set; } = true;

    public bool KeepEncountersInNormalContent { get; set; } = true;

    public bool KeepEncountersInHighEndContent { get; set; } = true;

    public bool KeepEncountersFromLast90Days { get; set; } = true;
}
