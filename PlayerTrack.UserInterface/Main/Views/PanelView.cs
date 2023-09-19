using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Main.Components;
using PlayerTrack.UserInterface.Main.Presenters;
using PlayerTrack.UserInterface.Views;

namespace PlayerTrack.UserInterface.Main.Views;

public class PanelView : PlayerTrackView
{
    private readonly IMainPresenter presenter;
    private readonly PanelComponent panelComponent;

    public PanelView(string name, PluginConfig config, PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        : base(name, config, flags)
    {
        this.Size = new Vector2(730f, 380f);
        this.presenter = presenter;
        this.panelComponent = new PanelComponent(playerComponent, addPlayerComponent);
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

    public override void Initialize() => this.SetWindowFlags();

    public void ResetWindow()
    {
        this.config.PanelType = PanelType.None;
        this.SetWindowFlags();
    }
}
