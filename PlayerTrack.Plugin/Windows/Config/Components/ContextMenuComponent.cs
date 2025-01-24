using System;
using PlayerTrack.Domain;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class ContextMenuComponent : ConfigViewComponent
{
    public Action? UpdateContextMenu;

    public override void Draw() => DrawControls();

    private void DrawControls()
    {
        var showOpenInPlayerTrack = Config.ShowOpenInPlayerTrack;
        if (Helper.Checkbox(Language.ShowOpenPlayerTracker, ref showOpenInPlayerTrack))
        {
            Config.ShowOpenInPlayerTrack = showOpenInPlayerTrack;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var showOpenInLodestone = Config.ShowOpenLodestone;
        if (Helper.Checkbox(Language.ShowOpenLodestone, ref showOpenInLodestone))
        {
            Config.ShowOpenLodestone = showOpenInLodestone;
            ServiceContext.ConfigService.SaveConfig(Config);
        }
    }
}
