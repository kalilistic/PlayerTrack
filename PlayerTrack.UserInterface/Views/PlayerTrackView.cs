using Dalamud.DrunkenToad.Gui;
using ImGuiNET;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Views;

public abstract class PlayerTrackView : WindowEx
{
    protected new readonly PluginConfig config;

    protected PlayerTrackView(string name, PluginConfig config, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags) => this.config = config;
}
