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

public class Combined : PlayerTrackView, IViewWithPanel
{
    private readonly PlayerListComponent playerListComponent;
    private readonly PanelComponent panelComponent;
    private readonly IMainPresenter presenter;
    private bool isPendingSizeUpdate;
    private Vector2 lastSize;

    public Combined(string name, PluginConfig config, PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, LodestoneComponent lodestoneComponent, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.presenter = presenter;
        this.playerListComponent = new PlayerListComponent(this.presenter);
        this.playerListComponent.PlayerListComponent_OpenConfig += () => this.OpenConfig?.Invoke();
        this.panelComponent = new PanelComponent(playerComponent, addPlayerComponent, lodestoneComponent);
    }

    public delegate void OpenConfigDelegate();

    public event OpenConfigDelegate? OpenConfig;

    public override void Draw()
    {
        this.UpdateWindowSizes();
        this.CheckResize();
        this.playerListComponent.Draw();
        ImGui.SameLine();
        this.panelComponent.Draw();
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

    public void ShowPanel(PanelType panelType)
    {
        this.config.PanelType = panelType;
        this.IsOpen = true;
        this.isPendingSizeUpdate = true;
    }

    public void HidePanel()
    {
        this.config.PanelType = PanelType.None;
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
        if (ImGui.GetWindowSize() != this.lastSize || this.isPendingSizeUpdate)
        {
            this.lastSize = ImGui.GetWindowSize();
            if (this.config.PanelType != PanelType.None)
            {
                this.config.MainWindowWidth = this.lastSize.X / ImGuiHelpers.GlobalScale;
            }

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
        if (this.config.PanelType != PanelType.None)
        {
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(221f, 120f),
                MaximumSize = new Vector2(1000f, 1000f),
            };
            this.Size = new Vector2(this.config.MainWindowWidth, this.config.MainWindowHeight);
            this.SizeCondition = ImGuiCond.Always;
        }
        else
        {
            this.config.PanelType = PanelType.None;
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(221f, 120f),
                MaximumSize = new Vector2(221f, 1000f),
            };
            this.Size = new Vector2(221f, this.config.MainWindowHeight);
            this.SizeCondition = ImGuiCond.Always;
        }
    }

    private void ValidateWindowConfig()
    {
        if (!Enum.IsDefined(typeof(PanelType), this.config.PanelType))
        {
            this.config.PanelType = PanelType.None;
        }

        if (this.config.MainWindowWidth is < 221f or > 1000f)
        {
            this.config.MainWindowWidth = 221f;
        }

        if (this.config.MainWindowHeight is < 120f or > 1000f)
        {
            this.config.MainWindowHeight = 120f;
        }
    }
}
