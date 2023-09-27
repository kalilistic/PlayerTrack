using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Main.Presenters;

namespace PlayerTrack.UserInterface.Main.Components;

using Dalamud.Interface.Utility;

public class PlayerEncounterComponent : ViewComponent
{
    private const float SameLineOffset1 = 70f;
    private const float SameLineOffset2 = 160f;
    private const float SameLineOffset3 = 200f;
    private const float SameLineOffset4 = 230f;
    private readonly IMainPresenter presenter;

    public PlayerEncounterComponent(IMainPresenter presenter) => this.presenter = presenter;

    public override void Draw()
    {
        var player = this.presenter.GetSelectedPlayer();
        if (player == null)
        {
            return;
        }

        ImGui.BeginChild("###PlayerSummary_Encounter", new Vector2(-1, 0), false);
        if (player.Encounters.Count == 0)
        {
            LocGui.Text("NoEncountersMessage");
        }
        else
        {
            LocGui.TextColored("Time", ImGuiColors.DalamudViolet);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
            LocGui.TextColored("Duration", ImGuiColors.DalamudViolet);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset2);
            LocGui.TextColored("Job", ImGuiColors.DalamudViolet);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset3);
            LocGui.TextColored("Level", ImGuiColors.DalamudViolet);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset4);
            LocGui.TextColored("Location", ImGuiColors.DalamudViolet);
            foreach (var enc in player.Encounters)
            {
                LocGui.Text(enc.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                LocGui.Text(enc.Duration);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset2);
                LocGui.Text(enc.Job);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset3);
                LocGui.Text(enc.Level);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset4);
                LocGui.Text(enc.Location);
            }
        }

        ImGui.EndChild();
    }
}
