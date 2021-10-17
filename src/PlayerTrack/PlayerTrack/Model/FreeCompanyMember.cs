namespace PlayerTrack
{
    /// <summary>
    /// Free Company Member from FCNameColor.
    /// </summary>
    public class FreeCompanyMember
    {
        /// <summary>
        /// Gets or sets player name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets lodestone id.
        /// </summary>
        public uint LodestoneId { get; set; }

        /// <summary>
        /// Gets player homeworld Id.
        /// </summary>
        public ushort HomeWorldId { get; init; }
    }
}
