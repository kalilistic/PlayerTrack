using PlayerTrack.Models;

namespace PlayerTrack.Windows.Main.Views;

public interface IViewWithPanel
{
    void TogglePanel(PanelType panelType);

    void HidePanel();

    void ShowPanel(PanelType panelType);
}
