namespace PlayerTrack
{
    /// <summary>
    /// Lodestone response.
    /// </summary>
    public class LodestoneResponse
    {
        /// <summary>
        /// Player key.
        /// </summary>
        public string PlayerKey = string.Empty;

        /// <summary>
        /// Gets or sets lodestone status.
        /// </summary>
        public LodestoneStatus Status { get; set; } = LodestoneStatus.Unverified;

        /// <summary>
        /// Gets or sets lodestone Id.
        /// </summary>
        public uint LodestoneId { get; set; }
    }
}
