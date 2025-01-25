using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
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
        using (ImRaii.PushColor(ImGuiCol.Button, new Vector4(0.12549f, 0.74902f, 0.33333f, 0.6f)))
        {
            if (ImGui.Button(Language.AboutKoFi))
                Dalamud.Utility.Util.OpenLink("https://ko-fi.com/infiii");
        }
    }
}
