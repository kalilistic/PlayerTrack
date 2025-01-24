using System.Numerics;

namespace PlayerTrack.Extensions;

/// <summary>
/// Uint Extensions.
/// </summary>
public static class UintExtensions
{
    /// <summary>
    /// Convert a UIColor (uint) to a Vector4.
    /// </summary>
    /// <param name="col">color.</param>
    /// <returns>color as vector4.</returns>
    public static Vector4 ToVector4(this uint col)
    {
        const float inv255 = 1 / 255f;
        return new Vector4(
            (col >> 24 & 255) * inv255,
            (col >> 16 & 255) * inv255,
            (col >> 8 & 255) * inv255,
            (col & 255) * inv255);
    }
}
