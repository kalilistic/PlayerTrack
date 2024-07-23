using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dapper.Contrib.Extensions;

namespace PlayerTrack.Models;

using System.Linq;

public class Player
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public long LastAlertSent { get; set; }

    public long LastSeen { get; set; }

    public byte[]? Customize { get; set; }

    public int SeenCount { get; set; }

    public LodestoneStatus LodestoneStatus { get; set; } = LodestoneStatus.Unverified;

    public KeyValuePair<FreeCompanyState, string> FreeCompany { get; set; } = new(FreeCompanyState.Unknown, string.Empty);

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public uint LodestoneId { get; set; }

    public long LodestoneVerifiedOn { get; set; }

    public uint EntityId { get; set; }

    public uint WorldId { get; set; }

    public ushort LastTerritoryType { get; set; }
    
    public ulong ContentId { get; set; }

    [Write(false)] public int PrimaryCategoryId { get; set; }

    [Write(false)] public bool IsCurrent { get; set; }

    [Write(false)] public bool IsRecent { get; set; }

    [Write(false)] public int OpenPlayerEncounterId { get; set; }

    [Write(false)] public string[] PreviousNames { get; set; } = Array.Empty<string>();

    [Write(false)] public string[] PreviousWorlds { get; set; } = Array.Empty<string>();

    [Write(false)] public PlayerConfig PlayerConfig { get; set; } = new(PlayerConfigType.Player);

    [Write(false)] public List<Tag> AssignedTags { get; set; } = new();

    [Write(false)] public List<Category> AssignedCategories { get; set; } = new();

    [Write(false)] public Vector4 PlayerListNameColor { get; set; } = ImGuiColors.DalamudWhite;

    [Write(false)] public string PlayerListIconString { get; set; } = FontAwesomeIcon.User.ToIconString();

    public override int GetHashCode() => this.Key.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is not Player player)
        {
            return false;
        }

        return this.GetHashCode() == player.GetHashCode();
    }

    public List<PlayerConfig> GetCategoryPlayerConfigs() => this.AssignedCategories.OrderBy(cat => cat.Rank).Select(cat => cat.PlayerConfig).ToList();

    public string WorldName()
    {
        return DalamudContext.DataManager.GetWorldNameById(this.WorldId);
    }
    
    public string FullyQualifiedName()
    {
        return $"{Name}@{WorldName()} (#{Id})";
    }
    
    public void Merge(Player player)
    {
        // combine
        this.SeenCount += player.SeenCount;
        this.Notes = string.IsNullOrEmpty(this.Notes) 
            ? player.Notes 
            : (string.IsNullOrEmpty(player.Notes) 
                ? this.Notes 
                : this.Notes + " | " + player.Notes);
        
        // true if either is true
        this.IsCurrent = player.IsCurrent || this.IsCurrent;
        this.IsRecent = player.IsRecent || this.IsRecent;
        
        // use from latest player if set
        if (player.LastSeen > this.LastSeen)
        {
            this.Customize = player.Customize?.Length > 0 ? player.Customize : this.Customize;
            this.LastTerritoryType = player.LastTerritoryType != 0 ? player.LastTerritoryType : this.LastTerritoryType;
            this.FreeCompany = player.FreeCompany.Key != FreeCompanyState.Unknown ? player.FreeCompany : this.FreeCompany;
            this.EntityId = player.EntityId != 0 ? player.EntityId : this.EntityId;
        }
        
        // use the most recent timestamp
        this.LastAlertSent = Math.Max(this.LastAlertSent, player.LastAlertSent);
        this.LastSeen = Math.Max(this.LastSeen, player.LastSeen);
        
        // set fields if not set
        if (ContentId == 0)
        {
            this.ContentId = player.ContentId;
        }
        if (this.LodestoneStatus != LodestoneStatus.Verified)
        {
            this.LodestoneId = player.LodestoneId;
            this.LodestoneStatus = player.LodestoneStatus;
            this.LodestoneVerifiedOn = player.LodestoneVerifiedOn;
        }
    }
}
