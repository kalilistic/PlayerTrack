using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class EncounterDTO : DTO
{
    public ushort territory_type_id { get; set; }

    public long ended { get; set; }
}
