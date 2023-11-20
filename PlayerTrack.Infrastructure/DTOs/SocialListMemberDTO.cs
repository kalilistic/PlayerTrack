using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class SocialListMemberDTO : DTO
{
    public ulong content_id { get; set; }
    public string key { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public uint world_id { get; set; }
    public ushort page_number { get; set; }
    public int social_list_id { get; set; }
}