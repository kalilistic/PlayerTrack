using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Main.Presenters;

namespace PlayerTrack.UserInterface.Main.Components;

using Dalamud.Interface.Utility;

public class PlayerHistoryComponent(IMainPresenter presenter) : ViewComponent
{
    private const float SameLineOffset1 = 120f;

    public override void Draw()
    {
        var player = presenter.GetSelectedPlayer();
        if (player == null)
        {
            return;
        }

        ImGui.BeginChild("###PlayerSummary_History", new Vector2(-1, 0), false);
        
        // Player Name History
        LocGui.TextColored("Time", ImGuiColors.DalamudViolet);
        ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
        LocGui.TextColored("Name@World", ImGuiColors.DalamudViolet);
        if (player.PlayerNameWorldHistories.Count == 0)
        {
            LocGui.Text("NoHistoryMessage");
        }
        else
        {
            foreach (var ph in player.PlayerNameWorldHistories)
            {
                LocGui.Text(ph.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                LocGui.Text(ph.NameWorld);
            }
        }
        
        // Player Appearance History
        ImGuiHelpers.ScaledDummy(new Vector2(0, 10));
        LocGui.TextColored("Time", ImGuiColors.DalamudViolet);
        ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
        LocGui.TextColored("Appearance", ImGuiColors.DalamudViolet);
        if (player.PlayerCustomizeHistories.Count == 0)
        {
            LocGui.Text("NoHistoryMessage");
        }
        else
        {
            foreach (var ph in player.PlayerCustomizeHistories)
            {
                LocGui.Text(ph.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                LocGui.Text(ph.Appearance);
            }
        }

        ImGui.EndChild();
    }
}
