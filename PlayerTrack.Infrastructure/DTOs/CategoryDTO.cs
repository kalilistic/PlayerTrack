using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class CategoryDTO : DTO
{
    public string name { get; set; } = string.Empty;

    public int rank { get; set; }
    
    public int social_list_id { get; set; }
}
