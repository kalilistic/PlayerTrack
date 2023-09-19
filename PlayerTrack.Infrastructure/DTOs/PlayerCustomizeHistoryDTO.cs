using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class PlayerCustomizeHistoryDTO : DTO
{
    public bool is_migrated { get; set; }

    public int player_id { get; set; }

    public byte[] customize { get; init; } = System.Array.Empty<byte>();
}
