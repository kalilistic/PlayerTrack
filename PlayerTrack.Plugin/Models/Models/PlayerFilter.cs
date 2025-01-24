using System.Collections.Generic;

namespace PlayerTrack.Models;

public class PlayerFilter
{
    public List<int> FilterIds { get; set; } = null!;

    public List<string> FilterNames { get; set; } = null!;

    public int TotalFilters { get; set; }
}
