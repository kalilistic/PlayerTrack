using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListDTO : DTO
{
    public ulong content_id { get; set; }
    public SocialListType list_type { get; set; }
    public ushort list_number { get; set; }
    public uint data_center_id { get; set; }
    public ushort page_count { get; set; }
    public bool add_players { get; set; }
    public bool sync_with_category { get; set; }
    public int default_category_id { get; set; }
    public string page_last_updated { get; set; } = string.Empty;
}