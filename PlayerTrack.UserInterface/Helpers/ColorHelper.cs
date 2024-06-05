using System;
using System.Numerics;
using Dalamud.Interface.Colors;
using PlayerTrack.Models;

namespace PlayerTrack.UserInterface.Helpers;

public static class ColorHelper
{
    public static Vector4 GetColorByStatus(LodestoneStatus status)
    {
        switch (status)
        {
            case LodestoneStatus.Unverified:
                return ImGuiColors.DalamudGrey;
            case LodestoneStatus.Verified:
                return ImGuiColors.HealerGreen;
            case LodestoneStatus.Failed:
                return ImGuiColors.DalamudYellow;
            case LodestoneStatus.Banned:
                return ImGuiColors.DPSRed;
            case LodestoneStatus.Blocked:
                return ImGuiColors.DalamudOrange;
            case LodestoneStatus.NotApplicable:
            case LodestoneStatus.Unavailable:
            case LodestoneStatus.Cancelled:
            case LodestoneStatus.Invalid:
                return ImGuiColors.ParsedPink;
            default:
                return ImGuiColors.DalamudWhite;
        }
    }
}