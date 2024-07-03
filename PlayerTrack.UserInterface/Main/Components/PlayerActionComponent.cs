using System.Numerics;
using Dalamud.Interface;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Main.Presenters;

namespace PlayerTrack.UserInterface.Main.Components;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;

public class PlayerActionComponent : ViewComponent
{
    private readonly IMainPresenter presenter;

    public PlayerActionComponent(IMainPresenter presenter) => this.presenter = presenter;

    public override void Draw()
    {
        var player = this.presenter.GetSelectedPlayer();
        if (player == null)
        {
            return;
        }

        ImGui.BeginChild("###PlayerAction", new Vector2(-1, 0), false);
        var buttonSize = ImGuiHelpers.ScaledVector2(120f, 25f);

        ImGuiHelpers.ScaledDummy(1f);
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
        ImGui.Text(FontAwesomeIcon.ExclamationTriangle.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        LocGui.Text("WarningZone");
        ImGui.PopStyleColor(1);
        ImGuiHelpers.ScaledDummy(1f);

        if (LocGui.Button("Reset", buttonSize))
        {
            PlayerConfigService.ResetPlayerConfig(player.Id);
            PlayerCategoryService.UnassignCategoriesFromPlayer(player.Id);
            PlayerTagService.UnassignTagsFromPlayer(player.Id);
            this.presenter.ClosePlayer();
            this.presenter.HidePanel();
        }

        if (LocGui.Button("Delete", buttonSize))
        {
            ServiceContext.PlayerDataService.DeletePlayer(player.Id);
            this.presenter.ClosePlayer();
            this.presenter.HidePanel();
        }
        
        if (LocGui.Button("DeleteHistory", buttonSize))
        {
            ServiceContext.PlayerDataService.DeleteHistory(player.Id);
            this.presenter.ClosePlayer();
            this.presenter.HidePanel();
        }

        ImGui.EndChild();
    }
}
