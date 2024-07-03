using Dalamud.DrunkenToad.Gui;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Interface.Utility;

public class IntegrationComponent : ConfigViewComponent
{
    public override void Draw()
    {
        if (ImGui.BeginTabBar("###Integration_TabBar", ImGuiTabBarFlags.None))
        {
            if (LocGui.BeginTabItem("Lodestone"))
            {
                this.DrawLodestoneTab();
                ImGui.EndTabItem();
            }

            if (LocGui.BeginTabItem("Visibility"))
            {
                this.DrawVisibilityTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawLodestoneTab()
    {
        var lodestoneLocale = this.config.LodestoneLocale;
        if (ToadGui.Combo("LodestoneLocale", ref lodestoneLocale, 60))
        {
            this.config.LodestoneLocale = lodestoneLocale;
            ServiceContext.ConfigService.SaveConfig(this.config);
        }
    }

    private void DrawVisibilityTab()
    {
        ImGuiHelpers.ScaledDummy(1f);
        var syncWithVisibility = this.config.SyncWithVisibility;
        if (ToadGui.Checkbox("SyncWithVisibility", ref syncWithVisibility))
        {
            this.config.SyncWithVisibility = syncWithVisibility;
            ServiceContext.ConfigService.SaveConfig(this.config);
        }
    }
}
