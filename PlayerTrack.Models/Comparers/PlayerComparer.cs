using System;
using System.Collections.Generic;

namespace PlayerTrack.Models.Comparers;

public class PlayerComparer : IComparer<Player>
{
    public readonly Dictionary<int, int> CategoryRanks;
    private const int DefaultRank = 99;

    public PlayerComparer(Dictionary<int, int> categoryRanks)
    {
        this.CategoryRanks = categoryRanks;
        this.CategoryRanks.Add(0, DefaultRank);
    }

    public int Compare(Player? x, Player? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (ReferenceEquals(null, y)) return 1;
        if (ReferenceEquals(null, x)) return -1;

        var xRank = this.CategoryRanks.TryGetValue(x.PrimaryCategoryId, out var rank) ? rank : DefaultRank;
        var yRank = this.CategoryRanks.TryGetValue(y.PrimaryCategoryId, out var categoryRank) ? categoryRank : DefaultRank;
        
        var categoryComparison = xRank.CompareTo(yRank);
        if (categoryComparison != 0)
        {
            return categoryComparison;
        }

        var nameComparison = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        return nameComparison != 0 ? nameComparison : x.WorldId.CompareTo(y.WorldId);
    }
}
