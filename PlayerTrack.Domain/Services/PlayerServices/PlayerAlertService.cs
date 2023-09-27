using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.DrunkenToad.Helpers;

public class PlayerAlertService
{
    private const long AlertFrequency = 14400000; // 4 hours
    private static readonly Regex ProximityRegex = new(@"^(?<playerName>[A-Z][a-zA-Z'-]*\s[A-Z][a-zA-Z'-]*)(?<worldName>[A-Z][a-zA-Z]*)\s.*", RegexOptions.Compiled);
    private static readonly Regex NameWorldChangeRegex = new(@".*》\s*(?<playerName>[A-Z][a-zA-Z'-]*\s[A-Z][a-zA-Z'-]*)(?<worldName>[A-Z][a-zA-Z]*)$", RegexOptions.Compiled);

    public PlayerAlertService() => this.OpenPlayerTrackChatLinkHandler = DalamudContext.PluginInterface.AddChatLinkHandler((uint)ChatLinkHandler.OpenPlayerTrack, OnChatLinkClick);

    private DalamudLinkPayload OpenPlayerTrackChatLinkHandler { get; set; }

    public static void SendNameWorldChangeAlert(IEnumerable<Payload> payloads) => DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);

    public List<Payload> CreatePlayerNameWorldChangeAlert(Player oldestPlayer, Player newPlayer)
    {
        DalamudContext.PluginLog.Verbose(
            $"Entering PlayerAlertService.SendPlayerNameWorldChangeAlert(): {oldestPlayer.Name}, {newPlayer.Name}");
        var shouldSendNameAlert = oldestPlayer.Name != newPlayer.Name && IsNameChangeAlertEnabled(oldestPlayer);
        var shouldSendWorldAlert = oldestPlayer.WorldId != newPlayer.WorldId && IsWorldTransferAlertEnabled(oldestPlayer);

        var payloads = new List<Payload>();
        if (shouldSendNameAlert || shouldSendWorldAlert)
        {
            payloads.Add(this.OpenPlayerTrackChatLinkHandler);
            payloads.Add(new TextPayload(oldestPlayer.Name));
            payloads.Add(new IconPayload(BitmapFontIcon.CrossWorld));
            payloads.Add(new TextPayload(DalamudContext.DataManager.GetWorldNameById(oldestPlayer.WorldId)));
            payloads.Add(new TextPayload(" 》 "));
            payloads.Add(new TextPayload(newPlayer.Name));
            payloads.Add(new IconPayload(BitmapFontIcon.CrossWorld));
            payloads.Add(new TextPayload(DalamudContext.DataManager.GetWorldNameById(newPlayer.WorldId)));
            payloads.Add(RawPayload.LinkTerminator);
        }

        return payloads;
    }

    public void SendProximityAlert(Player player) => Task.Run(() =>
    {
        var payloads = new List<Payload>();
        if (IsProximityAlertEnabled(player) &&
            UnixTimestampHelper.CurrentTime() - player.LastAlertSent > AlertFrequency &&
            UnixTimestampHelper.CurrentTime() - player.Created > AlertFrequency)
        {
            player.LastAlertSent = UnixTimestampHelper.CurrentTime();
            UpdatePlayerAlert(player.Id, player.LastAlertSent);
            payloads.Add(this.OpenPlayerTrackChatLinkHandler);
            payloads.Add(new TextPayload(player.Name));
            payloads.Add(new IconPayload(BitmapFontIcon.CrossWorld));
            payloads.Add(new TextPayload(DalamudContext.DataManager.GetWorldNameById(player.WorldId)));
            payloads.Add(new TextPayload($" {ServiceContext.Localization.GetString("ProximityAlertMessage")}"));
            payloads.Add(RawPayload.LinkTerminator);
            DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);
        }
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
