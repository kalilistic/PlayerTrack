using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.Windows.Components;

public abstract class ViewComponent
{
    protected readonly PluginConfig Config;

    protected ViewComponent()
    {
        Config = ServiceContext.ConfigService.GetConfig();
    }

    public abstract void Draw();
}
