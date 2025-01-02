using Dalamud.DrunkenToad.Core;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using ImGuiNET;

public class HelpComponent : ConfigViewComponent
{
    public override void Draw()
    {
        if (ImGui.BeginTabBar("Help_TabBar", ImGuiTabBarFlags.None))
        {
            if (LocGui.BeginTabItem("Search"))
            {
                ImGuiHelpers.ScaledDummy(1f);

                // General Syntax Section
                DrawSection("HowToSearchTitle", "HowToSearchExplanation");

                // Search Keys Section
                DrawSection("SearchOptionsTitle", "SearchOptionsExplanation",
                    [
                        "SearchOptionsExplanationNotes",
                        "SearchOptionsExplanationFC",
                        "SearchOptionsExplanationTags",
                        "SearchOptionsExplanationRace",
                        "SearchOptionsExplanationGender",
                        "SearchOptionsExplanationWorld",
                        "SearchOptionsExplanationDC",
                        "SearchOptionsExplanationDefault"
                    ]);

                // Wildcard Patterns Section
                DrawSection("AdvancedMatchingTitle",
                    "AdvancedMatchingExplanation",
                    [
                        "AdvancedMatchingExplanationExact",
                        "AdvancedMatchingExplanationStart",
                        "AdvancedMatchingExplanationEnd",
                        "AdvancedMatchingExplanationContains",
                        "AdvancedMatchingExplanationExclude"
                    ]);

                // Examples Section
                DrawSection("ExamplesTitle", 
                    "ExamplesExplanation",
                    [
                        "ExamplesExplanation1",
                        "ExamplesExplanation2",
                        "ExamplesExplanation3"
                    ]);

                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private static void DrawSection(string title, string description, string[]? bullets = null)
    {
        LocGui.TextColored(title, ImGuiColors.DalamudViolet);
        ImGui.TextWrapped( DalamudContext.LocManager.GetString(description));
        ImGuiHelpers.ScaledDummy(1f);

        if (bullets != null)
        {
            ImGui.Indent();
            foreach (var bullet in bullets)
            {
                LocGui.BulletText(bullet);
            }
            ImGui.Unindent();
        }

        ImGuiHelpers.ScaledDummy(2f);
    }
}
