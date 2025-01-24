using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class LocationComponent : ConfigViewComponent
{
    public override void Draw()
    {
        var categoryNames = ServiceContext.CategoryService.GetCategoryNames();

        using var tabBar = ImRaii.TabBar("Tracking_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        DrawLocationTab(Language.Overworld, Config.Overworld, categoryNames);
        DrawLocationTab(Language.Content, Config.Content, categoryNames);
        DrawLocationTab(Language.HighEndContent, Config.HighEndContent, categoryNames);
    }

    private void DrawLocationTab(string header, TrackingLocationConfig trackingLocationConfig, IReadOnlyCollection<string> categoryNames)
    {
        using var tabItem = ImRaii.TabItem(header);
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);
        var addPlayers = trackingLocationConfig.AddPlayers;
        if (Helper.Checkbox(Language.AddPlayers, ref addPlayers))
        {
            trackingLocationConfig.AddPlayers = addPlayers;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var addEncounters = trackingLocationConfig.AddEncounters;
        if (Helper.Checkbox(Language.AddEncounters, ref addEncounters))
        {
            trackingLocationConfig.AddEncounters = addEncounters;
            ServiceContext.ConfigService.SaveConfig(Config);
        }

        var disableCategoryBox = categoryNames.Count == 1;
        using (ImRaii.Disabled(disableCategoryBox))
        {
            var selectedCategoryIndex = 0;
            var categoryName = ServiceContext.CategoryService.GetCategory(trackingLocationConfig.DefaultCategoryId)?.Name;
            if (!string.IsNullOrEmpty(categoryName))
                selectedCategoryIndex = categoryNames.ToList().IndexOf(categoryName);

            if (Helper.Combo(Language.DefaultCategory, ref selectedCategoryIndex, categoryNames))
            {
                var category = ServiceContext.CategoryService.GetCategoryByName(categoryNames.ElementAt(selectedCategoryIndex));
                if (category?.Id != null)
                {
                    trackingLocationConfig.DefaultCategoryId = category.Id;
                    ServiceContext.ConfigService.SaveConfig(Config);
                }
                else if (selectedCategoryIndex == 0)
                {
                    trackingLocationConfig.DefaultCategoryId = 0;
                    ServiceContext.ConfigService.SaveConfig(Config);
                }
            }
        }
    }
}
