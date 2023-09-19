using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Threading.Tasks;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Logging;

public class PlayerAlertService
{
    private const long AlertFrequency = 14400000; // 4 hours

    public static void SendPlayerNameWorldChangeAlert(bool nameChanged, bool worldChanged, Player oldestPlayer, Player newPlayer) => Task.Run(() =>
    {
        PluginLog.LogVerbose(
            $"Entering PlayerAlertService.SendPlayerNameWorldChangeAlert(): {nameChanged}, {worldChanged}, {oldestPlayer.Name}, {newPlayer.Name}");
        var oldestPlayerWorldName = DalamudContext.DataManager.GetWorldNameById(newPlayer.WorldId);
        var newPlayerWorldName = DalamudContext.DataManager.GetWorldNameById(oldestPlayer.WorldId);

        var shouldSendNameAlert = nameChanged && IsNameChangeAlertEnabled(oldestPlayer);
        var shouldSendWorldAlert = worldChanged && IsWorldTransferAlertEnabled(oldestPlayer);

        if (shouldSendNameAlert || shouldSendWorldAlert)
        {
            SendNameWorldChangeNotification(oldestPlayer.Name, oldestPlayerWorldName, newPlayer.Name, newPlayerWorldName);
        }
    });

    public static void SendProximityAlert(Player player) => Task.Run(() =>
    {
        if (IsProximityAlertEnabled(player) &&
            UnixTimestampHelper.CurrentTime() - player.LastAlertSent > AlertFrequency &&
            UnixTimestampHelper.CurrentTime() - player.Created > AlertFrequency)
        {
            player.LastAlertSent = UnixTimestampHelper.CurrentTime();
            UpdatePlayerAlert(player.Id, player.LastAlertSent);
            List<Payload> payloads = new()
            {
                new TextPayload(player.Name),
                new IconPayload(BitmapFontIcon.CrossWorld),
                new TextPayload(DalamudContext.DataManager.GetWorldNameById(player.WorldId)),
                new TextPayload($" {ServiceContext.Localization.GetString("ProximityAlertMessage")}"),
            };

            DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);
        }
    });

    private static bool IsProximityAlertEnabled(Player player) => PlayerConfigService.GetIsProximityAlertEnabled(player);

    private static bool IsWorldTransferAlertEnabled(Player player) => PlayerConfigService.GetIsWorldTransferAlertEnabled(player);

    private static bool IsNameChangeAlertEnabled(Player player) => PlayerConfigService.GetIsNameChangeAlertEnabled(player);

    private static void SendNameWorldChangeNotification(string oldPlayerName, string oldWorldName, string newPlayerName, string newWorldName)
    {
        PluginLog.LogVerbose($"Entering PlayerAlertService.SendNameWorldChangeNotification(): {oldPlayerName}, {oldWorldName}, {newPlayerName}, {newWorldName}");
        List<Payload> payloads = new()
        {
            new TextPayload(oldPlayerName),
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload(oldWorldName),
            new TextPayload(" 》 "),
            new TextPayload(newPlayerName),
            new IconPayload(BitmapFontIcon.CrossWorld),
            new TextPayload(newWorldName),
        };

        DalamudContext.ChatGuiHandler.PluginPrintNotice(payloads);
    }

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
