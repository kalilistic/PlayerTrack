using System.Collections.Generic;

namespace PlayerTrack.Models;

public class CategoryFilter
{
    public List<int> CategoryFilterIds { get; set; } = null!;

    public List<string> CategoryFilterNames { get; set; } = null!;

    public int TotalCategories { get; set; }
}
