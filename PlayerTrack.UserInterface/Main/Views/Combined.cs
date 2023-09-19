using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Main.Views;

public class Combined : PlayerTrackView, IViewWithPanel
{
    private readonly PlayerListComponent playerListComponent;
    private readonly PanelComponent panelComponent;
    private readonly PlayerComponent playerComponent;
    private readonly AddPlayerComponent addPlayerComponent;
    private readonly IMainPresenter presenter;

    private float minimizedWidth;

    public Combined(string name, PluginConfig config, PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.playerComponent = playerComponent;
        this.addPlayerComponent = addPlayerComponent;
        this.presenter = presenter;
        this.ResetWidth();
        this.playerListComponent = new PlayerListComponent(this.presenter);
        this.playerListComponent.PlayerListComponent_OpenConfig += () => this.OpenConfig?.Invoke();
        this.panelComponent = new PanelComponent(this.playerComponent, this.addPlayerComponent);
    }

    public delegate void OpenConfigDelegate();

    public event OpenConfigDelegate? OpenConfig;

    public override void Draw()
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var minimizedWidthBase = 221f;
        if (globalScale < 1)
        {
            minimizedWidthBase += (float)(globalScale * 3.2);
        }
        else if (globalScale > 1.25)
        {
            minimizedWidthBase -= (float)(globalScale * 3.2);
        }

        this.minimizedWidth = minimizedWidthBase * globalScale;
        var windowSize = ImGui.GetWindowSize();

        if (this.config.PanelType == PanelType.None)
        {
            this.Size = windowSize with { X = this.minimizedWidth } / globalScale;
        }
        else
        {
            this.config.MainWindowWidth = windowSize.X;
            this.Size = windowSize / globalScale;
        }

        if (this.Size.Value.Y < 100)
        {
            this.Size = this.Size.Value with { Y = 400 };
        }

        this.playerListComponent.Draw();
        ImGui.SameLine();
        this.panelComponent.Draw();
    }

    public override void Initialize() => this.SetWindowFlags();

    public void RefreshWindowConfig()
    {
        this.config.PanelType = PanelType.None;
        this.SetMinimizedSize();
        this.presenter.ClosePlayer();
        this.SetWindowFlags();
    }

    public void ShowPanel(PanelType panelType)
    {
        this.Size = new Vector2(this.config.MainWindowWidth, this.config.MainWindowHeight);
        this.config.PanelType = panelType;
        this.IsOpen = true;
    }

    public void HidePanel()
    {
        this.SetMinimizedSize();
        this.config.PanelType = PanelType.None;
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

    private void ResetWidth()
    {
        if (this.config.MainWindowWidth < 222)
        {
            this.config.MainWindowWidth = 700;
        }
    }

    private void SetMinimizedSize() => this.Size = new Vector2(this.minimizedWidth, this.config.MainWindowHeight);
}
