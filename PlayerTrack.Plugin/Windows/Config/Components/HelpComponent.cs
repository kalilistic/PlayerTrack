using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class HelpComponent : ConfigViewComponent
{
    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("Help_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        using var tabItem = ImRaii.TabItem(Language.Search);
        if (!tabItem.Success)
            return;

        ImGuiHelpers.ScaledDummy(1f);

        // General Syntax Section
        DrawSection(Language.HowToSearchTitle, Language.HowToSearchExplanation);

        // Search Keys Section
        DrawSection(Language.SearchOptionsTitle, Language.SearchOptionsExplanation,
                    [
                        Language.SearchOptionsExplanationNotes,
                        Language.SearchOptionsExplanationFC,
                        Language.SearchOptionsExplanationTags,
                        Language.SearchOptionsExplanationRace,
                        Language.SearchOptionsExplanationGender,
                        Language.SearchOptionsExplanationWorld,
                        Language.SearchOptionsExplanationDC,
                        Language.SearchOptionsExplanationDefault
                    ]);

        // Wildcard Patterns Section
        DrawSection(Language.AdvancedMatchingTitle, Language.AdvancedMatchingExplanation,
            [
                Language.AdvancedMatchingExplanationExact,
                Language.AdvancedMatchingExplanationStart,
                Language.AdvancedMatchingExplanationEnd,
                Language.AdvancedMatchingExplanationContains,
                Language.AdvancedMatchingExplanationExclude
            ]);

        // Examples Section
        DrawSection(Language.ExamplesTitle, Language.ExamplesExplanation,
            [
                Language.ExamplesExplanation1,
                Language.ExamplesExplanation2,
                Language.ExamplesExplanation3
            ]);
    }

    private static void DrawSection(string title, string description, string[]? bullets = null)
    {
        Helper.TextColored(ImGuiColors.DalamudViolet, title);
        Helper.TextWrapped(description);
        ImGuiHelpers.ScaledDummy(1f);

        if (bullets != null)
        {
            using (ImRaii.PushIndent())
            {
                foreach (var bullet in bullets)
                    Helper.BulletText(bullet);
            }
        }

        ImGuiHelpers.ScaledDummy(2f);
    }
}
