namespace PlayerTrack.Data;

/// <summary>
/// DataCenter data.
/// </summary>
public class DCData
{
    /// <summary>
    /// Gets or sets data center id.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets data center name.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
