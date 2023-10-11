using Dapper.Contrib.Extensions;

namespace PlayerTrack.Infrastructure;

public class ConfigEntryDTO
{
    [ExplicitKey]
    public string key { get; set; } = string.Empty;

    public string value { get; set; } = string.Empty;
}
