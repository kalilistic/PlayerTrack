namespace PlayerTrack
{
    /// <summary>
    /// Visibility type.
    /// </summary>
    public enum VisibilityType
    {
        /// <summary>
        /// Not on any visibility lists (default).
        /// </summary>
        none = 0,

        /// <summary>
        /// On visibility voidlist.
        /// </summary>
        voidlist = 1,

        /// <summary>
        /// On visibility whitelist.
        /// </summary>
        whitelist = 2,
    }
}
