using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class PlayerCategoryDTO : DTO
{
    public int player_id { get; set; }

    public int category_id { get; set; }
}
