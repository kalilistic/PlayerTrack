using System;
using System.Collections.Generic;
using System.Data;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PlayerTrack.Data;

namespace PlayerTrack.Handler;

/// <summary>
/// Provides events when data is received for social lists (e.g. Friends List).
/// </summary>
public unsafe class SocialListHandler
{
    private const int BlackListStringArray = 14;
    private const int BlackListWorldStartIndex = 200;
    private static string UnableToRetrieveMessage = null!;

    private Hook<InfoProxyFriendList.Delegates.EndRequest>? InfoProxyFriendListEndRequestHook;
    private Hook<InfoProxyFreeCompanyMember.Delegates.EndRequest>? InfoProxyFreeCompanyEndRequestHook;
    private Hook<InfoProxyLinkshellMember.Delegates.EndRequest>? InfoProxyLinkShellEndRequestHook;
    private Hook<InfoProxyCrossWorldLinkshellMember.Delegates.EndRequest>? InfoProxyCrossWorldLinkShellEndRequestHook;
    private Hook<InfoProxyBlacklist.Delegates.EndRequest>? InfoProxyBlackListEndRequestHook;

    private bool IsEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocialListHandler" /> class.
    /// </summary>
    public SocialListHandler()
    {
        Plugin.PluginLog.Verbose("Entering SocialListHandler.Start()");
        SetupUnavailableMessage();
        SetupFriendList();
        SetupFreeCompany();
        SetupLinkShell();
        SetupCrossWorldLinkShell();
        SetupBlackList();
    }

    public delegate void FriendListReceivedDelegate(List<SocialListMemberData> members);
    public delegate void FreeCompanyReceivedDelegate(byte pageCount, byte currentPage, List<SocialListMemberData> members);
    public delegate void LinkShellReceivedDelegate(byte index, List<SocialListMemberData> members);
    public delegate void CrossWorldLinkShellReceivedDelegate(byte index, List<SocialListMemberData> members);
    public delegate void BlackListReceivedDelegate(List<SocialListMemberData> members);

    public event FriendListReceivedDelegate? OnFriendListReceived;
    public event FreeCompanyReceivedDelegate? OnFreeCompanyReceived;
    public event LinkShellReceivedDelegate? OnLinkShellReceived;
    public event CrossWorldLinkShellReceivedDelegate? OnCrossWorldLinkShellReceived;
    public event BlackListReceivedDelegate? OnBlackListReceived;

    public void Dispose()
    {
        Plugin.PluginLog.Verbose("Entering SocialListHandler.Dispose()");
        InfoProxyFriendListEndRequestHook?.Dispose();
        InfoProxyFreeCompanyEndRequestHook?.Dispose();
        InfoProxyLinkShellEndRequestHook?.Dispose();
        InfoProxyCrossWorldLinkShellEndRequestHook?.Dispose();
        InfoProxyBlackListEndRequestHook?.Dispose();
    }

    public void Start() => IsEnabled = true;

    private static List<SocialListMemberData> ExtractInfoProxyMembers(InfoProxyCommonList infoProxyCommonList)
    {
        Plugin.PluginLog.Verbose($"Entering ExtractInfoProxyMembers: EntryCount: {infoProxyCommonList.EntryCount}");

        var members = new List<SocialListMemberData>();
        foreach (var charaData in infoProxyCommonList.CharDataSpan)
        {
            var member = new SocialListMemberData
            {
                ContentId = charaData.ContentId,
                Name = charaData.NameString,
                HomeWorld = charaData.HomeWorld,
            };

            if (string.IsNullOrEmpty(member.Name))
                member.IsUnableToRetrieve = true;

            if (!member.IsValid())
                throw new DataException($"Invalid member: {member.Name} {member.ContentId} {member.HomeWorld}");

            members.Add(member);
        }

        return members;
    }

