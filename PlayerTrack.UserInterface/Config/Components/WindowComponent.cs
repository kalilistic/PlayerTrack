using Dalamud.DrunkenToad.Gui;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Interface.Utility;

public class WindowComponent : ConfigViewComponent
{
    public delegate void WindowConfigComponent_WindowConfigChangedDelegate();

    public event WindowConfigComponent_WindowConfigChangedDelegate? WindowConfigComponent_WindowConfigChanged;

    public override void Draw()
    {
        if (ImGui.BeginTabBar("###Window_TabBar", ImGuiTabBarFlags.None))
        {
            this.DrawGeneralTab();
            this.DrawPlayerListTab();
            this.DrawSettingsTab();
        }
    }

    private void DrawSettingsTab()
    {
        if (LocGui.BeginTabItem("Settings"))
        {
            ImGuiHelpers.ScaledDummy(1f);
            var preserveConfigState = this.config.PreserveConfigState;
            if (ToadGui.Checkbox("PreserveConfigState", ref preserveConfigState))
            {
                this.config.PreserveConfigState = preserveConfigState;
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.WindowConfigComponent_WindowConfigChanged?.Invoke();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawGeneralTab()
    {
        if (LocGui.BeginTabItem("General"))
        {
            ImGuiHelpers.ScaledDummy(1f);
            var isWindowCombined = this.config.IsWindowCombined;
            if (ToadGui.Checkbox("CombineWindows", ref isWindowCombined))
            {
                this.config.IsWindowCombined = isWindowCombined;
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.WindowConfigComponent_WindowConfigChanged?.Invoke();
            }

            var isWindowPositionLocked = this.config.IsWindowPositionLocked;
            if (ToadGui.Checkbox("LockWindowPosition", ref isWindowPositionLocked))
            {
                this.config.IsWindowPositionLocked = isWindowPositionLocked;
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.WindowConfigComponent_WindowConfigChanged?.Invoke();
            }

            var isWindowSizeLocked = this.config.IsWindowSizeLocked;
            if (ToadGui.Checkbox("LockWindowSize", ref isWindowSizeLocked))
            {
                this.config.IsWindowSizeLocked = isWindowSizeLocked;
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.WindowConfigComponent_WindowConfigChanged?.Invoke();
            }

            var onlyShowWindowWhenLoggedIn = this.config.OnlyShowWindowWhenLoggedIn;
            if (ToadGui.Checkbox("OnlyShowLoggedIn", ref onlyShowWindowWhenLoggedIn))
            {
                this.config.OnlyShowWindowWhenLoggedIn = onlyShowWindowWhenLoggedIn;
                ServiceContext.ConfigService.SaveConfig(this.config);
                this.WindowConfigComponent_WindowConfigChanged?.Invoke();
            }

            ImGui.EndTabItem();
        }
    }

    private void DrawPlayerListTab()
    {
        if (LocGui.BeginTabItem("PlayerList"))
        {
            ImGuiHelpers.ScaledDummy(1f);
            var showPlayerFilter = this.config.ShowPlayerFilter;
            if (ToadGui.Checkbox("ShowPlayerFilter", ref showPlayerFilter))
            {
                this.config.ShowPlayerFilter = showPlayerFilter;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            var showSearchBox = this.config.ShowSearchBox;
            if (ToadGui.Checkbox("ShowSearchBox", ref showSearchBox))
            {
                this.config.ShowSearchBox = showSearchBox;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            var searchType = this.config.SearchType;
            if (ToadGui.Combo("SearchType", ref searchType))
            {
                this.config.SearchType = searchType;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            ImGuiHelpers.ScaledDummy(1f);

            var showCategorySeparator = this.config.ShowCategorySeparator;
            if (ToadGui.Checkbox("ShowCategorySeparator", ref showCategorySeparator))
            {
                this.config.ShowCategorySeparator = showCategorySeparator;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            var preserveMainWindowState = this.config.PreserveMainWindowState;
            if (ToadGui.Checkbox("PreserveMainWindowState", ref preserveMainWindowState))
            {
                this.config.PreserveMainWindowState = preserveMainWindowState;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            ImGui.EndTabItem();
        }
    }
}
