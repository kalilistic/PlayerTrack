using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable InvertIf

namespace PlayerTrack
{
	public partial class PlayerService
	{
		public delegate void ProcessPlayersEventHandler();

		public event ProcessPlayersEventHandler PlayersProcessed;

		public void ProcessPlayers(List<TrackPlayer> incomingPlayers)
		{
			ProcessLodestoneRequests();
			MergeDuplicates();
			CurrentPlayers.Clear();
			var currentTime = DateUtil.CurrentTime();
			foreach (var player in incomingPlayers)
				try
				{
					var playerCheck = player;
					var encounter = player.Encounters.Last();
					if (!AllPlayers.ContainsKey(player.Key))
					{
						EnrichPlayerData(player);
						var addedPlayerToAllPlayers = AllPlayers.TryAdd(player.Key, player);
						if (!addedPlayerToAllPlayers) throw new ArgumentException();
						SubmitLodestoneRequest(player);
					}
					else
					{
						var retrievedPlayer = AllPlayers.TryGetValue(player.Key, out var existingPlayer);
						if (!retrievedPlayer) throw new KeyNotFoundException();
						var isNewEncounter = existingPlayer.Encounters.Last().Location.TerritoryType !=
						                     encounter.Location.TerritoryType ||
						                     DateUtil.CurrentTime() - existingPlayer.Encounters.Last().Updated >=
						                     _plugin.Configuration.NewEncounterThreshold;
						if (isNewEncounter)
						{
							existingPlayer.Encounters.Add(encounter);
						}
						else
						{
							encounter.Created = existingPlayer.Encounters[existingPlayer.Encounters.Count - 1].Created;
							existingPlayer.PreviouslyLastSeen = existingPlayer.LastSeen;
							existingPlayer.Encounters[existingPlayer.Encounters.Count - 1] = encounter;
						}

						if (player.Lodestone.Id != 0 && existingPlayer.Lodestone.Id == 0)
							existingPlayer.Lodestone.Id = player.Lodestone.Id;
						if (!string.IsNullOrEmpty(player.FreeCompany)) existingPlayer.FreeCompany = player.FreeCompany;
						existingPlayer.ClearBackingFields();
						playerCheck = existingPlayer;
					}

					var addedPlayerToCurrentPlayers = CurrentPlayers.TryAdd(player.Key, playerCheck);
					if (!addedPlayerToCurrentPlayers) throw new ArgumentException();
				}
				catch (Exception ex)
				{
					_plugin.LogError(ex, "Skipping player " + player.Key);
				}

			if (_plugin.TrackViewMode == TrackViewMode.RecentPlayers)
			{
				RecentPlayers.Clear();
				foreach (var playerEntry in AllPlayers)
					try
					{
						var retrievedPlayer = AllPlayers.TryGetValue(playerEntry.Key, out var player);
						if (!retrievedPlayer) throw new KeyNotFoundException();
						if (currentTime - player.Encounters[player.Encounters.Count - 1].Updated <
						    _plugin.Configuration.RecentPlayerThreshold)
						{
							var addedPlayerToRecentPlayers = RecentPlayers.TryAdd(player.Key, player);
							if (!addedPlayerToRecentPlayers) throw new ArgumentException();
						}
					}
					catch (Exception ex)
					{
						_plugin.LogError(ex, "Skipping player " + playerEntry.Key);
					}
			}

			PlayersProcessed?.Invoke();
			SendAlerts();
		}
	}
}