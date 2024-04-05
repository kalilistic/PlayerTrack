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
    /// Gets the player's Lodestone Id
    /// </summary>
    /// <param name="name">full player name at point in time.</param>
    /// <param name="worldId">player home world id at point in time.</param>
    /// <returns>Lodestone Id</returns>
    public uint GetPlayerLodestoneId(string name, uint worldId);

    /// <summary>
    /// Get notes for player.
    /// </summary>
    /// <param name="name">player's full name.</param>
    /// <param name="worldId">player home world id.</param>
    /// <returns>notes.</returns>
    public string GetPlayerNotes(string name, uint worldId);

    /// <summary>
    /// Gets the player's previous names
    /// </summary>
    /// <param name="name">full player name at point in time.</param>
    /// <param name="worldId">player home world id at point in time.</param>
    /// <returns>Lodestone Id</returns>
    public string[] GetPlayerPreviousNames(string name, uint worldId);
    /// <summary>
    /// Gets the player's previous names
    /// </summary>
    /// <param name="name">full player name at point in time.</param>
    /// <param name="worldId">player home world id at point in time.</param>
    /// <returns>Previous Names</returns>
    public string[] GetPlayerPreviousWorlds(string name, uint worldId);

    /// <summary>
    /// Returns a set of all known previous names and worlds for a list of players
    /// </summary>
    /// <param name="players">tuple array of player names and world Ids</param>
    /// <returns>tuple array of current player name+world id and lists of previous names and worlds</returns>
    public ((string, uint), string[], uint[])[] GetPlayersPreviousNamesWorlds((string, uint)[] players);
}
