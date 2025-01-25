using System;
using System.Collections.Generic;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Helpers;

public static class FormatHelper
{
    public static string FormatFileSize(this long value)
    {
        string[] sizes =
        [
            Language.Byte, Language.Kilobyte,
            Language.Megabyte, Language.Gigabyte,
            Language.Terabyte
        ];

        var order = 0;
        while (value >= 1024 && order < sizes.Length - 1)
        {
            order++;
            value /= 1024;
        }

        return $"{value:0.##} {sizes[order]}";
    }

    public static string ToDuration(this long value)
    {
        var parts = new List<string>();
        void Add(int val, string unit)
        {
            if (val > 0)
                parts.Add($"{val}{unit}");
        }

        var t = TimeSpan.FromMilliseconds(value);
        Add(t.Days, Language.Day);
        Add(t.Hours, Language.Hour);
        Add(t.Minutes, Language.Minute);
        var timeSpan = string.Join(" ", parts);
        return string.IsNullOrEmpty(timeSpan) ? $"< 1 {Language.Minute}" : timeSpan;
    }

    public static string ToTimeSpan(this long value)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        string timeSpan;
        if (currentTime > value)
        {
            timeSpan = ConvertToShortTimeSpan(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - value);
            return string.IsNullOrEmpty(timeSpan) ? Language.Now : $"{timeSpan} {Language.Ago}";
        }

        timeSpan = ConvertToShortTimeSpan(value - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return string.IsNullOrEmpty(timeSpan) ? Language.Now : $"{timeSpan} {Language.FromNow}";
    }

    private static string ConvertToShortTimeSpan(long value)
    {
        var timeSpan = TimeSpan.FromMilliseconds(value);
        if (timeSpan.Days > 0)
            return $"{timeSpan.Days}{Language.Day}";

        if (timeSpan.Hours > 0)
            return $"{timeSpan.Hours}{Language.Hour}";

        if (timeSpan.Minutes > 0)
            return $"{timeSpan.Minutes}{Language.Minute}";

        return string.Empty;
    }
}
