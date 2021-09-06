using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Lodestone Queue View.
    /// </summary>
    public partial class Panel
    {
        private void Lodestone()
        {
            const float sameLineOffset = 140f;
            var isLodestoneAvailable = this.plugin.LodestoneService.IsLodestoneAvailable();
            var requests = this.plugin.LodestoneService.GetRequests();

            // heading
            WindowManager.SpacerNoTabs();
            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("Lodestone", "Lodestone"));

            // lodestone state
            ImGui.Text(Loc.Localize("LodestoneStatus", "Status"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            if (isLodestoneAvailable)
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, Loc.Localize("LodestoneAvailable", "Available"));
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DPSRed, Loc.Localize("LodestoneUnavailable", "Unavailable"));
            }

            // total requests
            ImGui.Text(Loc.Localize("LodestoneTotalRequests", "Request Count"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            ImGui.Text(requests.Length.ToString());

            // requests
            ImGuiHelpers.ScaledDummy(new Vector2(0, 5f));
            ImGui.TextColored(ImGuiColors2.ToadViolet, Loc.Localize("LodestoneRequestsInQueue", "Requests In Queue"));
            if (requests.Any())
            {
                foreach (var request in requests)
                {
                    ImGui.Text(request.PlayerName + " (" + request.WorldName + ")");
                }
            }
            else
            {
                ImGui.Text(Loc.Localize("LodestoneNoRequests", "There are no pending lodestone requests."));
            }
        }
    }
}
