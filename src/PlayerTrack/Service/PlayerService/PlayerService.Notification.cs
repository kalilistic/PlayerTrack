using System;
using System.Linq;
using System.Threading;
using CheapLoc;

// ReSharper disable InvertIf

namespace PlayerTrack
{
    public partial class PlayerService
    {
        private bool IsTimeForAlert(TrackPlayer player)
        {
            return player.Alert.LastSent != 0 &&
                   (DateTime.UtcNow - player.Alert.LastSent.ToDateTime()).TotalMilliseconds >
                   _plugin.Configuration.AlertFrequency;
        }

        private void SendAlerts()
        {
            if (!_plugin.Configuration.EnableAlerts) return;
            try
            {
                foreach (var player in CurrentPlayers)
                    try
                    {
                        var category = _plugin.CategoryService.GetCategory(player.Value.CategoryId);
                        if (player.Value.Alert.State == TrackAlertState.Enabled || category.EnableAlerts)
                        {
                            if (IsTimeForAlert(player.Value))
                            {
                                var location = player.Value.Encounters.Last().Location?.ToString();
                                if (string.IsNullOrEmpty(location)) location = "Eorzea";
                                if (_plugin.Configuration.IncludeNotesInAlert &&
                                    !string.IsNullOrEmpty(player.Value.Notes))
                                    _plugin.PrintMessage(string.Format(
                                        Loc.Localize("PlayerAlertWithNotes", "{0} last seen {1} in {2}: {3}"),
                                        player.Value.Name,
                                        player.Value.PreviouslyLastSeen,
                                        location,
                                        player.Value.AbbreviatedNotes));
                                else
                                    _plugin.PrintMessage(string.Format(
                                        Loc.Localize("PlayerAlert", "{0} last seen {1} in {2}."), player.Value.Name,
                                        player.Value.PreviouslyLastSeen,
                                        location));
                                player.Value.Alert.LastSent = DateUtil.CurrentTime();
                                Thread.Sleep(_plugin.Configuration.AlertDelay);
                            }
                            else if (player.Value.Alert.LastSent == 0)
                            {
                                player.Value.Alert.LastSent = DateUtil.CurrentTime();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _plugin.LogError(ex, "Failed to send alert for specific player.");
                    }
            }
            catch (Exception ex)
            {
                _plugin.LogError(ex, "Failed alert processing.");
            }
        }
    }
}