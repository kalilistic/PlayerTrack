using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class PlayerEncounterDTO : DTO
{
    public int player_id { get; set; }

    public int encounter_id { get; set; }

    public uint job_id { get; set; }

    public byte job_lvl { get; set; }

    public long ended { get; set; }
}
