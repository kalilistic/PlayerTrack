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

        var isColorEnabled = PlayerConfigService.GetNameplateShowColor(player);
        if (isColorEnabled)
        {
            nameplate.Color = PlayerConfigService.GetNameplateColor(player);
        }

        nameplate.DisableColorIfDead = PlayerConfigService.GetNameplateShowColorIfDead(player);

        var useCustomTitle = PlayerConfigService.GetNameplateUseCustomTitle(player);
        var customTitle = string.Empty;
        if (useCustomTitle)
        {
            customTitle = PlayerConfigService.GetNameplateCustomTitle(player);
            if (string.IsNullOrEmpty(customTitle))
            {
                var useCategory = PlayerConfigService.GetNameplateUseCategory(player);
                if (useCategory && player.PrimaryCategoryId != 0)
                {
                    var category = ServiceContext.CategoryService.GetCategory(player.PrimaryCategoryId);
                    if (category != null)
                    {
                        customTitle = category.Name;
                    }
                }
            }
        }

        if (useCustomTitle && !string.IsNullOrEmpty(customTitle))
        {
            nameplate.CustomTitle = customTitle;
            nameplate.HasCustomTitle = true;
        }

        if ((!isColorEnabled || nameplate.Color == 0) && !nameplate.HasCustomTitle)
        {
            nameplate.CustomizeNameplate = false;
        }

        return nameplate;
    }
}
