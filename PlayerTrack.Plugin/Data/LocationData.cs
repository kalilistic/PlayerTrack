namespace PlayerTrack.Data;

public enum LocationType
{
    /// <summary>
    /// None such as in the character select screen.
    /// </summary>
    None,

    /// <summary>
    /// Overworld such as Limsa Lominsa.
    /// </summary>
    Overworld,

    /// <summary>
    /// Content such as Trials or Raids.
    /// </summary>
    Content,

    /// <summary>
    /// High end duty such as Extreme Trials or Savage Raids.
    /// </summary>
    HighEndContent,
}

public class LocationData
{
    /// <summary>
    /// Gets or sets territoryTypeId.
    /// </summary>
    public ushort TerritoryId { get; set; }

    /// <summary>
    /// Gets or sets territory type place name.
    /// </summary>
    public string TerritoryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets CFC id.
    /// </summary>
    public uint ContentId { get; set; }

    /// <summary>
    /// Gets or sets CFC name.
    /// </summary>
    public string ContentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Location type (e.g. overworld, duty, high-end duty).
    /// </summary>
    public LocationType LocationType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the location is in content.
    /// </summary>
    /// <returns>indicator whether in content.</returns>
    public bool InContent() => ContentId != 0;

    /// <summary>
    /// Get content name if available otherwise place name.
    /// </summary>
    /// <returns>effective name.</returns>
    public string GetName()
    {
        return !InContent() ? TerritoryName : ContentName;
    }
}
