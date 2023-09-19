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
        try
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (ReferenceEquals(null, y))
            {
                return 1;
            }

            if (ReferenceEquals(null, x))
            {
                return -1;
            }

            var categoryComparison = this.CategoryRanks[x.PrimaryCategoryId].CompareTo(this.CategoryRanks[y.PrimaryCategoryId]);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            var nameComparison = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            if (nameComparison != 0)
            {
                return nameComparison;
            }

            var worldIdComparison = x.WorldId.CompareTo(y.WorldId);
            return worldIdComparison;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
