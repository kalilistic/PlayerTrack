using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PlayerTrack.Models;
using PlayerTrack.Windows.Main.Components;
using PlayerTrack.Windows.Main.Presenters;
using PlayerTrack.Windows.Views;

namespace PlayerTrack.Windows.Main.Views;

public class PanelView : PlayerTrackView
{
    private readonly IMainPresenter Presenter;
    private readonly PanelComponent PanelComponent;

    public PanelView(string name, PluginConfig config, PlayerComponent playerComponent, AddPlayerComponent addPlayerComponent, IMainPresenter presenter, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, config, flags)
    {
        Size = new Vector2(730f, 380f);
        Presenter = presenter;
        PanelComponent = new PanelComponent(playerComponent, addPlayerComponent);
    }

    public override bool DrawConditions()
    {
        if (!base.DrawConditions())
            return false;

        if (Config.PanelType == PanelType.None)
            return false;

        return true;
    }

    public override void Draw()
    {
        Size = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
        PanelComponent.Draw();
    }

    public override void OnClose()
    {
        Presenter.ClosePlayer();
        Config.PanelType = PanelType.None;
    }

    public override void Initialize()
    {
        SetWindowFlags();
        ValidateWindowConfig();
    }

    public void ResetWindow()
    {
        Config.PanelType = PanelType.None;
        SetWindowFlags();
    }

    private void ValidateWindowConfig()
    {
        if (!Enum.IsDefined(Config.PanelType))
            Config.PanelType = PanelType.None;

        if (Size.HasValue)
        {
            if (Size.Value.X < 0f || Size.Value.Y < 0f)
                Size = new Vector2(730f, 380f);
        }
        else
        {
            Size = new Vector2(730f, 380f);
        }
    }
}
