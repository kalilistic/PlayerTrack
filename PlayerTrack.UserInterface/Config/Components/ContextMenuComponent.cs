using Dalamud.DrunkenToad.Gui;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Config.Components;

public class ContextMenuComponent : ConfigViewComponent
{
    public override void Draw() => this.DrawControls();

    private void DrawControls()
    {
        var showOpenInPlayerTrack = this.config.ShowOpenInPlayerTrack;
        if (ToadGui.Checkbox("ShowOpenPlayerTracker", ref showOpenInPlayerTrack))
        {
            this.UpdateShowOpenInPlayerTrack(showOpenInPlayerTrack);
        }

        var showOpenInLodestone = this.config.ShowOpenLodestone;
        if (ToadGui.Checkbox("ShowOpenLodestone", ref showOpenInLodestone))
        {
            this.UpdateShowOpenLodestone(showOpenInLodestone);
        }
    }

    private void UpdateShowOpenInPlayerTrack(bool showOpenInPlayerTrack)
    {
        this.config.ShowOpenInPlayerTrack = showOpenInPlayerTrack;
        ServiceContext.ConfigService.SaveConfig(this.config);
    }

    private void UpdateShowOpenLodestone(bool showOpenLodestone)
    {
        this.config.ShowOpenLodestone = showOpenLodestone;
        ServiceContext.ConfigService.SaveConfig(this.config);
    }
}
