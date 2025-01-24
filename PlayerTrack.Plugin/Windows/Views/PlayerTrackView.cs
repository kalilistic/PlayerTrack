using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.Windows.Main;

namespace PlayerTrack.Windows.Views;

public abstract class PlayerTrackView : WindowEx
{
    protected new readonly PluginConfig Config;

    protected PlayerTrackView(string name, PluginConfig config, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, config, flags)
    {
        Config = config;
    }
}
