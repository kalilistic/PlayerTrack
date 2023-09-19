using Dalamud.Loc.Interfaces;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Components;

public abstract class ViewComponent
{
    protected readonly ILocalization loc;
    protected readonly PluginConfig config;

    protected ViewComponent()
    {
        this.loc = ServiceContext.Localization;
        this.config = ServiceContext.ConfigService.GetConfig();
    }

    public abstract void Draw();
}
