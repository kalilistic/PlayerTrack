namespace PlayerTrack
{
    /// <summary>
    /// Interface to communicate with PlayerTrack.
    /// </summary>
    public interface IPlayerTrackAPI
    {
        /// <summary>
        /// Gets api version.
        /// </summary>
        public int APIVersion { get; }

        /// <summary>
        /// Get player's most recent name and world.
        /// </summary>
        /// <param name="name">full player name at point in time.</param>
        /// <param name="worldId">player home world id at point in time.</param>
        /// <returns>string in the form of (name worldId).</returns>
        public string GetPlayerCurrentNameWorld(string name, uint worldId);
    }
}
