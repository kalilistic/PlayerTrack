using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class IntegrationComponent : ConfigViewComponent
{
    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("###Integration_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        using (var tabItem = ImRaii.TabItem(Language.Lodestone))
        {
            if (tabItem.Success)
                DrawLodestoneTab();
        }

        using (var tabItem = ImRaii.TabItem(Language.Visibility))
        {
            if (tabItem.Success)
                DrawVisibilityTab();
        }
    }

    private void DrawLodestoneTab()
    {
        var lodestoneLocale = Config.LodestoneLocale;
        if (Helper.Combo(Language.LodestoneLocale, ref lodestoneLocale, 60))
        {
            Config.LodestoneLocale = lodestoneLocale;
            ServiceContext.ConfigService.SaveConfig(Config);
        }
    }

    private void DrawVisibilityTab()
    {
        ImGuiHelpers.ScaledDummy(1f);
        var syncWithVisibility = Config.SyncWithVisibility;
        if (Helper.Checkbox(Language.SyncWithVisibility, ref syncWithVisibility))
        {
            Config.SyncWithVisibility = syncWithVisibility;
            ServiceContext.ConfigService.SaveConfig(Config);
        }
    }
}
