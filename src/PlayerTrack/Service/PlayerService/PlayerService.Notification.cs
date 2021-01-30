using System;
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
			foreach (var player in CurrentPlayers)
			{
				var category = _plugin.CategoryService.GetCategory(player.Value.CategoryId);
				if (player.Value.Alert.State == TrackAlertState.Enabled || category.EnableAlerts)
				{
					if (IsTimeForAlert(player.Value))
					{
						if (_plugin.Configuration.IncludeNotesInAlert &&
						    !string.IsNullOrEmpty(player.Value.Notes))
							_plugin.PrintMessage(string.Format(
								Loc.Localize("PlayerAlertWithNotes", "{0} last seen {1}: {2}"), player.Value.Name,
								player.Value.PreviouslyLastSeen, player.Value.AbbreviatedNotes));
						else
							_plugin.PrintMessage(string.Format(
								Loc.Localize("PlayerAlert", "{0} last seen {1}."), player.Value.Name,
								player.Value.PreviouslyLastSeen));
						player.Value.Alert.LastSent = DateUtil.CurrentTime();
						Thread.Sleep(_plugin.Configuration.AlertDelay);
					}
					else if (player.Value.Alert.LastSent == 0)
					{
						player.Value.Alert.LastSent = DateUtil.CurrentTime();
					}
				}
			}
		}
	}
}