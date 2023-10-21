using System;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Main.Views;

using System.Numerics;
using Dalamud.Interface.Utility;
using Domain;

public class PlayerList : PlayerTrackView, IViewWithPanel
{
    private readonly IMainPresenter presenter;
    private readonly PlayerListComponent playerListComponent;
    private bool isPendingSizeUpdate;
    private Vector2 lastSize;

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
        this.CheckResize();
        this.UpdateWindowSizes();
        this.playerListComponent.Draw();
    }

    public override void Initialize()
    {
        this.SetWindowFlags();
        this.isPendingSizeUpdate = true;
        this.UpdateWindowSizes();
        this.ValidateWindowConfig();
    }

    public void RefreshWindowConfig()
    {
        this.config.PanelType = PanelType.None;
        this.presenter.ClosePlayer();
        this.SetWindowFlags();
    }

    public void HidePanel()
    {
        this.config.PanelType = PanelType.None;
        this.isPendingSizeUpdate = true;
    }

    public void ShowPanel(PanelType panelType)
    {
        this.config.PanelType = panelType;
        this.OpenPanelView?.Invoke();
        this.IsOpen = true;
        this.isPendingSizeUpdate = true;
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

    private void CheckResize()
    {
        if (ImGui.GetWindowSize() != this.lastSize)
        {
            this.lastSize = ImGui.GetWindowSize();
            this.config.MainWindowHeight = this.lastSize.Y / ImGuiHelpers.GlobalScale;
            ServiceContext.ConfigService.SaveConfig(this.config);
        }
    }

    private void UpdateWindowSizes()
    {
        if (!this.isPendingSizeUpdate)
        {
            this.SizeCondition = ImGuiCond.FirstUseEver;
            return;
        }

        this.isPendingSizeUpdate = false;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(221f, 120f),
            MaximumSize = new Vector2(221f, 1000f),
        };
        this.Size = new Vector2(221f, this.config.MainWindowHeight);
        this.SizeCondition = ImGuiCond.Always;
    }
    
    private void ValidateWindowConfig()
    {
        if (!Enum.IsDefined(typeof(PanelType), this.config.PanelType))
        {
            this.config.PanelType = PanelType.None;
        }

        if (this.config.MainWindowHeight is < 120f or > 1000f)
        {
            this.config.MainWindowHeight = 120f;
        }
    }
}
