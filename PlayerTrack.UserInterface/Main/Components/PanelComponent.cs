using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Main.Components;

public class PanelComponent : ViewComponent
{
    private readonly PlayerComponent playerComponent;
    private readonly AddPlayerComponent addPlayerComponent;

    public PanelComponent(PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent)
    {
        this.playerComponent = playerComponent;
        this.addPlayerComponent = addPlayerComponent;
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
            case PanelType.None:
            default:
                break;
        }
    }
}
