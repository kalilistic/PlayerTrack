using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Search.
    /// </summary>
    public partial class MainWindow
    {
        private string searchInput = string.Empty;

        private void SearchBox()
        {
            ImGui.SetNextItemWidth(175 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputTextWithHint(
                "###PlayerTrack_SearchBox_Input",
                Loc.Localize("SearchHint", "search"),
                ref this.searchInput,
                30))
            {
                this.lastPlayerListRefresh = DateUtil.CurrentTime();
            }

            ImGui.SameLine();
        }
    }
}
