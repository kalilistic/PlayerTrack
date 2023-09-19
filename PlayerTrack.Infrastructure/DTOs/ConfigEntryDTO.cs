using System.Diagnostics.CodeAnalysis;
using Dapper.Contrib.Extensions;

namespace PlayerTrack.Infrastructure;

[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Not for DTO")]
public class ConfigEntryDTO
{
    [ExplicitKey]
    public string key { get; set; } = string.Empty;

    public string value { get; set; } = string.Empty;
}
