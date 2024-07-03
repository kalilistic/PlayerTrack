using System.Collections.Generic;
using System.Numerics;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.ViewModels;

public class PlayerView
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string HomeWorld { get; set; } = null!;

    public string FreeCompany { get; set; } = null!;

    public uint LodestoneId { get; set; }
    
    public string FirstSeen { get; set; } = null!;

    public string LastSeen { get; set; } = null!;

    public string LastLocation { get; set; } = null!;

    public string SeenCount { get; set; } = null!;

    public string Appearance { get; set; } = null!;

    public string PreviousNames { get; set; } = null!;

    public string PreviousWorlds { get; set; } = null!;

    public List<Tag> AssignedTags { get; set; } = new();

    public List<Tag> UnassignedTags { get; set; } = new();

    public int PrimaryCategoryId { get; set; }

    public List<Category> AssignedCategories { get; set; } = new();

    public List<Category> UnassignedCategories { get; set; } = new();

    public string Notes { get; set; } = null!;

    public PlayerConfig PlayerConfig { get; set; } = null!;

    public List<PlayerEncounterView> Encounters { get; set; } = new();
}
