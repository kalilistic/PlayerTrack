namespace PlayerTrack.Data;

/// <summary>
/// World data.
/// </summary>
public class WorldData
{
    /// <summary>
    /// Gets or sets world id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets world name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets world's data center.
    /// </summary>
    public uint DataCenterId { get; init; }
}