    private static List<SocialListMemberData> ExtractInfoProxyBlackListMembers(InfoProxyBlacklist* infoProxyBlacklist)
    {
        Plugin.PluginLog.Verbose($"Entering ExtractInfoProxyBlackListMembers: BlockedCharactersCount: {infoProxyBlacklist->BlockedCharactersCount}");

        var members = new List<SocialListMemberData>();
        for (var i = 0; i < infoProxyBlacklist->BlockedCharactersCount; i++)
        {
            var data = (nint*)AtkStage.Instance()->AtkArrayDataHolder->StringArrays[BlackListStringArray]->StringArray;
            var worldName = MemoryHelper.ReadStringNullTerminated(data[BlackListWorldStartIndex + i]);
            var member = new SocialListMemberData
            {
                Name = MemoryHelper.ReadStringNullTerminated(data[i]),
                HomeWorld = (ushort)Sheets.GetWorldIdByName(worldName),
                ShouldHaveContentId = false,
            };

            if (member.Name.Equals(UnableToRetrieveMessage, StringComparison.Ordinal))
            {
                Plugin.PluginLog.Verbose($"Skipping Blacklist Member: {i} {worldName}");
                continue;
            }

            if (!member.IsValid())
            {
                Plugin.PluginLog.Warning($"Invalid Blacklist member: {member.Name} {member.HomeWorld}");
                continue;
            }

            members.Add(member);
        }

        return members;
    }

    private static void SetupUnavailableMessage()
    {
        var isUnavailable = Sheets.AddonSheet.GetRowOrDefault(964)?.Text.ExtractText();
        if (string.IsNullOrEmpty(isUnavailable))
            throw new DataException("Unable to retrieve the message for unavailable players.");

        UnableToRetrieveMessage = isUnavailable;
    }

    private void SetupFriendList()
    {
        InfoProxyFriendListEndRequestHook = Plugin.HookManager.HookFromAddress<InfoProxyFriendList.Delegates.EndRequest>(InfoProxyFriendList.Instance()->VirtualTable->EndRequest, InfoProxyFriendListEndRequestDetour);
        InfoProxyFriendListEndRequestHook.Enable();
    }

    private void SetupFreeCompany()
    {
        InfoProxyFreeCompanyEndRequestHook = Plugin.HookManager.HookFromAddress<InfoProxyFreeCompanyMember.Delegates.EndRequest>(InfoProxyFreeCompanyMember.Instance()->VirtualTable->EndRequest, InfoProxyFreeCompanyEndRequestDetour);
        InfoProxyFreeCompanyEndRequestHook.Enable();
    }

    private void SetupLinkShell()
    {
        InfoProxyLinkShellEndRequestHook = Plugin.HookManager.HookFromAddress<InfoProxyLinkshellMember.Delegates.EndRequest>(InfoProxyLinkshellMember.Instance()->VirtualTable->EndRequest, InfoProxyLinkShellEndRequestDetour);
        InfoProxyLinkShellEndRequestHook.Enable();
    }

    private void SetupCrossWorldLinkShell()
    {
        InfoProxyCrossWorldLinkShellEndRequestHook = Plugin.HookManager.HookFromAddress<InfoProxyCrossWorldLinkshellMember.Delegates.EndRequest>(InfoProxyCrossWorldLinkshellMember.Instance()->VirtualTable->EndRequest, InfoProxyCrossWorldLinkShellEndRequestDetour);
        InfoProxyCrossWorldLinkShellEndRequestHook.Enable();
    }

    private void SetupBlackList()
    {
        InfoProxyBlackListEndRequestHook = Plugin.HookManager.HookFromAddress<InfoProxyBlacklist.Delegates.EndRequest>(InfoProxyBlacklist.Instance()->VirtualTable->EndRequest, InfoProxyBlackListEndRequestDetour);
        InfoProxyBlackListEndRequestHook.Enable();
    }

    private void InfoProxyFriendListEndRequestDetour(InfoProxyFriendList* infoProxyFriendList)
    {
        try
        {
            Plugin.PluginLog.Verbose("Entering InfoProxyFriendListEndRequestDetour");
            InfoProxyFriendListEndRequestHook?.Original(infoProxyFriendList);
            if (!IsEnabled)
                return;

            OnFriendListReceived?.Invoke(ExtractInfoProxyMembers(infoProxyFriendList->InfoProxyCommonList));
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Exception in InfoProxyFriendListEndRequestDetour");
        }
    }

