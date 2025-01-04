using System.Numerics;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;
using PlayerTrack.UserInterface.Main.Presenters;

namespace PlayerTrack.UserInterface.Main.Components;

using Dalamud.Interface.Utility;

public class PlayerComponent(IMainPresenter presenter)
{
    private readonly PlayerSummaryComponent playerSummaryComponent = new(presenter);
    private readonly PlayerEncounterComponent playerEncounterComponent = new(presenter);
    private readonly PlayerHistoryComponent playerHistoryComponent = new(presenter);
    private readonly PlayerActionComponent playerActionComponent = new(presenter);

    public void Draw()
    {
        var player = presenter.GetSelectedPlayer();
        var isLoadingPlayer = presenter.IsPlayerLoading();
        if (player == null)
        {
            return;
        }

        ImGui.BeginChild("###ConfigMenuOption_Player", new Vector2(-1, 0), false);
        ImGui.BeginDisabled(isLoadingPlayer);
        if (ImGui.BeginTabBar("###PlayerSummary_TabBar", ImGuiTabBarFlags.None))
        {
            if (LocGui.BeginTabItem("Players"))
            {
                this.playerSummaryComponent.Draw();
                ImGui.EndTabItem();
            }

            if (LocGui.BeginTabItem("Encounters"))
            {
                this.playerEncounterComponent.Draw();
                ImGui.EndTabItem();
            }
            
            if (LocGui.BeginTabItem("History"))
            {
                this.playerHistoryComponent.Draw();
                ImGui.EndTabItem();
            }

            if (LocGui.BeginTabItem("Settings"))
            {
                ImGuiHelpers.ScaledDummy(3f);
                ImGuiHelpers.ScaledIndent(6f);
                if (ImGui.BeginTabBar("###PlayerConfigTabBar", ImGuiTabBarFlags.None))
                {
                    player.PlayerConfig.PlayerConfigType = PlayerConfigType.Player;
                    player.PlayerConfig = PlayerConfigComponent.DrawPlayerConfigTabs(player);
                    if (player.PlayerConfig.IsChanged)
                    {
                        player.PlayerConfig.IsChanged = false;
                        PlayerConfigService.UpdateConfig(player.Id, player.PlayerConfig);
                    }
                }

                ImGui.EndTabBar();
                ImGui.EndTabItem();
            }

            if (LocGui.BeginTabItem("Actions"))
            {
                this.playerActionComponent.Draw();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.EndDisabled();
        ImGui.EndChild();
    }
}
