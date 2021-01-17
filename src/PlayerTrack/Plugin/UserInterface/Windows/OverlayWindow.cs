// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private string _addPlayerInput = string.Empty;
		private Modal _currentModal = Modal.None;
		private TrackPlayer _currentPlayer;
		private TrackPlayerMode _currentPlayerMode = TrackPlayerMode.CurrentPlayers;
		private View _currentView = View.Players;
		private string _searchInput = string.Empty;
		private int _selectedWorld;

		public OverlayWindow(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
		}

		public void DrawWindow()
		{
			if (!_playerTrackPlugin.IsLoggedIn()) return;
			if (!IsVisible) return;
			if (ImGui.Begin(Loc.Localize("OverlayWindow", "PlayerTrack") + "###PlayerTrack_Overlay_Window",
				ref IsVisible, ImGuiWindowFlags.NoResize))
				SelectCurrentView();
			ImGui.End();
			OpenModals();
		}

		private void OpenModals()
		{
			if (_playerTrackPlugin.Configuration.Enabled)
				switch (_currentModal)
				{
					case Modal.None:
						break;
					case Modal.Lodestone:
						LodestoneModal();
						break;
					case Modal.ResetConfirmation:
						ResetConfirmationModal();
						break;
					case Modal.DeleteConfirmation:
						DeleteConfirmationModal();
						break;
					case Modal.DeleteComplete:
						DeleteCompleteModal();
						break;
					case Modal.InvalidCharacterName:
						InvalidCharacterNameModal();
						break;
					case Modal.DuplicateCharacter:
						DuplicateCharacterModal();
						break;
				}
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
			PlayerAddInput();
			PlayerList(GetPlayers(TrackPlayerMode.GetPlayerModeByIndex(_currentPlayerMode.Index)));
		}

		private void DrawPlayerDetailView()
		{
			ImGui.SetWindowSize(new Vector2(500 * Scale, 550 * Scale), ImGuiCond.Always);
			SetCurrentPlayer();
			Controls(_currentPlayer);
			PlayerInfo(_currentPlayer);
			PlayerDisplay(_currentPlayer);
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

		private void PlayerAddInput()
		{
			if (_currentPlayerMode.Code == TrackPlayerMode.AddPlayer.Code)
			{
				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale);
				var worldNames = _playerTrackPlugin.GetWorldNames();
				if (ImGui.Combo("###PlayerTrack_PlayerAdd_Combo", ref _selectedWorld,
					worldNames.ToArray(),
					worldNames.Count))

					ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale / 1.3f);
				ImGui.InputTextWithHint("###PlayerTrack_PlayerNameAdd_Input",
					Loc.Localize("PlayerNameAddHint", "player name"), ref _addPlayerInput, 30);

				ImGui.SameLine();
				if (ImGui.Button(Loc.Localize("PlayerAdd", "Add") + "###PlayerTrack_PlayerAdd_Button"))
				{
					if (!_playerTrackPlugin.IsValidCharacterName(_addPlayerInput))
					{
						_currentModal = Modal.InvalidCharacterName;
					}
					else if (!_playerTrackPlugin.RosterService.IsNewPlayer(_addPlayerInput, worldNames[_selectedWorld]))
					{
						_currentModal = Modal.DuplicateCharacter;
					}
					else
					{
						_playerTrackPlugin.RosterService.AddPlayer(_addPlayerInput, worldNames[_selectedWorld]);
						_selectedWorld = 0;
						_searchInput = _addPlayerInput;
						_activeSearch = _addPlayerInput;
						_addPlayerInput = string.Empty;
						_currentPlayerMode = TrackPlayerMode.SearchForPlayers;
					}
				}
			}
		}

		private Dictionary<string, TrackPlayer> GetPlayers(TrackPlayerMode currentPlayerMode)
		{
			if (currentPlayerMode.Code == TrackPlayerMode.CurrentPlayers.Code)
				return _playerTrackPlugin.RosterService.Current.Roster;
			if (currentPlayerMode.Code == TrackPlayerMode.RecentPlayers.Code)
				return _playerTrackPlugin.RosterService.Recent;
			if (currentPlayerMode.Code == TrackPlayerMode.AllPlayers.Code)
				return _playerTrackPlugin.RosterService.All.Roster;
			if (currentPlayerMode.Code == TrackPlayerMode.SearchForPlayers.Code && !string.IsNullOrEmpty(_activeSearch))
				return _playerTrackPlugin.RosterService.GetPlayersByName(_activeSearch);

			return null;
		}

		private void PlayerList(Dictionary<string, TrackPlayer> players)
		{
			if (_currentPlayerMode.Code == TrackPlayerMode.AddPlayer.Code) return;
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
			ImGui.Text(Loc.Localize("PlayerCount", "Count") + ": " + count);
			ImGui.Separator();
		}

		private void PlayerRow(TrackPlayer player)
		{
			var category = _playerTrackPlugin.RosterService.GetCategory(player.Key);
			PlayerIcon(player);
			ImGui.SameLine();
			ImGui.TextColored(player.Color ?? category.Color, player.Name);
			if (ImGui.IsItemClicked())
			{
				_playerTrackPlugin.RosterService.ChangeSelectedPlayer(player.Key);
				_currentView = View.PlayerDetail;
			}
		}

		private void PlayerIcon(TrackPlayer player)
		{
			var category = _playerTrackPlugin.RosterService.GetCategory(player.Key);
			var color = player.Color ?? category.Color;
			var iconValue = player.Icon;
			FontAwesomeIcon icon;
			if (iconValue != 0)
				icon = (FontAwesomeIcon) player.Icon;
			else if (category.Icon != 0)
				icon = (FontAwesomeIcon) category.Icon;
			else
				icon = FontAwesomeIcon.User;
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.TextColored(color, icon.ToIconString());
			ImGui.PopFont();
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

			if (ImGui.SmallButton(Loc.Localize("DeleteConfirmation", "Delete") + "###PlayerTrack_Delete_Button"))
				_currentModal = Modal.DeleteConfirmation;

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("ResetConfirmation", "Reset") + "###PlayerTracker_DetailsReset_Button"))
				_currentModal = Modal.ResetConfirmation;

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("Lodestone", "Lodestone") + "###PlayerTracker_Lodestone_Button"))
			{
				var url = player.Lodestone.GetProfileUrl(_playerTrackPlugin.Configuration.LodestoneLocale);
				if (url != null)
					Process.Start(url);
				else
					_currentModal = Modal.Lodestone;
			}

			ImGui.Separator();
		}

		private void LodestoneModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(Loc.Localize("LodestoneModalTitle", "Lodestone") + "###PlayerTracker_LodestoneModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("LodestoneModalContent", "Lodestone verification pending."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_LodestoneModal_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void InvalidCharacterNameModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("InvalidCharacterNameModalTitle", "Character Name") +
				"###PlayerTracker_InvalidCharacterNameModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("InvalidCharacterNameModalContent", "Invalid character name...try again!"));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_InvalidCharacterNameModal_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void DuplicateCharacterModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("DuplicateCharacterModalTitle", "Duplicate Character") +
				"###PlayerTracker_DuplicateCharacterModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("DuplicateCharacterModalContent",
				"There's already a character with that name/world."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_DuplicateCharacterModal_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void ResetConfirmationModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("ResetConfirmationModalTitle", "ResetConfirmation Confirmation") +
				"###PlayerTracker_ResetConfirmationModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("ResetConfirmationModalContent", "Are you sure you want to reset?"));
			ImGui.Spacing();
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_ResetConfirmationModalOK_Button"))
			{
				_currentPlayer.Icon = 0;
				_currentPlayer.Color = null;
				_currentPlayer.Notes = string.Empty;
				_currentPlayer.CategoryId = _playerTrackPlugin.GetCategoryService().GetDefaultCategory().Id;
				_playerTrackPlugin.GetCategoryService().SetPlayerPriority();
				_currentModal = Modal.None;
			}

			ImGui.SameLine();
			if (ImGui.Button(Loc.Localize("Cancel", "Cancel") + "###PlayerTracker_ResetConfirmationModalCancel_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void DeleteCompleteModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("DeleteCompleteModalTitle", "Delete Complete") +
				"###PlayerTracker_DeleteCompleteModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("DeleteCompleteContent1",
				"Character has been deleted and will be removed from your list shortly."));
			ImGui.Text(Loc.Localize("DeleteCompleteContent2",
				"The next time you encounter this character they'll be added back."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_DeleteCompleteModal_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void DeleteConfirmationModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("DeleteConfirmationModalTitle", "Delete Confirmation") +
				"###PlayerTracker_DeleteConfirmationModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("DeleteConfirmationModalContent", "Are you sure you want to delete?"));
			ImGui.Spacing();
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTracker_DeleteConfirmationModalOK_Button"))
			{
				_playerTrackPlugin.RosterService.DeletePlayer(_currentPlayer.Key);
				_currentView = View.Players;
				_currentModal = Modal.DeleteComplete;
			}

			ImGui.SameLine();
			if (ImGui.Button(Loc.Localize("Cancel", "Cancel") +
			                 "###PlayerTracker_DeleteConfirmationModalCancel_Button"))
				_currentModal = Modal.None;
			ImGui.End();
		}

		private void PlayerInfo(TrackPlayer player)
		{
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerInfo", "Player Info"));
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerStats", "Player Stats"));

			if (player.Names?.Count > 1)
				CustomWidgets.Text(Loc.Localize("PlayerName", "Name"), player.Name,
					string.Format(Loc.Localize("PlayerPreviousNames", "Previously known as {0}"),
						player.PreviousNames));
			else
				CustomWidgets.Text(Loc.Localize("PlayerName", "Name"), player.Name);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);

			CustomWidgets.Text(Loc.Localize("PlayerFirstSeen", "First Seen"), player.FirstSeen);
			if (player.HomeWorlds?.Count > 1)
				CustomWidgets.Text(Loc.Localize("PlayerHomeWorld", "World"), player.HomeWorld,
					string.Format(Loc.Localize("PlayerPreviousWorlds", "Previously on {0}"), player.PreviousWorlds));
			else
				CustomWidgets.Text(Loc.Localize("PlayerHomeWorld", "World"), player.HomeWorld);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);

			CustomWidgets.Text(Loc.Localize("PlayerLastSeen", "Last Seen"), player.LastSeen);
			CustomWidgets.Text(Loc.Localize("PlayerFreeCompany", "Free Company"),
				player.FreeCompanyDisplay(_playerTrackPlugin.InContent()));
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("PlayerSeenCount", "Seen Count"), player.SeenCount);
			CustomWidgets.Text(Loc.Localize("LodestoneStatus", "Lodestone Status"), player.Lodestone.Status.ToString());
		}

		private void PlayerDisplay(TrackPlayer player)
		{
			var category = _playerTrackPlugin.RosterService.GetCategory(player.Key);
			ImGui.Spacing();
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerDisplay", "Display Options"));
			var categoryNames = _playerTrackPlugin.GetCategoryService().GetCategoryNames();
			var categoryIndexLookup = _playerTrackPlugin.GetCategoryService()?.GetCategoryIndex(category?.Name);
			if (categoryIndexLookup == null) return;
			var categoryIndex = (int) categoryIndexLookup;
			ImGui.Text(Loc.Localize("PlayerCategory", "Category"));
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
			if (ImGui.Combo("###PlayerTrack_PlayerCategory_Combo", ref categoryIndex,
				categoryNames,
				categoryNames.Length))
			{
				player.Icon = 0;
				player.Color = null;
				player.CategoryId = _playerTrackPlugin.GetCategoryService().Categories[categoryIndex].Id;
				_playerTrackPlugin.GetCategoryService().SetPlayerPriority();
			}

			var namesList = new List<string> {Loc.Localize("Default", "Default")};
			namesList.AddRange(_playerTrackPlugin.Configuration.EnabledIcons.ToList()
				.Select(icon => icon.ToString()));
			var names = namesList.ToArray();
			var codesList = new List<int> {0};
			codesList.AddRange(_playerTrackPlugin.Configuration.EnabledIcons.ToList().Select(icon => (int) icon));
			var codes = codesList.ToArray();
			if (category?.Icon == null) return;
			var iconIndex = Array.IndexOf(codes, player.Icon != 0 ? player.Icon : category.Icon);
			ImGui.Spacing();

			ImGui.Text(Loc.Localize("PlayerOverride", "Custom"));
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
			if (ImGui.Combo("###PlayerTrack_SelectIcon_Combo", ref iconIndex,
				names,
				names.Length))
				player.Icon = codes[iconIndex];
			ImGui.SameLine();
			PlayerIcon(player);

			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			var color = player.Color ?? category.Color;
			if (ImGui.ColorButton("###PlayerTracker_PlayerColor_Button", color)
			) ImGui.OpenPopup("###PlayerTracker_PlayerColor_Popup");

			if (ImGui.BeginPopup("###PlayerTracker_PlayerColor_Popup"))
			{
				if (ImGui.ColorPicker4("###PlayerTracker_PlayerColor_ColorPicker", ref color)) player.Color = color;
				PlayerPalette(player);
				ImGui.EndPopup();
			}
		}

		private void PlayerNotes(TrackPlayer player)
		{
			ImGui.Spacing();
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerNotes", " Player Notes"));
			var notes = player.Notes;
			if (ImGui.InputTextMultiline("###PlayerTrack_PlayerNotes_InputText", ref notes, 128,
				new Vector2(ImGui.GetWindowSize().X - 25f * Scale, 80f * Scale))) player.Notes = notes;
		}

		private void PlayerPalette(TrackPlayer player)
		{
			PlayerSwatchRow(player, 0, 8);
			PlayerSwatchRow(player, 8, 16);
			PlayerSwatchRow(player, 16, 24);
			PlayerSwatchRow(player, 24, 32);
		}

		private void PlayerSwatchRow(TrackPlayer player, int min, int max)
		{
			ImGui.Spacing();
			for (var i = min; i < max; i++)
			{
				if (ImGui.ColorButton("###PlayerTracker_PlayerColor_Swatch_" + i, _colorPalette[i]))
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

			if (_currentPlayer.GetEncounterCount() > 0)
			{
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
			}
			else
			{
				ImGui.Text(Loc.Localize("NoEncounters", "No encounters to show..."));
			}


			ImGui.EndChild();
		}

		private enum View
		{
			Players,
			PlayerDetail
		}

		private enum Modal
		{
			None,
			Lodestone,
			ResetConfirmation,
			DeleteConfirmation,
			DeleteComplete,
			InvalidCharacterName,
			DuplicateCharacter
		}
	}
}