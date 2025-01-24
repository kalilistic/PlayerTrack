namespace PlayerTrack.Models;

/// <summary>
/// Base plugin configuration class to be extended.
/// </summary>
public interface IPluginConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for when this configuration was created. Typically in Unix epoch format.
    /// </summary>
    long Created { get; set; }

    /// <summary>
    /// Gets or sets the timestamp for the last time this configuration was updated. Typically in Unix epoch format.
    /// </summary>
    long Updated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether plugin is enabled.
    /// </summary>
    bool IsPluginEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to lock window size.
    /// </summary>
    bool IsWindowSizeLocked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to lock window size.
    /// </summary>
    bool IsWindowPositionLocked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show window when not logged in.
    /// </summary>
    bool OnlyShowWindowWhenLoggedIn { get; set; }
}
