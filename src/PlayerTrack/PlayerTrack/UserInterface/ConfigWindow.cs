using System.Collections.Generic;
using System.Numerics;

using CheapLoc;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Config window for the plugin.
    /// </summary>
    public partial class ConfigWindow : PluginWindow
    {
        private readonly PlayerTrackPlugin plugin;
        private readonly List<Vector4> colorPalette = ImGuiHelpers.DefaultColorPalette(36);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigWindow"/> class.
        /// </summary>
        /// <param name="plugin">PlayerTrack plugin.</param>
        public ConfigWindow(PlayerTrackPlugin plugin)
            : base(plugin, "PlayerTrack Config")
        {
            this.plugin = plugin;
            this.Size = new Vector2(750f, 300f);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (ImGui.BeginTabBar("###PlayerTrack_Config_TabBar", ImGuiTabBarFlags.None))
            {
                if (ImGui.BeginTabItem(Loc.Localize("DisplayConfig", "Display")))
                {
                    WindowManager.SpacerWithTabs();
                    this.DisplayConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("ProcessingConfig", "Processing")))
                {
                    WindowManager.SpacerWithTabs();
                    this.ProcessingConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("IconConfig", "Icons")))
                {
                    WindowManager.SpacerWithTabs();
                    this.IconConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("NamePlateConfig", "NamePlates")))
                {
                    WindowManager.SpacerWithTabs();
                    this.NamePlateConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("ContextMenuConfig", "ContextMenu")))
                {
                    WindowManager.SpacerWithTabs();
                    this.ContextMenuConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("AlertsConfig", "Alerts")))
                {
                    WindowManager.SpacerWithTabs();
                    this.AlertsConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("LodestoneConfig", "Lodestone")))
                {
                    WindowManager.SpacerWithTabs();
                    this.LodestoneConfig();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("CategoryConfig", "Categories")))
                {
                    WindowManager.SpacerWithTabs();
                    this.CategoryConfig();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.Spacing();
        }
    }
}
