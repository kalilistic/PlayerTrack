using System.Diagnostics.CodeAnalysis;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class ArchiveRecordDTO : DTO
{
    public int Id { get; set; }

    public ArchiveType archive_type { get; set; }

    public string data { get; set; } = string.Empty;
}
