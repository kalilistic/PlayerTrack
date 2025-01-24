namespace PlayerTrack.Models;

public class SocialListMember
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }
    
    public ulong ContentId { get; set; }
    
    public string Key { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public uint WorldId { get; set; }
    
    public ushort PageNumber { get; set; }
    
    public int SocialListId { get; set; }
}
