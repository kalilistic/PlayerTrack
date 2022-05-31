using CheapLoc;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Context Menu Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void ContextMenuConfig()
        {
            var showAddShowInfoContextMenu = this.Plugin.Configuration.ShowAddShowInfoContextMenu;
            if (ImGui.Checkbox(
                Loc.Localize($"ShowAddShowInfoContextMenu", "Show add/show info option"),
                ref showAddShowInfoContextMenu))
            {
                this.Plugin.Configuration.ShowAddShowInfoContextMenu = showAddShowInfoContextMenu;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowAddShowInfoContextMenu_HelpMarker",
                                           "show add/show info context menu on players"));
            ImGui.Spacing();

            var showOpenLodestoneContextMenu = this.Plugin.Configuration.ShowOpenLodestoneContextMenu;
            if (ImGui.Checkbox(
                Loc.Localize($"ShowOpenLodestoneContextMenu", "Show open lodestone profile option"),
                ref showOpenLodestoneContextMenu))
            {
                this.Plugin.Configuration.ShowOpenLodestoneContextMenu = showOpenLodestoneContextMenu;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowOpenLodestoneContextMenu_HelpMarker",
                                           "show open lodestone context menu on players"));
            ImGui.Spacing();
        }
    }
}
