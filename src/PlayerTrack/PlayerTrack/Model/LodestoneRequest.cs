namespace PlayerTrack
{
    /// <summary>
    /// Request for lodestone.
    /// </summary>
    public class LodestoneRequest
    {
        /// <summary>
        /// Player key.
        /// </summary>
        public string PlayerKey = string.Empty;

        /// <summary>
        /// Gets or sets player name.
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets world name.
        /// </summary>
        public string WorldName { get; set; } = string.Empty;
    }
}
