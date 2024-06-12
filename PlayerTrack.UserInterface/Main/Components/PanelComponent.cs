using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Main.Components;

public class PanelComponent : ViewComponent
{
    private readonly PlayerComponent playerComponent;
    private readonly AddPlayerComponent addPlayerComponent;
    private readonly LodestoneServiceComponent _lodestoneServiceComponent;

    public PanelComponent(PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, LodestoneServiceComponent lodestoneServiceComponent)
    {
        this.playerComponent = playerComponent;
        this.addPlayerComponent = addPlayerComponent;
        this._lodestoneServiceComponent = lodestoneServiceComponent;
    }

    public override void Draw()
    {
        switch (this.config.PanelType)
        {
            case PanelType.Player:
                this.playerComponent.Draw();
                break;
            case PanelType.AddPlayer:
                this.addPlayerComponent.Draw();
                break;
            case PanelType.Lodestone:
                this._lodestoneServiceComponent.Draw();
                break;
            case PanelType.None:
            default:
                break;
        }
    }
}
