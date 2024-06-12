using System;
using System.Numerics;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Main.Views;

using Dalamud.Interface.Utility;

public class PanelView : PlayerTrackView
{
    private readonly IMainPresenter presenter;
    private readonly PanelComponent panelComponent;

    public PanelView(string name, PluginConfig config, PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, LodestoneServiceComponent lodestoneServiceComponent, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.Size = new Vector2(730f, 380f);
        this.presenter = presenter;
        this.panelComponent = new PanelComponent(playerComponent, addPlayerComponent, lodestoneServiceComponent);
    }

    public override bool DrawConditions()
    {
        if (!base.DrawConditions())
        {
            return false;
        }

        if (this.config.PanelType == PanelType.None)
        {
            return false;
        }

        return true;
    }

    public override void Draw()
    {
        this.Size = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
        this.panelComponent.Draw();
    }

    public override void OnClose()
    {
        this.presenter.ClosePlayer();
        this.config.PanelType = PanelType.None;
    }

    public override void Initialize()
    {
        this.SetWindowFlags();
        this.ValidateWindowConfig();
    }

    public void ResetWindow()
    {
        this.config.PanelType = PanelType.None;
        this.SetWindowFlags();
    }
    
    private void ValidateWindowConfig()
    {
        if (!Enum.IsDefined(typeof(PanelType), this.config.PanelType))
        {
            this.config.PanelType = PanelType.None;
        }

        if (this.Size.HasValue)
        {
            if (this.Size.Value.X < 0f || this.Size.Value.Y < 0f)
            {
                this.Size = new Vector2(730f, 380f);
            }
        }
        else
        {
            this.Size = new Vector2(730f, 380f);
        }
    }
}
