namespace PlayerTrack
{
    /// <summary>
    /// Visibility entry.
    /// </summary>
    public class VisibilityEntry
    {
        /// <summary>
        /// Gets or sets composite player key of world id and name.
        /// </summary>
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets player name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Gets player homeworld Id.
        /// </summary>
        public uint HomeWorldId { get; init; }

        /// <summary>
        /// Gets reason for hiding player.
        /// </summary>
        public string Reason { get; init; } = string.Empty;
    }
}
