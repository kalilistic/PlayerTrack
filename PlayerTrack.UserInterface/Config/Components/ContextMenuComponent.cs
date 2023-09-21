using Dalamud.DrunkenToad.Gui;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Config.Components;

using System;

public class ContextMenuComponent : ConfigViewComponent
{
    public Action? UpdateContextMenu;

    public override void Draw() => this.DrawControls();

    private void DrawControls()
    {
        var showOpenInPlayerTrack = this.config.ShowOpenInPlayerTrack;
        if (ToadGui.Checkbox("ShowOpenPlayerTracker", ref showOpenInPlayerTrack))
        {
            this.config.ShowOpenInPlayerTrack = showOpenInPlayerTrack;
            ServiceContext.ConfigService.SaveConfig(this.config);
        }

        var showOpenInLodestone = this.config.ShowOpenLodestone;
        if (ToadGui.Checkbox("ShowOpenLodestone", ref showOpenInLodestone))
        {
            this.config.ShowOpenLodestone = showOpenInLodestone;
            ServiceContext.ConfigService.SaveConfig(this.config);
        }

        var showContextMenuIndicator = this.config.ShowContextMenuIndicator;
        if (ToadGui.Checkbox("ShowContextMenuIndicator", ref showContextMenuIndicator))
        {
            this.config.ShowContextMenuIndicator = showContextMenuIndicator;
            ServiceContext.ConfigService.SaveConfig(this.config);
            this.UpdateContextMenu?.Invoke();
        }
    }
}
