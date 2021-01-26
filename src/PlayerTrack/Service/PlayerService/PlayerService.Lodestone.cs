namespace PlayerTrack
{
	public partial class PlayerService : IPlayerService
	{
		public void ProcessLodestoneRequests()
		{
			var responses = _plugin.LodestoneService.GetResponses();
			if (responses == null) return;
			foreach (var response in responses)
			{
				var result = AllPlayers.TryGetValue(response.PlayerKey, out var player);
				if (result == false) continue;
				player.Lodestone.Id = response.LodestoneId;
				player.Lodestone.Status = response.Status;
				player.Lodestone.LastUpdated = DateUtil.CurrentTime();
				HandleLodestoneFailure(player);
			}
		}

		private static void HandleLodestoneFailure(TrackPlayer player)
		{
			if (player.Lodestone.Status == TrackLodestoneStatus.Failed)
			{
				player.Lodestone.LastFailed = player.Lodestone.LastUpdated;
				player.Lodestone.FailureCount++;
			}
			else
			{
				player.Lodestone.LastFailed = 0;
				player.Lodestone.FailureCount = 0;
				player.Lodestone.LastFailed = 0;
			}
		}

		private void UpdateLodestoneStatus(TrackPlayer player)
		{
			if (player.Lodestone?.Status != null)
			{
				if (player.Lodestone.Status == TrackLodestoneStatus.Verifying)
					player.Lodestone.Status = TrackLodestoneStatus.Unverified;
				else if (player.Lodestone.Status == TrackLodestoneStatus.Updating ||
				         player.Lodestone.Status == TrackLodestoneStatus.Updated)
					player.Lodestone.Status = TrackLodestoneStatus.Verified;
			}
		}

		private void SubmitLodestoneRequest(TrackPlayer player, long currentTime)
		{
			var lode = player.Lodestone;
			if (lode.Status == TrackLodestoneStatus.Unverified)
			{
				SubmitLodestoneRequest(player);
			}
			else if (lode.Status == TrackLodestoneStatus.Failed &&
			         lode.FailureCount < _plugin.Configuration.LodestoneMaxFailure &&
			         currentTime < lode.LastFailed + _plugin.Configuration.LodestoneFailureDelay)
			{
				player.Lodestone.Status = TrackLodestoneStatus.Unverified;
				SubmitLodestoneRequest(player);
			}
		}

		private void SubmitLodestoneRequest(TrackPlayer player)
		{
			if (!_plugin.Configuration.SyncToLodestone) return;
			if (player.Lodestone.Status == TrackLodestoneStatus.Unverified)
			{
				_plugin.LodestoneService.AddRequest(new TrackLodestoneRequest
				{
					PlayerKey = player.Key,
					PlayerName = player.Name,
					WorldName = player.HomeWorld
				});
				player.Lodestone.Status = TrackLodestoneStatus.Verifying;
			}
		}
	}
}