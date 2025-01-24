using System.Text;

namespace PlayerTrack.Extensions;

public static class StringExtension
{
    /// <summary>
    /// Truncates the string to a specified length and appends an ellipsis if truncated.
    /// </summary>
    /// <param name="value">string to truncate.</param>
    /// <param name="lengthLimit">maximum length of the string.</param>
    /// <returns>truncated string.</returns>
    public static string TruncateWithEllipsis(this string value, int lengthLimit)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > lengthLimit ? new StringBuilder(value, 0, lengthLimit, lengthLimit + 3).Append("...").ToString() : value;
    }

    /// <summary>
    /// Converts the first character of the string to lowercase.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The string with its first character converted to lowercase.</returns>
    public static string FirstCharToLower(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToLowerInvariant(input[0]) + input[1..];
    }
}
