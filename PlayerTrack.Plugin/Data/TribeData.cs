namespace PlayerTrack.Data;

/// <summary>
/// Tribe data.
/// </summary>
public class TribeData
{
    /// <summary>
    /// Gets or sets tribe Id.
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
