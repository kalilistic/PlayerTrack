using PlayerTrack.Models.Structs;

namespace PlayerTrack.Models;

public class PlayerConfig
{
    public bool IsChanged;
    public int? PlayerId;
    public int? CategoryId;
    public PlayerConfigType PlayerConfigType;
    public ConfigValue<uint> PlayerListNameColor;
    public ConfigValue<char> PlayerListIcon;
    public ConfigValue<string> NameplateCustomTitle;
    public ConfigValue<bool> NameplateShowInOverworld;
    public ConfigValue<bool> NameplateShowInContent;
    public ConfigValue<bool> NameplateShowInHighEndContent;
    public ConfigValue<uint> NameplateColor;
    public ConfigValue<bool> NameplateShowColor;
    public ConfigValue<bool> NameplateDisableColorIfDead;
    public ConfigValue<bool> NameplateUseCategoryName;
    public ConfigValue<bool> NameplateUseCustomTitle;
    public ConfigValue<bool> AlertNameChange;
    public ConfigValue<bool> AlertWorldTransfer;
    public ConfigValue<bool> AlertProximity;
    public ConfigValue<bool> AlertFormatIncludeCategory;
    public ConfigValue<bool> AlertFormatIncludeCustomTitle;
    public ConfigValue<VisibilityType> VisibilityType;

    public PlayerConfig()
    {
    }

    public PlayerConfig(PlayerConfigType playerConfigType)
    {
        this.PlayerConfigType = playerConfigType;
        var inheritOverride = this.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Inherit;
        this.PlayerListNameColor = new ConfigValue<uint>(inheritOverride, 1);
        this.PlayerListIcon = new ConfigValue<char>(inheritOverride, (char)61447);
        this.NameplateCustomTitle = new ConfigValue<string>(inheritOverride, string.Empty);
        this.NameplateShowInOverworld = new ConfigValue<bool>(inheritOverride, true);
        this.NameplateShowInContent = new ConfigValue<bool>(inheritOverride, true);
        this.NameplateShowInHighEndContent = new ConfigValue<bool>(inheritOverride, true);
        this.NameplateColor = new ConfigValue<uint>(inheritOverride, 1);
        this.NameplateShowColor = new ConfigValue<bool>(inheritOverride, false);
        this.NameplateDisableColorIfDead = new ConfigValue<bool>(inheritOverride, false);
        this.NameplateUseCategoryName = new ConfigValue<bool>(inheritOverride, false);
        this.NameplateUseCustomTitle = new ConfigValue<bool>(inheritOverride, false);
        this.AlertNameChange = new ConfigValue<bool>(inheritOverride, true);
        this.AlertWorldTransfer = new ConfigValue<bool>(inheritOverride, true);
        this.AlertProximity = new ConfigValue<bool>(inheritOverride, false);
        this.AlertFormatIncludeCategory = new ConfigValue<bool>(inheritOverride, true);
        this.AlertFormatIncludeCustomTitle = new ConfigValue<bool>(inheritOverride, true);
        this.VisibilityType = new ConfigValue<VisibilityType>(inheritOverride, Models.VisibilityType.None);
    }

    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }
}
