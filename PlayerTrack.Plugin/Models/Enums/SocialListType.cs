using System;
using PlayerTrack.Resource;

namespace PlayerTrack.Models;

public enum SocialListType
{
    None,
    FriendList,
    BlackList,
    FreeCompany,
    LinkShell,
    CrossWorldLinkShell,
}

public static class SocialListTypeExtension
{
    public static string ToLocalizedString(this SocialListType socialListType)
    {
        return socialListType switch
        {
            SocialListType.None => Language.None,
            SocialListType.FriendList => Language.FriendList,
            SocialListType.BlackList => Language.BlackList,
            SocialListType.FreeCompany => Language.FreeCompany,
            SocialListType.LinkShell => Language.LinkShell,
            SocialListType.CrossWorldLinkShell => Language.CrossWorldLinkShell,
            _ => throw new ArgumentOutOfRangeException(nameof(socialListType), socialListType, null)
        };
    }

    public static string ToAbrLocalizedString(this SocialListType socialListType)
    {
        return socialListType switch
        {
            SocialListType.None => Language.None,
            SocialListType.FriendList => Language.FriendList_Abbreviation,
            SocialListType.BlackList => Language.BlackList_Abbreviation,
            SocialListType.FreeCompany => Language.FreeCompany_Abbreviation,
            SocialListType.LinkShell => Language.LinkShell_Abbreviation,
            SocialListType.CrossWorldLinkShell => Language.CrossWorldLinkShell_Abbreviation,
            _ => throw new ArgumentOutOfRangeException(nameof(socialListType), socialListType, null)
        };
    }
}
