// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable MemberCanBeMadeStatic.Local

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
	public class OverlayWindow : WindowBase
	{
		private readonly List<Vector4> _colorPalette = ImGuiUtil.CreatePalette();
		private readonly IPlayerTrackPlugin _playerTrackPlugin;
		private string _activeSearch = string.Empty;
		private TrackPlayer _currentPlayer;
		private TrackPlayerMode _currentPlayerMode = TrackPlayerMode.CurrentPlayers;
		private View _currentView = View.Players;
		private string _searchInput = string.Empty;

		public OverlayWindow(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
		}

		public void DrawWindow()
		{
			if (!IsVisible) return;
			if (ImGui.Begin(Loc.Localize("OverlayWindow", "PlayerTrack") + "###PlayerTrack_Overlay_Window",
				ref IsVisible, ImGuiWindowFlags.NoResize))
				SelectCurrentView();
			ImGui.End();
		}

		private void SelectCurrentView()
		{
			if (_playerTrackPlugin.Configuration.Enabled)
				switch (_currentView)
				{
					case View.Players:
						DrawPlayersView();
						break;
					case View.PlayerDetail:
						DrawPlayerDetailView();
						break;
					default:
						DrawPlayersView();
						break;
				}
			else
				ImGui.Text(Loc.Localize("PluginDisabled", "Plugin is disabled."));
		}

		private void DrawPlayersView()
		{
			ImGui.SetWindowSize(new Vector2(200 * Scale, 250 * Scale), ImGuiCond.Always);
			_currentPlayer = null;
			PlayerMode(_currentPlayerMode.Index);
			PlayerSearchInput();
			PlayerList(GetPlayers(TrackPlayerMode.GetPlayerModeByIndex(_currentPlayerMode.Index)));
		}

		private void DrawPlayerDetailView()
		{
			ImGui.SetWindowSize(new Vector2(500 * Scale, 470 * Scale), ImGuiCond.Always);
			SetCurrentPlayer();
			Controls(_currentPlayer);
			PlayerInfo(_currentPlayer);
			PlayerLodestone(_currentPlayer);
			PlayerNotes(_currentPlayer);
			PlayerEncounters(_currentPlayer.Encounters);
		}

		private void PlayerMode(int viewIndex)
		{
			ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale);
			if (ImGui.Combo("###PlayerTrack_PlayerMode_Combo", ref viewIndex,
				TrackPlayerMode.ViewNames.ToArray(),
				TrackPlayerMode.ViewNames.Count))
				_currentPlayerMode = TrackPlayerMode.GetPlayerModeByIndex(viewIndex);
		}

		private void PlayerSearchInput()
		{
			if (_currentPlayerMode.Code == TrackPlayerMode.SearchForPlayers.Code)
			{
				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale / 1.5f);
				ImGui.InputTextWithHint("###PlayerTrack_PlayerNameSearch_Input",
					Loc.Localize("PlayerNameSearchHint", "player name"), ref _searchInput, 30);
				ImGui.SameLine();
				if (ImGui.Button(Loc.Localize("PlayerSearch", "Search") + "###PlayerTrack_PlayerSearch_Button"))
					_activeSearch = _searchInput;
			}
		}

		private Dictionary<string, TrackPlayer> GetPlayers(TrackPlayerMode currentPlayerMode)
		{
			if (currentPlayerMode.Code == TrackPlayerMode.CurrentPlayers.Code)
				return _playerTrackPlugin.GetCurrentPlayers();
			if (currentPlayerMode.Code == TrackPlayerMode.RecentPlayers.Code)
				return _playerTrackPlugin.GetRecentPlayers();
			if (currentPlayerMode.Code == TrackPlayerMode.AllPlayers.Code) return _playerTrackPlugin.GetAllPlayers();
			if (currentPlayerMode.Code == TrackPlayerMode.SearchForPlayers.Code && !string.IsNullOrEmpty(_activeSearch))
				return _playerTrackPlugin.GetPlayersByName(_activeSearch);

			return new Dictionary<string, TrackPlayer>();
		}

		private void PlayerList(Dictionary<string, TrackPlayer> players)
		{
			if (players != null && players.Count > 0)
			{
				PlayerCount(players.Count);
				ImGui.Spacing();
				foreach (var player in players.ToList().Select(playerEntry => playerEntry.Value)) PlayerRow(player);
			}
			else
			{
				ImGui.Text(Loc.Localize("NoPlayers", "No players to show..."));
			}
		}

		private void PlayerCount(int count)
		{
			if (!_playerTrackPlugin.Configuration.ShowPlayerCount) return;
			ImGui.Text(Loc.Localize("PlayerCount", "Count") + ": " + count);
			ImGui.Separator();
		}

		private void PlayerRow(TrackPlayer player)
		{
			PlayerIcon(player, player.Color ?? _playerTrackPlugin.Configuration.DefaultColor);
			ImGui.TextColored(player.Color ?? _playerTrackPlugin.Configuration.DefaultColor, player.Name);
			if (ImGui.IsItemClicked())
			{
				_playerTrackPlugin.RosterService.ChangeSelectedPlayer(player.Key);
				_currentView = View.PlayerDetail;
			}
		}

		private void PlayerIcon(TrackPlayer player, Vector4 color)
		{
			if (!_playerTrackPlugin.Configuration.ShowIcons) return;
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.TextColored(color, player.Icon != 0
				? ((FontAwesomeIcon) player.Icon).ToIconString()
				: _playerTrackPlugin.Configuration.DefaultIcon.ToIconString());
			ImGui.PopFont();
			ImGui.SameLine();
		}

		private void SetCurrentPlayer()
		{
			if (_currentPlayer == null)
			{
				var player = _playerTrackPlugin?.RosterService?.SelectedPlayer;
				if (player == null)
				{
					_currentView = View.Players;
					return;
				}

				_currentPlayer = player;
				_currentPlayer.ClearBackingFields();
			}
		}

		private void Controls(TrackPlayer player)
		{
			if (ImGui.SmallButton(Loc.Localize("Back", "Back") + "###PlayerTrack_Back_Button"))
				_currentView = View.Players;
			ImGui.SameLine();
			if (ImGui.SmallButton(Loc.Localize("Delete", "Delete") + "###PlayerTrack_Delete_Button"))
			{
				_playerTrackPlugin.RosterService.DeletePlayer(_currentPlayer.Key);
				_currentView = View.Players;
			}

			ImGui.SameLine();
			if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTracker_DetailsReset_Button"))
			{
				player.Icon = 0;
				player.Color = null;
			}

			ImGui.Separator();
		}

		private void PlayerInfo(TrackPlayer player)
		{
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerInfo", "Player Info"));
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerStats", "Player Stats"));

			if (player.Names?.Count > 1)
				CustomWidgets.Text(Loc.Localize("PlayerName", "Name"), player.Name, string.Join(", ", player.Names));
			else
				CustomWidgets.Text(Loc.Localize("PlayerName", "Name"), player.Name);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);

			CustomWidgets.Text(Loc.Localize("PlayerFirstSeen", "First Seen"), player.FirstSeen);
			if (player.HomeWorlds?.Count > 1)
				CustomWidgets.Text(Loc.Localize("PlayerHomeWorld", "World"), player.HomeWorld,
					string.Join(", ", player.HomeWorlds.Select(world => world.Name)));
			else
				CustomWidgets.Text(Loc.Localize("PlayerHomeWorld", "World"), player.HomeWorld);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);

			CustomWidgets.Text(Loc.Localize("PlayerLastSeen", "Last Seen"), player.LastSeen);
			CustomWidgets.Text(Loc.Localize("PlayerFreeCompany", "Free Company"), player.FreeCompany);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("PlayerSeenCount", "Seen Count"), player.SeenCount);
		}

		private void PlayerLodestone(TrackPlayer player)
		{
			if (!_playerTrackPlugin.Configuration.SyncToLodestone) return;
			ImGui.Spacing();
			ImGui.TextColored(UIColor.Violet, Loc.Localize("LodestoneInfo", "Lodestone"));
			CustomWidgets.Text(Loc.Localize("LodestoneStatus", "Status"), player.Lodestone.Status.ToString());
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("LodestoneLastUpdated", "Last Updated"),
				player.Lodestone.LastUpdatedDisplay);
		}

		private void PlayerNotes(TrackPlayer player)
		{
			ImGui.Spacing();
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerNotes", "Notes"));

			if (_playerTrackPlugin.Configuration.ShowIcons)
			{
				var namesList = new List<string> {Loc.Localize("Default", "Default")};
				namesList.AddRange(_playerTrackPlugin.Configuration.EnabledIcons.ToList()
					.Select(icon => icon.ToString()));
				var names = namesList.ToArray();

				var codesList = new List<int> {0};
				codesList.AddRange(_playerTrackPlugin.Configuration.EnabledIcons.ToList().Select(icon => (int) icon));
				var codes = codesList.ToArray();
				var iconIndex = Array.IndexOf(codes, player.Icon);
				ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
				if (ImGui.Combo("###PlayerTrack_SelectIcon_Combo", ref iconIndex,
					names,
					names.Length))
					player.Icon = codes[iconIndex];
				ImGui.SameLine();
				PlayerIcon(player, _playerTrackPlugin.Configuration.DefaultColor);
				ImGui.Spacing();
			}

			ImGui.SameLine();

			var color = player.Color ?? _playerTrackPlugin.Configuration.DefaultColor;
			if (ImGui.ColorButton("###PlayerTracker_PlayerColor_Button", color)
			) ImGui.OpenPopup("###PlayerTracker_PlayerColor_Popup");

			if (ImGui.BeginPopup("###PlayerTracker_PlayerColor_Popup"))
			{
				if (ImGui.ColorPicker4("###PlayerTracker_PlayerColor_ColorPicker", ref color)) player.Color = color;
				Palette(player);
				ImGui.EndPopup();
			}

			var notes = player.Notes;
			if (ImGui.InputTextMultiline("###PlayerTrack_PlayerNotes_InputText", ref notes, 128,
				new Vector2(ImGui.GetWindowSize().X - 25f * Scale, 80f * Scale))) player.Notes = notes;
		}

		private void Palette(TrackPlayer player)
		{
			SwatchRow(player, 0, 8);
			SwatchRow(player, 8, 16);
			SwatchRow(player, 16, 24);
			SwatchRow(player, 24, 32);
		}

		private void SwatchRow(TrackPlayer player, int min, int max)
		{
			ImGui.Spacing();
			for (var i = min; i < max; i++)
			{
				if (ImGui.ColorButton("###PlayerTracker_PlayerColor_Swatch" + i, _colorPalette[i]))
					player.Color = _colorPalette[i];
				ImGui.SameLine();
			}
		}

		private void PlayerEncounters(List<TrackEncounter> encounters)
		{
			ImGui.Spacing();
			ImGui.BeginChild("###PlayerTrack_Encounters_Widget",
				new Vector2(ImGui.GetWindowSize().X - 10f * Scale, 130f * Scale),
				false,
				ImGuiWindowFlags.AlwaysVerticalScrollbar);
			ImGui.TextColored(UIColor.Violet, Loc.Localize("Encounters", "Encounters"));

			var col1 = 110f * Scale;
			var col2 = 220f * Scale;
			var col3 = 260f * Scale;
			var col4 = 295f * Scale;

			ImGui.Text(Loc.Localize("EncounterTime", "Time"));
			ImGui.SameLine(col1);
			ImGui.Text(Loc.Localize("EncounterDuration", "Duration"));
			ImGui.SameLine(col2);
			ImGui.Text(Loc.Localize("EncounterJob", "Job"));
			ImGui.SameLine(col3);
			ImGui.Text(Loc.Localize("EncounterJobLevel", "Lvl"));
			ImGui.SameLine(col4);
			ImGui.Text(Loc.Localize("EncounterLocation", "Location"));

			if (encounters != null && encounters.Count > 0)
				foreach (var encounter in encounters.AsEnumerable().Reverse())
				{
					ImGui.Text(encounter.Time);
					ImGui.SameLine(col1);
					ImGui.Text(encounter.Duration);
					ImGui.SameLine(col2);
					ImGui.Text(encounter.Job.Code);
					ImGui.SameLine(col3);
					ImGui.Text(encounter.Job.Lvl.ToString());
					ImGui.SameLine(col4);
					ImGui.Text(encounter.Location.ToString());
				}

			ImGui.EndChild();
		}

		private enum View
		{
			Players,
			PlayerDetail
		}
	}
}