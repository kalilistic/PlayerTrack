using System.Collections.Generic;

namespace PlayerTrack.Models;

public class LocalPlayer
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }
    
    public ulong ContentId { get; set; }
    
    public byte[]? Customize { get; set; }
    
    public string Key { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public uint WorldId { get; set; }
    
    public KeyValuePair<FreeCompanyState, string> FreeCompany { get; set; } = new(FreeCompanyState.Unknown, string.Empty);
}
