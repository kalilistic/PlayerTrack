using System.Diagnostics;

using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Main Menu.
    /// </summary>
    public partial class MainWindow
    {
        private void Menu()
        {
            // settings icon
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(ImGuiColors.White, FontAwesomeIcon.Cog.ToIconString());
            ImGui.PopFont();

            // open settings on left click
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                this.ToggleRightPanel(View.Settings);
            }

            // open settings popup on right click
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup("###PlayerTrack_Menu_Popup");
            }

            // settings menu popup
            if (ImGui.BeginPopup("###PlayerTrack_Menu_Popup"))
            {
                if (ImGui.MenuItem(
                    Loc.Localize("OpenSettings", "Open Settings")))
                {
                    this.ToggleRightPanel(View.Settings);
                }

                if (ImGui.MenuItem(
                    Loc.Localize("OpenLodestoneService", "Open Lodestone")))
                {
                    this.ToggleRightPanel(View.Lodestone);
                }

                if (ImGui.MenuItem(
                    Loc.Localize("AddPlayer", "Add Player")))
                {
                    this.ToggleRightPanel(View.AddPlayer);
                }

                ImGui.Separator();

                if (ImGui.MenuItem(
                    Loc.Localize("OpenCrowdin", "Open Crowdin")))
                {
                    Process.Start("https://crowdin.com/project/playertrack");
                }

                if (ImGui.MenuItem(
                    Loc.Localize("OpenGitHub", "Open GitHub")))
                {
                    Process.Start("https://github.com/kalilistic/playertrack");
                }

                if (ImGui.MenuItem(
                    Loc.Localize("PrintInstructions", "Print Instructions")))
                {
                    this.plugin.PrintHelpMessage();
                }

                ImGui.EndPopup();
            }
        }
    }
}
