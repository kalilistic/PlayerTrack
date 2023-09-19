using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Config.Components;

public class PlayerDefaultsComponent : ConfigViewComponent
{
    public override void Draw()
    {
        if (ImGui.BeginTabBar("###Player_TabBar", ImGuiTabBarFlags.None))
        {
            var playerConfig = PlayerConfigComponent.DrawDefaultConfigTabs();
            if (playerConfig.IsChanged)
            {
                playerConfig.IsChanged = false;
                ServiceContext.ConfigService.SaveConfig(this.config);
                ServiceContext.PlayerDataService.RefreshAllPlayers();
                this.NotifyConfigChanged();
            }
        }

        ImGui.EndTabBar();
    }
}
