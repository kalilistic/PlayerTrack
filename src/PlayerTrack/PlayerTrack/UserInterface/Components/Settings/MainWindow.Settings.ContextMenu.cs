using System;
using System.Linq;

using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Context Menu Settings.
    /// </summary>
    public partial class MainWindow
    {
        private int currentInternalAction;

        private void ContextMenuSettings()
        {
            var showAddShowInfoContextMenu = this.Plugin.Configuration.ShowAddShowInfoContextMenu;
            if (ImGui.Checkbox(
                Loc.Localize($"ShowContextMenu", "Show add/show info option"),
                ref showAddShowInfoContextMenu))
            {
                this.Plugin.Configuration.ShowAddShowInfoContextMenu = showAddShowInfoContextMenu;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowContextMenu_HelpMarker",
                                           "show playertrack submenu on players"));
            ImGui.Spacing();

            var showOpenLodestoneContextMenu = this.Plugin.Configuration.ShowOpenLodestoneContextMenu;
            if (ImGui.Checkbox(
                Loc.Localize($"ShowContextMenu", "Show open lodestone profile option"),
                ref showOpenLodestoneContextMenu))
            {
                this.Plugin.Configuration.ShowOpenLodestoneContextMenu = showOpenLodestoneContextMenu;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowContextMenu_HelpMarker",
                                           "show playertrack submenu on players"));
            ImGui.Spacing();

            ImGui.Text(Loc.Localize("ShowContextPosition", "Set context menu item position"));
            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "ShowContextPosition_HelpMarker",
                                           "show above or below the first matching item in the menu."));

            var names = Enum.GetNames(typeof(InternalAction));
            var values = Enum.GetValues(typeof(InternalAction)).Cast<byte>().ToArray();
            ImGui.SetNextItemWidth(200f * ImGuiHelpers.GlobalScale);
            ImGui.Combo("###PlayerTrack_ShowContext_Combo", ref this.currentInternalAction, names, names.Length);
            ImGui.SameLine();

            if (ImGui.SmallButton(Loc.Localize("ShowAbove", "Show Above") + "###PlayerTrack_ContextAbove_Button"))
            {
                if (this.plugin.Configuration.ShowContextAboveThis.Contains(
                    values[this.currentInternalAction]) || this.plugin.Configuration.ShowContextBelowThis.Contains(
                        values[this.currentInternalAction]))
                {
                    ImGui.OpenPopup("###PlayerTrack_DupeContext_Popup");
                }
                else
                {
                    this.plugin.Configuration.ShowContextAboveThis.Add(
                        values[this.currentInternalAction]);
                    this.plugin.SaveConfig();
                }
            }

            ImGui.SameLine();

            if (ImGui.SmallButton(Loc.Localize("ShowBelow", "Show Below") + "###PlayerTrack_ContextBelow_Button"))
            {
                if (this.plugin.Configuration.ShowContextAboveThis.Contains(
                        values[this.currentInternalAction]) || this.plugin.Configuration.ShowContextBelowThis.Contains(
                        values[this.currentInternalAction]))
                {
                    ImGui.OpenPopup("###PlayerTrack_DupeContext_Popup");
                }
                else
                {
                    this.plugin.Configuration.ShowContextBelowThis.Add(
                        values[this.currentInternalAction]);
                    this.plugin.SaveConfig();
                }
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("ShowAboveList", "Show Above"));
            if (this.plugin.Configuration.ShowContextAboveThis.Count > 0)
            {
                foreach (var contextAbove in this.plugin.Configuration.ShowContextAboveThis.ToList())
                {
                    var index = Array.IndexOf(values, contextAbove);
                    ImGui.Text(names[index]);
                    if (ImGui.IsItemClicked())
                    {
                        this.plugin.Configuration.ShowContextAboveThis.Remove(contextAbove);
                        this.plugin.SaveConfig();
                    }
                }
            }
            else
            {
                ImGui.Text(Loc.Localize("NoContextItems", "None"));
            }

            ImGui.Spacing();
            ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("ShowBelowList", "Show Below"));
            if (this.plugin.Configuration.ShowContextBelowThis.Count > 0)
            {
                foreach (var contextBelow in this.plugin.Configuration.ShowContextBelowThis.ToList())
                {
                    var index = Array.IndexOf(values, contextBelow);
                    ImGui.Text(names[index]);
                    if (ImGui.IsItemClicked())
                    {
                        this.plugin.Configuration.ShowContextBelowThis.Remove(contextBelow);
                        this.plugin.SaveConfig();
                    }
                }
            }
            else
            {
                ImGui.Text(Loc.Localize("NoContextItems", "None"));
            }

            if (ImGui.BeginPopup("###PlayerTrack_DupeContext_Popup"))
            {
                ImGui.Text(Loc.Localize("DupeContextBelow", "This context item is already added!"));
                ImGui.EndPopup();
            }
        }
    }
}