    private void InfoProxyFreeCompanyEndRequestDetour(InfoProxyFreeCompanyMember* infoProxyFreeCompanyMember)
    {
        try
        {
            Plugin.PluginLog.Verbose("Entering InfoProxyFreeCompanyEndRequestDetour");
            InfoProxyFreeCompanyEndRequestHook?.Original(infoProxyFreeCompanyMember);
            if (!IsEnabled)
                return;

            var infoProxyFreeCompany = InfoProxyFreeCompany.Instance();
            if (infoProxyFreeCompany == null || infoProxyFreeCompany->TotalMembers == 0 || infoProxyFreeCompany->ActiveListItemNum != 1)
            {
                Plugin.PluginLog.Verbose("No FC members to process.");
                return;
            }

            var maxPage = (infoProxyFreeCompany->TotalMembers / 200) + 1;
            if (maxPage is < 1 or > 3)
            {
                Plugin.PluginLog.Warning($"Invalid FC page count: {maxPage}");
                return;
            }

            var agentFC = AgentFreeCompany.Instance();
            if (agentFC == null)
            {
                Plugin.PluginLog.Warning("Failed to get FC agent.");
                return;
            }

            var pageIndex = agentFC->CurrentMemberPageIndex;
            var currentPage = pageIndex + 1;
            if (currentPage > maxPage)
            {
                Plugin.PluginLog.Warning($"Invalid FC page: {currentPage}");
                return;
            }

            var members = ExtractInfoProxyMembers(infoProxyFreeCompanyMember->InfoProxyCommonList);
            OnFreeCompanyReceived?.Invoke((byte)maxPage, (byte)currentPage, members);
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Exception in InfoProxyFreeCompanyEndRequestDetour");
        }
    }

    private void InfoProxyCrossWorldLinkShellEndRequestDetour(InfoProxyCrossWorldLinkshellMember* infoProxyLinkshellMember)
    {
        try
        {
            Plugin.PluginLog.Verbose("Entering InfoProxyCrossWorldLinkShellEndRequestDetour");
            InfoProxyCrossWorldLinkShellEndRequestHook?.Original(infoProxyLinkshellMember);
            if (!IsEnabled)
                return;

            var agentCrossWorldLinkShell = AgentCrossWorldLinkshell.Instance();
            var index = agentCrossWorldLinkShell != null ? agentCrossWorldLinkShell->SelectedCWLSIndex : (byte)0;
            OnCrossWorldLinkShellReceived?.Invoke(index, ExtractInfoProxyMembers(infoProxyLinkshellMember->InfoProxyCommonList));
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Exception in InfoProxyCrossWorldLinkShellEndRequestDetour");
        }
    }

    private void InfoProxyLinkShellEndRequestDetour(InfoProxyLinkshellMember* infoProxyLinkshellMember)
    {
        try
        {
            Plugin.PluginLog.Verbose("Entering InfoProxyLinkShellEndRequestDetour");
            InfoProxyLinkShellEndRequestHook?.Original(infoProxyLinkshellMember);
            if (!IsEnabled)
                return;

            var agentLinkShell = AgentLinkshell.Instance();
            var index = agentLinkShell != null ? agentLinkShell->SelectedLSIndex : (byte)0;
            OnLinkShellReceived?.Invoke(index, ExtractInfoProxyMembers(infoProxyLinkshellMember->InfoProxyCommonList));
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Exception in InfoProxyLinkShellEndRequestDetour");
        }
    }

    private void InfoProxyBlackListEndRequestDetour(InfoProxyBlacklist* infoProxyBlacklist)
    {
        try
        {
            Plugin.PluginLog.Verbose("Entering InfoProxyBlackListEndRequestDetour");
            InfoProxyBlackListEndRequestHook?.Original(infoProxyBlacklist);
            if (!IsEnabled)
                return;

            OnBlackListReceived?.Invoke(ExtractInfoProxyBlackListMembers(infoProxyBlacklist));
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Exception in InfoProxyBlackListEndRequestDetour");
        }
    }
}
