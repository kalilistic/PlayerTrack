using Dalamud.Plugin.Services;
using PlayerTrack.Data;

namespace PlayerTrack.Extensions;

/// <summary>
/// Dalamud ClientStateHandler extensions.
/// </summary>
public static class ClientStateExtension
{
    /// <summary>
    /// Validate if actor is valid player character.
    /// </summary>
    /// <param name="value">actor.</param>
    /// <returns>Indicator if player character is valid.</returns>
    public static LocalPlayerData? GetLocalPlayer(this IClientState value)
    {
        if (value.LocalPlayer == null)
            return null;

        var localPlayer = new LocalPlayerData
        {
            Name = value.LocalPlayer.Name.TextValue,
            HomeWorld = value.LocalPlayer.HomeWorld.RowId,
            ContentId = value.LocalContentId,
            Customize = value.LocalPlayer.Customize,
        };

        return localPlayer.IsValid() ? localPlayer : null;
    }
}
