using PlayerTrack.Resource;

namespace PlayerTrack.Handler;

using Dalamud.Game.Command;

public static class CommandHandler
{
    public delegate void PlayerWindowToggledDelegate();
    public delegate void ConfigWindowToggledDelegate();

    public static event PlayerWindowToggledDelegate? OnPlayerWindowToggled;
    public static event ConfigWindowToggledDelegate? OnConfigWindowToggled;

    public static void Start()
    {
        Plugin.PluginLog.Verbose("Entering CommandHandler.Start()");
        Plugin.CommandManager.AddHandler("/ptrack", new CommandInfo((_, _) => { OnPlayerWindowToggled?.Invoke(); })
        {
            HelpMessage = Language.ShowHidePlayerTrack,
            ShowInHelp = true,
        });
        Plugin.CommandManager.AddHandler("/ptrackconfig", new CommandInfo((_, _) => { OnConfigWindowToggled?.Invoke(); })
        {
            HelpMessage = Language.ShowHidePlayerTrackConfig,
            ShowInHelp = true,
        });
    }

    public static void Dispose()
    {
        Plugin.CommandManager.RemoveHandler("/ptrack");
        Plugin.CommandManager.RemoveHandler("/ptrackconfig");
    }
}
