using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class WindowComponent : ConfigViewComponent
{
    public delegate void WindowConfigComponent_WindowConfigChangedDelegate();

    public event WindowConfigComponent_WindowConfigChangedDelegate? WindowConfigComponent_WindowConfigChanged;

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("###Window_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        DrawGeneralTab();
        DrawPlayerListTab();
        DrawSettingsTab();
    }

    private void DrawSettingsTab()
    {
        using var tabItem = ImRaii.TabItem(Language.Settings);
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);
        var preserveConfigState = Config.PreserveConfigState;
        if (Helper.Checkbox(Language.PreserveConfigState, ref preserveConfigState))
        {
            Config.PreserveConfigState = preserveConfigState;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }
    }

    private void DrawGeneralTab()
    {
        using var tabItem = ImRaii.TabItem(Language.General);
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);
        var isWindowCombined = Config.IsWindowCombined;
        if (Helper.Checkbox(Language.CombineWindows, ref isWindowCombined))
        {
            Config.IsWindowCombined = isWindowCombined;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }

        var isWindowPositionLocked = Config.IsWindowPositionLocked;
        if (Helper.Checkbox(Language.LockWindowPosition, ref isWindowPositionLocked))
        {
            Config.IsWindowPositionLocked = isWindowPositionLocked;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }

        var isWindowSizeLocked = Config.IsWindowSizeLocked;
        if (Helper.Checkbox(Language.LockWindowSize, ref isWindowSizeLocked))
        {
            Config.IsWindowSizeLocked = isWindowSizeLocked;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }

        var onlyShowWindowWhenLoggedIn = Config.OnlyShowWindowWhenLoggedIn;
        if (Helper.Checkbox(Language.OnlyShowLoggedIn, ref onlyShowWindowWhenLoggedIn))
        {
            Config.OnlyShowWindowWhenLoggedIn = onlyShowWindowWhenLoggedIn;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }

        var useCtrlNewLine = Config.UseCtrlNewLine;
        if (Helper.Checkbox(Language.OptionCtrlNewline, ref useCtrlNewLine))
        {
            Config.UseCtrlNewLine = useCtrlNewLine;
            ServiceContext.ConfigService.SaveConfig(Config);
            WindowConfigComponent_WindowConfigChanged?.Invoke();
        }
    }

    private void DrawPlayerListTab()
    {
        using var tabItem = ImRaii.TabItem(Language.PlayerList);
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);
        var showPlayerFilter = Config.ShowPlayerFilter;
        if (Helper.Checkbox(Language.ShowPlayerFilter, ref showPlayerFilter))
        {
            Config.ShowPlayerFilter = showPlayerFilter;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var showPlayerCountInFilter = Config.ShowPlayerCountInFilter;
        if (Helper.Checkbox(Language.ShowPlayerCountInFilter, ref showPlayerCountInFilter))
        {
            Config.ShowPlayerCountInFilter = showPlayerCountInFilter;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var showSearchBox = Config.ShowSearchBox;
        if (Helper.Checkbox(Language.ShowSearchBox, ref showSearchBox))
        {
            Config.ShowSearchBox = showSearchBox;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var searchType = Config.SearchType;
        if (Helper.Combo(Language.SearchType, ref searchType))
        {
            Config.SearchType = searchType;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        ImGuiHelpers.ScaledDummy(1f);

        var showCategorySeparator = Config.ShowCategorySeparator;
        if (Helper.Checkbox(Language.ShowCategorySeparator, ref showCategorySeparator))
        {
            Config.ShowCategorySeparator = showCategorySeparator;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var preserveMainWindowState = Config.PreserveMainWindowState;
        if (Helper.Checkbox(Language.PreserveMainWindowState, ref preserveMainWindowState))
        {
            Config.PreserveMainWindowState = preserveMainWindowState;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var recentPlayersThreshold = Config.RecentPlayersThreshold / 60000;
        ImGui.SetNextItemWidth(85f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputInt(Language.RecentPlayersThreshold, ref recentPlayersThreshold, 1, 5))
        {
            Config.RecentPlayersThreshold = recentPlayersThreshold * 60000;
            ServiceContext.ConfigService.SaveConfig(Config);
        }
    }
}
