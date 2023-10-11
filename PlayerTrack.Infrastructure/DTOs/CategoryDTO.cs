using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class CategoryDTO : DTO
{
    public string name { get; set; } = string.Empty;

    public int rank { get; set; }
}
