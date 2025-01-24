using System.Collections.Generic;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Common;

public static class PlayerFCHelper
{
    public static KeyValuePair<FreeCompanyState, string> CheckFreeCompany(string tag, bool inContent) =>
        DetermineFCState(tag, inContent);

    public static KeyValuePair<FreeCompanyState, string> CheckFreeCompany(string tag, KeyValuePair<FreeCompanyState, string> previousState, bool inContent) =>
        inContent ? previousState : DetermineFCState(tag, inContent);

    private static KeyValuePair<FreeCompanyState, string> DetermineFCState(string tag, bool inContent)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerFCHelper.DetermineFCState(): {tag}, {inContent}");
        if (inContent)
            return new KeyValuePair<FreeCompanyState, string>(FreeCompanyState.Unknown, tag);

        var state = string.IsNullOrEmpty(tag) ? FreeCompanyState.NotInFC : FreeCompanyState.InFC;
        return new KeyValuePair<FreeCompanyState, string>(state, tag);
    }
}
