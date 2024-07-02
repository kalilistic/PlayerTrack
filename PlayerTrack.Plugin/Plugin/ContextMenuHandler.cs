using Dalamud.DrunkenToad.Core.Models;

#pragma warning disable CS0067 // Event is never used
namespace PlayerTrack.Plugin;

public static class ContextMenuHandler
{
    public delegate void SelectPlayerDelegate(ToadPlayer player, bool isCurrent);

    public static event SelectPlayerDelegate? SelectPlayer;

    public static void Start()
    {
    }

    public static void Restart()
    {
    }

    public static void Dispose()
    {
    }
}
