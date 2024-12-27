using System;
using System.Collections.Generic;

namespace PlayerTrack.Models.Comparers;

public class PlayerComparer : IComparer<Player>
{
    public readonly Dictionary<int, int> CategoryRanks;
    public readonly int DefaultRank;

    public PlayerComparer(Dictionary<int, int> categoryRanks, int defaultRank)
    {
        this.CategoryRanks = categoryRanks;
        this.CategoryRanks.Add(0, defaultRank);
        this.DefaultRank = defaultRank;
    }

    public int Compare(Player? x, Player? y)
    {
        try
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            var xRank = this.CategoryRanks.GetValueOrDefault(x.PrimaryCategoryId, DefaultRank);
            var yRank = this.CategoryRanks.GetValueOrDefault(y.PrimaryCategoryId, DefaultRank);

            var categoryComparison = xRank.CompareTo(yRank);
            if (categoryComparison != 0)
            {
                return categoryComparison;
            }

            var nameComparison = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
            if (nameComparison != 0)
            {
                return nameComparison;
            }

            var worldComparison = x.WorldId.CompareTo(y.WorldId);
            
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (worldComparison != 0)
            {
                return worldComparison;
            }

            return x.Created.CompareTo(y.Created);
        }
        catch (Exception)
        {
            return 0;
        }
    }

}
