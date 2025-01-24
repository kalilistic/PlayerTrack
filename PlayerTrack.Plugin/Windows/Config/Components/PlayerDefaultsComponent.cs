using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Windows.Components;

namespace PlayerTrack.Windows.Config.Components;

public class PlayerDefaultsComponent : ConfigViewComponent
{
    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("###Player_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        var playerConfig = PlayerConfigComponent.DrawDefaultConfigTabs();
        if (playerConfig.IsChanged)
        {
            playerConfig.IsChanged = false;
            ServiceContext.ConfigService.SaveConfig(Config);
            ServiceContext.PlayerDataService.RefreshAllPlayers();
            NotifyConfigChanged();
        }
    }
}
