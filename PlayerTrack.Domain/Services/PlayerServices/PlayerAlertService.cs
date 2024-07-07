using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Helpers;
using Newtonsoft.Json;

public class PlayerAlertService
{
    private const long AlertFrequency = 14400000; // 4 hours
    private static readonly Regex ProximityRegex = new(@"^(?<playerName>[A-Z][a-zA-Z'-]*\s[A-Z][a-zA-Z'-]*)(?<worldName>[A-Z][a-zA-Z]*)\s.*", RegexOptions.Compiled);
    private static readonly Regex NameWorldChangeRegex = new(@".*》\s*(?<playerName>[A-Z][a-zA-Z'-]*\s[A-Z][a-zA-Z'-]*)(?<worldName>[A-Z][a-zA-Z]*)$", RegexOptions.Compiled);

    private DalamudLinkPayload OpenPlayerTrackChatLinkHandler { get; set; } = 
        DalamudContext.PluginInterface.AddChatLinkHandler(
            (uint)ChatLinkHandler.OpenPlayerTrack, OnChatLinkClick);

    public void SendPlayerNameWorldChangeAlert(
        Player player, 
        string previousPlayerName, uint previousWorldId,
        string newPlayerName, uint newWorldId) => 
        Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose(
            $"Entering PlayerAlertService.SendPlayerNameWorldChangeAlert(): {previousPlayerName}, {previousWorldId}");
        var shouldSendNameAlert = previousPlayerName != newPlayerName && IsNameChangeAlertEnabled(player);
        var shouldSendWorldAlert = previousWorldId != newWorldId && IsWorldTransferAlertEnabled(player);
        if (!shouldSendNameAlert && !shouldSendWorldAlert) return;
        
        var payloads = new List<Payload>
        {
            this.OpenPlayerTrackChatLinkHandler,
            new TextPayload(previousPlayerName),
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload(DalamudContext.DataManager.GetWorldNameById(previousWorldId)),
            new TextPayload(" 》 "),
            new TextPayload(newPlayerName),
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload(DalamudContext.DataManager.GetWorldNameById(newWorldId)),
            RawPayload.LinkTerminator
        };
        if (payloads.Count == 0)
        {
            DalamudContext.PluginLog.Warning("Skipping empty alert for name/world change.");
            try
            {
                DalamudContext.PluginLog.Warning($"Player: {JsonConvert.SerializeObject(player)}");
            }
            catch (Exception ex)
            {
                DalamudContext.PluginLog.Error(ex, "Failed to serialize player.");
            }

            return;
        }
        
        DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);
    });
    
    public void SendProximityAlert(Player player) => Task.Run(() =>
    {
        var payloads = new List<Payload>();
        if (!IsProximityAlertEnabled(player) ||
            UnixTimestampHelper.CurrentTime() - player.LastAlertSent <= AlertFrequency ||
            UnixTimestampHelper.CurrentTime() - player.Created <= AlertFrequency) return;
        player.LastAlertSent = UnixTimestampHelper.CurrentTime();
        UpdatePlayerAlert(player.Id, player.LastAlertSent);
        payloads.Add(this.OpenPlayerTrackChatLinkHandler);
        payloads.Add(new TextPayload(player.Name));
        payloads.Add(new IconPayload(BitmapFontIcon.CrossWorld));
        payloads.Add(new TextPayload(DalamudContext.DataManager.GetWorldNameById(player.WorldId)));
        payloads.Add(new TextPayload($" {ServiceContext.Localization.GetString("ProximityAlertMessage")}"));
        payloads.Add(RawPayload.LinkTerminator);
        if (payloads.Count == 0)
        {
            DalamudContext.PluginLog.Warning("Skipping empty alert for proximity.");
            try
            {
                DalamudContext.PluginLog.Warning($"Player: {JsonConvert.SerializeObject(player)}");
            }
            catch (Exception ex)
            {
                DalamudContext.PluginLog.Error(ex, "Failed to serialize player.");
            }

            return;
        }

        DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);
    });

    public void Dispose() => DalamudContext.PluginInterface.RemoveChatLinkHandler((uint)ChatLinkHandler.OpenPlayerTrack);

    private static void OnChatLinkClick(uint id, SeString message)
    {
        DalamudContext.PluginLog.Verbose($"Entering ChatHandler.OnChatLinkClick(): {id}, {message}");

        var match = NameWorldChangeRegex.Match(message.TextValue);
        if (!match.Success)
        {
            match = ProximityRegex.Match(message.TextValue);
        }

        if (match.Success)
        {
            var name = match.Groups["playerName"].Value;
            var worldName = match.Groups["worldName"].Value;
            ServiceContext.PlayerProcessService.SelectPlayer(name, worldName);
        }
        else
        {
            DalamudContext.PluginLog.Verbose("Failed to parse chat link.");
        }
    }

    private static bool IsProximityAlertEnabled(Player player) => PlayerConfigService.GetIsProximityAlertEnabled(player);

    private static bool IsWorldTransferAlertEnabled(Player player) => PlayerConfigService.GetIsWorldTransferAlertEnabled(player);

    private static bool IsNameChangeAlertEnabled(Player player) => PlayerConfigService.GetIsNameChangeAlertEnabled(player);

    private static void UpdatePlayerAlert(int playerId, long playerLastAlertSent)
    {
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            return;
        }

        player.LastAlertSent = playerLastAlertSent;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
    }
}
