using Dalamud.DrunkenToad.Core.Enums;
using Dalamud.DrunkenToad.Core.Models;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using Dalamud.Logging;

public class PlayerNameplateService
{
    public static PlayerNameplate GetPlayerNameplate(Player player, ToadLocation loc)
    {
        PluginLog.LogVerbose($"Entering PlayerNameplateService.GetPlayerNameplate(): {player.Id}, {loc.LocationType}");
        var nameplate = new PlayerNameplate
        {
            CustomizeNameplate = loc.LocationType switch
            {
                ToadLocationType.Overworld => PlayerConfigService.GetNameplateShowInOverworld(player),
                ToadLocationType.Content => PlayerConfigService.GetNameplateShowInContent(player),
                ToadLocationType.HighEndContent => PlayerConfigService.GetNameplateShowInHighEndContent(player),
                _ => false,
            },
        };

        if (!nameplate.CustomizeNameplate)
        {
            return nameplate;
        }

        var isColorEnabled = PlayerConfigService.GetNameplateUseColor(player);
        if (isColorEnabled)
        {
            nameplate.Color = PlayerConfigService.GetNameplateColor(player);
        }

        nameplate.NameplateUseColorIfDead = PlayerConfigService.GetNameplateUseColorIfDead(player);

        var nameplateTitleType = PlayerConfigService.GetNameplateTitleType(player);
        var title = string.Empty;

        if (nameplateTitleType == NameplateTitleType.CustomTitle)
        {
            title = PlayerConfigService.GetNameplateCustomTitle(player);
        }
        else if (nameplateTitleType == NameplateTitleType.CategoryName && player.PrimaryCategoryId != 0)
        {
            title = ServiceContext.CategoryService.GetCategory(player.PrimaryCategoryId)?.Name ?? string.Empty;
        }

        if (nameplateTitleType != NameplateTitleType.NoChange && !string.IsNullOrEmpty(title))
        {
            nameplate.CustomTitle = title;
            nameplate.HasCustomTitle = true;
        }

        if ((!isColorEnabled || nameplate.Color == 0) && !nameplate.HasCustomTitle)
        {
            nameplate.CustomizeNameplate = false;
        }

        return nameplate;
    }
}
