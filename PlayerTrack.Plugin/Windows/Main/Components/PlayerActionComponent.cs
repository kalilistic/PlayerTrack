using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;
using PlayerTrack.Windows.Main.Presenters;

namespace PlayerTrack.Windows.Main.Components;

public class PlayerActionComponent : ViewComponent
{
    private readonly IMainPresenter Presenter;

    public PlayerActionComponent(IMainPresenter presenter)
    {
        Presenter = presenter;
    }

    public override void Draw()
    {
        var player = Presenter.GetSelectedPlayer();
        if (player == null)
            return;

        using var child = ImRaii.Child("###PlayerAction", new Vector2(-1, 0), false);
        if (!child.Success)
            return;

        var buttonSize = ImGuiHelpers.ScaledVector2(120f, 25f);
        ImGuiHelpers.ScaledDummy(1f);
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());

            ImGui.SameLine();
            ImGui.TextUnformatted(Language.WarningZone);
        }

        ImGuiHelpers.ScaledDummy(1f);

        if (ImGui.Button(Language.Reset, buttonSize))
        {
            PlayerConfigService.ResetPlayerConfig(player.Id);
            PlayerCategoryService.UnassignCategoriesFromPlayer(player.Id);
            PlayerTagService.UnassignTagsFromPlayer(player.Id);
            Presenter.ClosePlayer();
            Presenter.HidePanel();
        }

        if (ImGui.Button(Language.Delete, buttonSize))
        {
            ServiceContext.PlayerDataService.DeletePlayer(player.Id);
            Presenter.ClosePlayer();
            Presenter.HidePanel();
        }

        if (ImGui.Button(Language.DeleteHistory, buttonSize))
        {
            ServiceContext.PlayerDataService.DeleteHistory(player.Id);
            Presenter.ClosePlayer();
            Presenter.HidePanel();
        }
    }
}
