using Dalamud.Game.Gui.ContextMenu;
using PlayerTrack.Data;
using PlayerTrack.Domain;
using PlayerTrack.Extensions;
using PlayerTrack.Resource;

namespace PlayerTrack.Handler;

public static class ContextMenuHandler
{
    private const char PrefixChar = 'P';
    public delegate void SelectPlayerDelegate(PlayerData player, bool isCurrent);
    public static event SelectPlayerDelegate? OnSelectPlayer;

    public static void Start()
    {
        Plugin.ContextMenu.OnMenuOpened += OnMenuOpen;
    }

    public static void Restart()
    {
        Dispose();
        Start();
    }

    public static void Dispose()
    {
        Plugin.ContextMenu.OnMenuOpened -= OnMenuOpen;
    }

    private static void OnMenuOpen(IMenuOpenedArgs menuOpenedArgs)
    {
        if (!menuOpenedArgs.IsValidPlayerMenu())
            return;

        if (ServiceContext.ConfigService.GetConfig().ShowOpenInPlayerTrack)
        {
            menuOpenedArgs.AddMenuItem(new MenuItem
            {
                PrefixChar = PrefixChar,
                Name = Language.OpenPlayerTrack,
                OnClicked = OpenPlayerTrack
            });
        }
        if (ServiceContext.ConfigService.GetConfig().ShowOpenLodestone)
        {
            menuOpenedArgs.AddMenuItem(new MenuItem
            {
                PrefixChar = PrefixChar,
                Name = Language.OpenLodestone,
                OnClicked = OpenLodestone
            });
        }
    }

    private static void OpenPlayerTrack(IMenuItemClickedArgs menuItemClickedArgs)
    {
        var selectedPlayer = menuItemClickedArgs.GetPlayer();
        if (selectedPlayer == null)
            return;

        Plugin.GameFramework.RunOnFrameworkThread(() =>
        {
            var currentPlayer = Plugin.ObjectCollection.GetPlayerByContentId(selectedPlayer.ContentId);
            if (currentPlayer != null)
                OnSelectPlayer?.Invoke(currentPlayer, true);
            else
                OnSelectPlayer?.Invoke(selectedPlayer, false);
        });
    }

    private static void OpenLodestone(IMenuItemClickedArgs menuItemClickedArgs)
    {
        var selectedPlayer = menuItemClickedArgs.GetPlayer();
        if (selectedPlayer == null)
            return;

        ServiceContext.LodestoneService.OpenLodestoneProfile(selectedPlayer.Name, selectedPlayer.HomeWorld);
    }
}
