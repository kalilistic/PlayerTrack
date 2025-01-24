namespace PlayerTrack.Data;

/// <summary>
/// Race data.
/// </summary>
public class RaceData
{
    /// <summary>
    /// Gets or sets race Id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets masculine name.
    /// </summary>
    public string MasculineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets feminine name.
    /// </summary>
    public string FeminineName { get; set; } = string.Empty;
}
