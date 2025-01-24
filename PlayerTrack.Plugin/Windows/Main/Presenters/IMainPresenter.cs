using System.Collections.Generic;
using PlayerTrack.Models;
using PlayerTrack.Windows.ViewModels;

namespace PlayerTrack.Windows.Main.Presenters;

public interface IMainPresenter
{
    PlayerView? GetSelectedPlayer();

    void ClosePlayer();

    bool IsPlayerLoading();

    int GetPlayersCount();

    List<Player> GetPlayers(int displayStart, int displayEnd);

    void TogglePanel(PanelType panelType);

    void HidePanel();

    void SelectPlayer(Player player);

    void ShowPanel(PanelType panelType);

    void ClearCache();

    void OpenConfig(ConfigMenuOption configMenuOption);
}
