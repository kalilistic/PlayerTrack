using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;
using PlayerTrack.Windows.Main.Presenters;

namespace PlayerTrack.Windows.Main.Components;

public class PlayerHistoryComponent(IMainPresenter presenter) : ViewComponent
{
    private const float SameLineOffset1 = 120f;

    public override void Draw()
    {
        var player = presenter.GetSelectedPlayer();
        if (player == null)
            return;

        using var child = ImRaii.Child("###PlayerSummary_History", new Vector2(-1, 0), false);
        if (!child.Success)
            return;

        // Player Name History
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Time);
        ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.NameWorld);
        if (player.PlayerNameWorldHistories.Count == 0)
        {
            ImGui.TextUnformatted(Language.NoHistoryMessage);
        }
        else
        {
            foreach (var ph in player.PlayerNameWorldHistories)
            {
                ImGui.TextUnformatted(ph.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                ImGui.TextUnformatted(ph.NameWorld);
            }
        }

        // Player Appearance History
        ImGuiHelpers.ScaledDummy(new Vector2(0, 10));
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Time);
        ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
        Helper.TextColored(ImGuiColors.DalamudViolet, Language.Appearance);
        if (player.PlayerCustomizeHistories.Count == 0)
        {
            ImGui.TextUnformatted(Language.NoHistoryMessage);
        }
        else
        {
            foreach (var ph in player.PlayerCustomizeHistories)
            {
                ImGui.TextUnformatted(ph.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                ImGui.TextUnformatted(ph.Appearance);
            }
        }
    }
}
