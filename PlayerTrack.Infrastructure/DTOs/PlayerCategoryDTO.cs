using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class PlayerCategoryDTO : DTO
{
    public int player_id { get; set; }

    public int category_id { get; set; }
}
