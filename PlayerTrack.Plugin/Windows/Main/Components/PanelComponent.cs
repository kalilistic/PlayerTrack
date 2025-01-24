using PlayerTrack.Models;
using PlayerTrack.Windows.Components;

namespace PlayerTrack.Windows.Main.Components;

public class PanelComponent : ViewComponent
{
    private readonly PlayerComponent PlayerComponent;
    private readonly AddPlayerComponent AddPlayerComponent;

    public PanelComponent(PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent)
    {
        PlayerComponent = playerComponent;
        AddPlayerComponent = addPlayerComponent;
    }

    public override void Draw()
    {
        switch (Config.PanelType)
        {
            case PanelType.Player:
                PlayerComponent.Draw();
                break;
            case PanelType.AddPlayer:
                AddPlayerComponent.Draw();
                break;
            case PanelType.None:
            default:
                break;
        }
    }
}
