using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class TagDTO : DTO
{
    public string name { get; set; } = string.Empty;

    public uint color { get; set; } = 5;
}
