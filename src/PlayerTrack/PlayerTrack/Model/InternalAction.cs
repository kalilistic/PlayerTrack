#pragma warning disable 1591
namespace PlayerTrack
{
    /// <summary>
    /// Internal action code enum for use with context menus.
    /// </summary>
    public enum InternalAction : byte
    {
        SendTell = 1,
        ViewSearchInfo = 6,
        Trade = 8,
        InviteToParty = 9,
        Promote = 12,
        KickFromParty = 13,
        SendFriendRequest = 16,
        InviteToLinkshell = 17,
        AddToBlacklist = 28,
        Examine = 30,
        Follow = 31,
        RequestMeld = 32,
        InviteToCompany = 34,
        ViewCompanyProfile = 35,
        Emote = 36,
        Mark = 37,
        ViewPartyFinder = 46,
        FocusTarget = 48,
        ChallengeToNormalMatch = 64,
        Target = 66,
        Report = 69,
        SendFriendRequestChat = 73,
        InviteToCrossWorldLinkshell = 78,
        ReplyInSelectedChatMode = 100,
    }
}
