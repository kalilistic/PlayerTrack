namespace PlayerTrack.UserInterface.Config.Components;

using System.Diagnostics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Loc.ImGui;
using ImGuiNET;

public class ContributeComponent : ConfigViewComponent
{
    public override void Draw() => DrawContribute();

    private static void DrawContribute()
    {
        ImGuiHelpers.ScaledDummy(1f);
        LocGui.Text("PitchInIntro");
        ImGuiHelpers.ScaledDummy(1f);

        LocGui.TextColored("ForEveryoneTitle", ImGuiColors.DalamudViolet);
        LocGui.BulletText("SignUpTest");
        LocGui.BulletText("ReportFindings");
        LocGui.BulletText("StableExperience");
        ImGuiHelpers.ScaledDummy(1f);

        LocGui.TextColored("ForTranslatorsTitle", ImGuiColors.DalamudViolet);
        LocGui.BulletText("HelpWithTranslation");
        LocGui.BulletText("CrowdinProject");
        LocGui.BulletText("DiscordRoleInfo");
        ImGuiHelpers.ScaledDummy(1f);

        LocGui.TextColored("ForDevelopersTitle", ImGuiColors.DalamudViolet);
        LocGui.BulletText("OpenForDevHelp");
        LocGui.BulletText("ReviewGitHub");
        LocGui.BulletText("LargeFeatureDiscussion");
        ImGuiHelpers.ScaledDummy(1f);

        LocGui.TextColored("SupportFurtherTitle", ImGuiColors.DalamudViolet);
        LocGui.BulletText("DonationsAppreciated");
        LocGui.BulletText("NoSpecialPerks");
        LocGui.BulletText("ContributionUsage");
        ImGuiHelpers.ScaledDummy(1f);

        if (ImGui.Button("Patreon"))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.patreon.com/kalilistic",
                UseShellExecute = true,
            });
        }

        ImGui.SameLine();
        if (ImGui.Button("Ko-fi"))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://ko-fi.com/kalilistic",
                UseShellExecute = true,
            });
        }
    }
}
