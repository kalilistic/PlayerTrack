namespace PlayerTrack.Plugin;

using Dalamud.DrunkenToad.Core;
using Dalamud.Game.Command;
using Dalamud.Logging;

public static class CommandHandler
{
    public delegate void PlayerWindowToggledDelegate();

    public delegate void ConfigWindowToggledDelegate();

    public static event PlayerWindowToggledDelegate? PlayerWindowToggled;

    public static event ConfigWindowToggledDelegate? ConfigWindowToggled;

    public static void Start()
    {
        PluginLog.LogVerbose("Entering CommandHandler.Start()");
        DalamudContext.CommandManager.AddHandler("/ptrack", new CommandInfo((_, _) => { PlayerWindowToggled?.Invoke(); })
        {
            HelpMessage = DalamudContext.LocManager.GetString("ShowHidePlayerTrack"),
            ShowInHelp = true,
        });
        DalamudContext.CommandManager.AddHandler("/ptrackconfig", new CommandInfo((_, _) => { ConfigWindowToggled?.Invoke(); })
        {
            HelpMessage = DalamudContext.LocManager.GetString("ShowHidePlayerTrackConfig"),
            ShowInHelp = true,
        });
    }

    public static void Dispose()
    {
        DalamudContext.CommandManager.RemoveHandler("/ptrack");
        DalamudContext.CommandManager.RemoveHandler("/ptrackconfig");
    }
}
