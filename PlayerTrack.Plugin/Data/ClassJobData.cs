namespace PlayerTrack.Data;

/// <summary>
/// ClassJob data.
/// </summary>
public class ClassJobData
{
    /// <summary>
    /// Gets or sets class job id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets class job name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets class job abbreviation.
    /// </summary>
    public string Code { get; init; } = string.Empty;
}
