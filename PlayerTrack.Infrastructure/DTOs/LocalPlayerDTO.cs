using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class LocalPlayerDTO : DTO
{
    
    public string name { get; set; } = string.Empty;
    
    public uint world_id { get; set; }
    
    public string key { get; set; } = string.Empty;
    
    public byte[]? customize { get; set; }
    
    public ulong content_id { get; set; }
    
}