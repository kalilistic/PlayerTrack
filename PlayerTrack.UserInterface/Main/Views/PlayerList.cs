using Dalamud.Interface;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Main.Views;

using Dalamud.Interface.Utility;

public class PlayerList : PlayerTrackView, IViewWithPanel
{
    private readonly IMainPresenter presenter;
    private readonly PlayerListComponent playerListComponent;
    private float minimizedWidth;

    public PlayerList(string name, PluginConfig config, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.presenter = presenter;
        this.playerListComponent = new PlayerListComponent(presenter);
        this.playerListComponent.PlayerListComponent_OpenConfig += () => this.OpenConfig?.Invoke();
    }

    public delegate void OpenConfigDelegate();

    public delegate void OpenPanelViewDelegate();

    public event OpenConfigDelegate? OpenConfig;

    public event OpenPanelViewDelegate? OpenPanelView;

    public override void Draw()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var minimizedWidthBase = 221f;
        if (globalScale < 1)
        {
            minimizedWidthBase = (int)(minimizedWidthBase + globalScale * 3.2);
        }
        else if (globalScale > 1.25)
        {
            minimizedWidthBase = (int)(minimizedWidthBase - globalScale * 3.2);
        }

        this.minimizedWidth = minimizedWidthBase * globalScale;
        this.config.MainWindowHeight = ImGui.GetWindowSize().Y;
        this.Size = ImGui.GetWindowSize() with { X = this.minimizedWidth } / ImGuiHelpers.GlobalScale;
        if (this.Size.Value.Y < 100)
        {
            this.Size = this.Size.Value with { Y = 400 };
        }

        this.playerListComponent.Draw();
    }

    public override void Initialize() => this.SetWindowFlags();

    public void RefreshWindowConfig()
    {
        this.config.PanelType = PanelType.None;
        this.presenter.ClosePlayer();
        this.SetWindowFlags();
    }

    public void HidePanel() => this.config.PanelType = PanelType.None;

    public void ShowPanel(PanelType panelType)
    {
        this.config.PanelType = panelType;
        this.OpenPanelView?.Invoke();
        this.IsOpen = true;
    }

    public void TogglePanel(PanelType panelType)
    {
        if (this.config.PanelType == panelType)
        {
            this.HidePanel();
        }
        else
        {
            this.ShowPanel(panelType);
        }
    }
}
