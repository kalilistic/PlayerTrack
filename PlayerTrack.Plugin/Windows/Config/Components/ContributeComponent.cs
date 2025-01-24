using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using ImGuiNET;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class ContributeComponent : ConfigViewComponent
{
    public override void Draw() => DrawContribute();

    private static void DrawContribute()
    {
        ImGuiHelpers.ScaledDummy(1f);
        ImGui.TextUnformatted(Language.PitchInIntro);
        ImGuiHelpers.ScaledDummy(1f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ForEveryoneTitle);
        Helper.BulletText(Language.SignUpTest);
        Helper.BulletText(Language.ReportFindings);
        Helper.BulletText(Language.StableExperience);
        ImGuiHelpers.ScaledDummy(1f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ForTranslatorsTitle);
        Helper.BulletText(Language.HelpWithTranslation);
        Helper.BulletText(Language.CrowdinProject);
        Helper.BulletText(Language.DiscordRoleInfo);
        ImGuiHelpers.ScaledDummy(1f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.ForDevelopersTitle);
        Helper.BulletText(Language.OpenForDevHelp);
        Helper.BulletText(Language.ReviewGitHub);
        Helper.BulletText(Language.LargeFeatureDiscussion);
        ImGuiHelpers.ScaledDummy(1f);

        Helper.TextColored(ImGuiColors.DalamudViolet, Language.SupportFurtherTitle);
        Helper.BulletText(Language.DonationsAppreciated);
        Helper.BulletText(Language.NoSpecialPerks);
        Helper.BulletText(Language.ContributionUsage);
        ImGuiHelpers.ScaledDummy(1f);

        ImGui.SameLine();
        if (ImGui.Button("Ko-fi"))
            Dalamud.Utility.Util.OpenLink("https://ko-fi.com/kalilistic");
    }
}
