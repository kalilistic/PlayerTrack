namespace PlayerTrack.Domain.Common;

public static class PlayerKeyBuilder
{
    public static string Build(string name, uint worldId) =>
        string.Concat(name.Replace(' ', '_').ToUpperInvariant(), "_", worldId);
}
