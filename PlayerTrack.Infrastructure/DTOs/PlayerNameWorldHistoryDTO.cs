using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class PlayerNameWorldHistoryDTO : DTO
{
    public bool is_migrated { get; set; }

    public string player_name { get; set; } = string.Empty;

    public uint world_id { get; set; }

    public int player_id { get; set; }
}
