namespace PlayerTrack.Data;

/// <summary>
/// Subset of key properties from IPlayerCharacter for eventing.
/// </summary>
// ReSharper disable ConditionIsAlwaysTrueOrFalse
public class PlayerData
{
    /// <summary>
    /// Player Job ID.
    /// </summary>
    public uint ClassJob;

    /// <summary>
    /// Player Free Company.
    /// </summary>
    public string CompanyTag = null!;

    /// <summary>
    /// Player Customize Array.
    /// </summary>
    public byte[]? Customize;

    /// <summary>
    /// Player HomeWorld ID.
    /// </summary>
    public uint HomeWorld;

    /// <summary>
    /// Player Entity ID assigned to networked GameObjects.
    /// </summary>
    public uint EntityId;

    /// <summary>
    /// Player GameObject ID.
    /// </summary>
    public ulong GameObjectId;

    /// <summary>
    /// Is Player Dead.
    /// </summary>
    public bool IsDead;

    /// <summary>
    /// Is Local Player.
    /// </summary>
    public bool IsLocalPlayer;

    /// <summary>
    /// Player Job Level.
    /// </summary>
    public byte Level;

    /// <summary>
    /// Player Name.
    /// </summary>
    public string Name = null!;

    /// <summary>
    /// Player Content ID.
    /// </summary>
    public ulong ContentId;

    /// <summary>
    /// Is Player Valid.
    /// </summary>
    /// <remarks>
    /// Use Dalamud's IsValidCharacterName() for more robust checks.
    /// </remarks>
    /// <returns>Indicator if player is valid.</returns>
    public bool IsValid() => ContentId > 0 &&
                             !string.IsNullOrEmpty(Name) &&
                             HomeWorld != ushort.MaxValue &&
                             HomeWorld != 0 &&
                             ClassJob != 0 &&
                             EntityId >= 0 &&
                             EntityId != uint.MaxValue;
}
