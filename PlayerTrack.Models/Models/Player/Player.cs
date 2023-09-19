using System;
using System.Collections.Generic;
using System.Numerics;
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

    public uint ObjectId { get; set; }

    public uint WorldId { get; set; }

    public ushort LastTerritoryType { get; set; }

    [Write(false)] public int PrimaryCategoryId { get; set; }

    [Write(false)] public bool IsCurrent { get; set; }

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

    public void Merge(Player player)
    {
        this.LastAlertSent = player.LastAlertSent;
        this.LastSeen = player.LastSeen;
        this.Customize = player.Customize;
        this.SeenCount += player.SeenCount;
        this.LodestoneStatus = player.LodestoneStatus;
        this.LodestoneVerifiedOn = player.LodestoneVerifiedOn;
        this.FreeCompany = player.FreeCompany;
        this.Key = player.Key;
        this.Name = player.Name;
        this.Notes += " " + player.Notes;
        this.LodestoneId = player.LodestoneId;
        this.ObjectId = player.ObjectId;
        this.WorldId = player.WorldId;
        this.LastTerritoryType = player.LastTerritoryType;

        this.PrimaryCategoryId = player.PrimaryCategoryId;
        this.IsCurrent = this.IsCurrent || player.IsCurrent;
        this.PreviousNames = player.PreviousNames;
        this.PreviousWorlds = player.PreviousWorlds;
        this.PlayerConfig = player.PlayerConfig;
        this.AssignedTags = player.AssignedTags;
        this.AssignedCategories = player.AssignedCategories;
    }

    public List<PlayerConfig> GetCategoryPlayerConfigs() => this.AssignedCategories.OrderBy(cat => cat.Rank).Select(cat => cat.PlayerConfig).ToList();
}
