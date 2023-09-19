using System;
using System.Collections.Generic;
using Dalamud.DrunkenToad.Util;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Helpers;

using Dalamud.DrunkenToad.Helpers;

public static class FormatHelper
{
    public static string FormatFileSize(this long value)
    {
        string[] sizes =
        {
            "Byte", "Kilobyte",
            "Megabyte", "Gigabyte",
            "Terabyte",
        };
        var order = 0;
        while (value >= 1024 && order < sizes.Length - 1)
        {
            order++;
            value /= 1024;
        }

        return $"{value:0.##} {ServiceContext.Localization.GetString(sizes[order])}";
    }

    public static string ToDuration(this long value)
    {
        var parts = new List<string>();

        void Add(int val, string unit)
        {
            if (val > 0)
            {
                parts.Add(val + ServiceContext.Localization.GetString(unit));
            }
        }

        var t = TimeSpan.FromMilliseconds(value);
        Add(t.Days, "Day");
        Add(t.Hours, "Hour");
        Add(t.Minutes, "Minute");
        var timeSpan = string.Join(" ", parts);
        return string.IsNullOrEmpty(timeSpan) ? $"< 1{ServiceContext.Localization.GetString("Minute")}" : timeSpan;
    }

    public static string ToTimeSpan(this long value)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
        string timeSpan;
        if (currentTime > value)
        {
            timeSpan = ConvertToShortTimeSpan(UnixTimestampHelper.CurrentTime() - value);
            return string.IsNullOrEmpty(timeSpan) ? ServiceContext.Localization.GetString("Now")
                       : $"{timeSpan} {ServiceContext.Localization.GetString("Ago")}";
        }

        timeSpan = ConvertToShortTimeSpan(value - UnixTimestampHelper.CurrentTime());
        return string.IsNullOrEmpty(timeSpan) ? ServiceContext.Localization.GetString("Now")
                   : $"{timeSpan} {ServiceContext.Localization.GetString("FromNow")}";
    }

    private static string ConvertToShortTimeSpan(long value)
    {
        var timeSpan = TimeSpan.FromMilliseconds(value);
        if (timeSpan.Days > 0)
        {
            return $"{timeSpan.Days}{ServiceContext.Localization.GetString("Day")}";
        }

        if (timeSpan.Hours > 0)
        {
            return $"{timeSpan.Hours}{ServiceContext.Localization.GetString("Hour")}";
        }

        if (timeSpan.Minutes > 0)
        {
            return $"{timeSpan.Minutes}{ServiceContext.Localization.GetString("Minute")}";
        }

        return string.Empty;
    }
}
