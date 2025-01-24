using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;
using PlayerTrack.Windows.Main.Presenters;

namespace PlayerTrack.Windows.Main.Components;

public class PlayerEncounterComponent : ViewComponent
{
    private const float SameLineOffset1 = 70f;
    private const float SameLineOffset2 = 160f;
    private const float SameLineOffset3 = 200f;
    private const float SameLineOffset4 = 230f;
    private readonly IMainPresenter Presenter;

    public PlayerEncounterComponent(IMainPresenter presenter)
    {
        Presenter = presenter;
    }

    public override void Draw()
    {
        var player = Presenter.GetSelectedPlayer();
        if (player == null)
            return;

        using var child = ImRaii.Child("###PlayerSummary_Encounter", new Vector2(-1, 0), false);
        if (!child.Success)
            return;

        if (player.Encounters.Count == 0)
        {
            ImGui.TextUnformatted(Language.NoEncountersMessage);
        }
        else
        {
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.Time);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.Duration);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset2);
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.Job);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset3);
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.Level);
            ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset4);
            Helper.TextColored(ImGuiColors.DalamudViolet, Language.Location);
            foreach (var enc in player.Encounters)
            {
                ImGui.TextUnformatted(enc.Time);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset1);
                ImGui.TextUnformatted(enc.Duration);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset2);
                ImGui.TextUnformatted(enc.Job);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset3);
                ImGui.TextUnformatted(enc.Level);
                ImGuiHelpers.ScaledRelativeSameLine(SameLineOffset4);
                ImGui.TextUnformatted(enc.Location);
            }
        }
    }
}
