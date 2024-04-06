namespace PlayerTrack.API;

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
    /// Gets the player's current name and world.
    /// </summary>
    /// <param name="name">full player name at point in time.</param>
    /// <param name="worldId">player home world id at point in time.</param>
    /// <returns>string in the form of (name worldId).</returns>
    public string GetPlayerCurrentNameWorld(string name, uint worldId);

    /// <summary>
    /// Get notes for player.
    /// </summary>
    /// <param name="name">player's full name.</param>
    /// <param name="worldId">player home world id.</param>
    /// <returns>notes.</returns>
    public string GetPlayerNotes(string name, uint worldId);

    /// <summary>
    /// Returns a set of all known unique names/worlds combinations for a list of players
    /// </summary>
    /// <param name="players">tuple array of (player name, world id)</param>
    /// <returns>tuple array of current (player name, world id) and an array of known unique previous (player name, world id) combinations.</returns>
    public ((string, uint), (string, uint)[])[] GetUniquePlayerNameWorldHistories((string, uint)[] players);
}
