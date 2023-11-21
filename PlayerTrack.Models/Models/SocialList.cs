using System.Collections.Generic;
using System.Linq;
using Dapper.Contrib.Extensions;

namespace PlayerTrack.Models;

public class SocialList
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }
    
    public ulong ContentId { get; set; }
    
    public SocialListType ListType { get; set; }
    
    public ushort ListNumber { get; set; }
    
    public uint DataCenterId { get; set; }
    
    public ushort PageCount { get; set; }
    
    public bool AddPlayers { get; set; }
    
    public bool SyncWithCategory { get; set; }
    
    public int DefaultCategoryId { get; set; }
    
    public readonly Dictionary<ushort, long> PageLastUpdated = new();
    
    public long GetLastUpdated()
    {
        // only free company uses multiple pages
        if (ListType == SocialListType.FreeCompany)
        {
            return PageLastUpdated.Count == 0 ? 0 : PageLastUpdated.Values.Min();
        }
        
        return Updated;
    }
}