using CheapLoc;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Alerts Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void AlertsConfig()
        {
            var sendNameChangeAlert = this.Plugin.Configuration.SendNameChangeAlert;
            if (ImGui.Checkbox(
                Loc.Localize($"SendNameChangeAlert", "Send name change alert"),
                ref sendNameChangeAlert))
            {
                this.Plugin.Configuration.SendNameChangeAlert = sendNameChangeAlert;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SendNameChangeAlert_HelpMarker",
                                           "send name change alert in chat when detected from lodestone lookup"));
            ImGui.Spacing();

            var sendWorldTransferAlert = this.Plugin.Configuration.SendWorldTransferAlert;
            if (ImGui.Checkbox(
                Loc.Localize($"SendWorldTransferAlert", "Send world transfer alert"),
                ref sendWorldTransferAlert))
            {
                this.Plugin.Configuration.SendWorldTransferAlert = sendWorldTransferAlert;
                this.Plugin.SaveConfig();
            }

            ImGuiComponents.HelpMarker(Loc.Localize(
                                           "SendWorldTransferAlert_HelpMarker",
                                           "send world transfer alert in chat when detected from lodestone lookup"));
            ImGui.Spacing();
        }
    }
}
