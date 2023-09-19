using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class LodestoneLookupDTO : DTO
{
    public int player_id { get; set; }

    public string player_name { get; set; } = string.Empty;

    public string world_name { get; set; } = string.Empty;

    public uint lodestone_id { get; set; }

    public int failure_count { get; set; }

    public LodestoneStatus lookup_status { get; set; } = LodestoneStatus.Unverified;
}
