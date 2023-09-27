using System.Collections.Generic;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Config.Components;

using System.Linq;
using Dalamud.Interface.Utility;

public class LocationComponent : ConfigViewComponent
{
    public override void Draw()
    {
        var categoryNames = ServiceContext.CategoryService.GetCategoryNames();
        if (ImGui.BeginTabBar("Tracking_TabBar", ImGuiTabBarFlags.None))
        {
            this.DrawLocationTab("Overworld", this.config.Overworld, categoryNames);
            this.DrawLocationTab("Content", this.config.Content, categoryNames);
            this.DrawLocationTab("HighEndContent", this.config.HighEndContent, categoryNames);
        }
    }

    private void DrawLocationTab(string header, TrackingLocationConfig trackingLocationConfig, IReadOnlyCollection<string> categoryNames)
    {
        if (LocGui.BeginTabItem(header))
        {
            ImGuiHelpers.ScaledDummy(1f);
            var addPlayers = trackingLocationConfig.AddPlayers;
            if (ToadGui.Checkbox("AddPlayers", ref addPlayers))
            {
                trackingLocationConfig.AddPlayers = addPlayers;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            var addEncounters = trackingLocationConfig.AddEncounters;
            if (ToadGui.Checkbox("AddEncounters", ref addEncounters))
            {
                trackingLocationConfig.AddEncounters = addEncounters;
                ServiceContext.ConfigService.SaveConfig(this.config);
            }

            var disableCategoryBox = categoryNames.Count == 1;
            if (disableCategoryBox)
            {
                ImGui.BeginDisabled();
            }

            var selectedCategoryIndex = 0;
            var categoryName = ServiceContext.CategoryService.GetCategory(trackingLocationConfig.DefaultCategoryId)?.Name;
            if (!string.IsNullOrEmpty(categoryName))
            {
                selectedCategoryIndex = categoryNames.ToList().IndexOf(categoryName);
            }

            if (ToadGui.Combo("DefaultCategory", ref selectedCategoryIndex, categoryNames))
            {
                var category = ServiceContext.CategoryService.GetCategoryByName(categoryNames.ElementAt(selectedCategoryIndex));
                if (category?.Id != null)
                {
                    trackingLocationConfig.DefaultCategoryId = category.Id;
                    ServiceContext.ConfigService.SaveConfig(this.config);
                }
                else if (selectedCategoryIndex == 0)
                {
                    trackingLocationConfig.DefaultCategoryId = 0;
                    ServiceContext.ConfigService.SaveConfig(this.config);
                }
            }

            if (disableCategoryBox)
            {
                ImGui.EndDisabled();
            }

            ImGui.EndTabItem();
        }
    }
}
