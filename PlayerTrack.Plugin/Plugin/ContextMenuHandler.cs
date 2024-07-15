using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Game.Gui.ContextMenu;
using PlayerTrack.Domain;

// ReSharper disable ConvertSwitchStatementToSwitchExpression
#pragma warning disable CS0067 // Event is never used
namespace PlayerTrack.Plugin;

public static class ContextMenuHandler
{
    private const char PrefixChar = 'P';
    public delegate void SelectPlayerDelegate(ToadPlayer player, bool isCurrent);
    public static event SelectPlayerDelegate? SelectPlayer;
    public static void Start()
    {
        DalamudContext.ContextMenuHandler.OnMenuOpened += OnMenuOpen;
    }

    private static void OnMenuOpen(IMenuOpenedArgs menuOpenedArgs)
    {
        if (!menuOpenedArgs.IsValidPlayerMenu()) return;
        if (ServiceContext.ConfigService.GetConfig().ShowOpenInPlayerTrack)
        {
            menuOpenedArgs.AddMenuItem(new MenuItem
            {
                PrefixChar = PrefixChar,
                Name = DalamudContext.LocManager.GetString("OpenPlayerTrack"),
                OnClicked = OpenPlayerTrack
            });
        }
        if (ServiceContext.ConfigService.GetConfig().ShowOpenLodestone)
        {
            menuOpenedArgs.AddMenuItem(new MenuItem
            {
                PrefixChar = PrefixChar,
                Name = DalamudContext.LocManager.GetString("OpenLodestone"),
                OnClicked = OpenLodestone
            });
        }
    }

    private static void OpenPlayerTrack(IMenuItemClickedArgs menuItemClickedArgs)
    {
        var selectedPlayer = menuItemClickedArgs.GetPlayer();
        if (selectedPlayer == null) return;
        DalamudContext.GameFramework.RunOnFrameworkThread(() =>
        {
            var currentPlayer = DalamudContext.ObjectCollection.GetPlayerByContentId(selectedPlayer.ContentId);
            if (currentPlayer != null)
            {
                SelectPlayer?.Invoke(currentPlayer, true);
            }
            else
            {
                SelectPlayer?.Invoke(selectedPlayer, false);
            }
        });
    }
    
    private static void OpenLodestone(IMenuItemClickedArgs menuItemClickedArgs)
    {
        var selectedPlayer = menuItemClickedArgs.GetPlayer();
        if (selectedPlayer == null) return;
        _ = ServiceContext.LodestoneService.OpenLodestoneProfile(selectedPlayer.Name, selectedPlayer.HomeWorld);
    }

    public static void Restart()
    {
        Dispose();
        Start();
    }
    
    public static void Dispose()
    {
        DalamudContext.ContextMenuHandler.OnMenuOpened -= OnMenuOpen;
    }
}