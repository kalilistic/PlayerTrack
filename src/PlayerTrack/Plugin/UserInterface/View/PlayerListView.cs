// ReSharper disable InvertIf
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InconsistentNaming
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault

using System;
using System.Collections.Generic;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface;
using ImGuiNET;

namespace PlayerTrack
{
	public class PlayerListView : WindowBase
	{
		public delegate void AddPlayerEventHandler(string playerName, string worldName);

		public delegate void CategoryFilterEventHandler(int categoryIndex);

		public delegate void HoverPlayerEventHandler(int actorId);

		public delegate void OpenPlayerEventHandler(string playerKey);

		public delegate void SearchEventHandler(string input);

		public delegate void StopHoverPlayerEventHandler();

		public delegate void TargetPlayerEventHandler(int actorId);

		public delegate void ViewModeEventHandler(TrackViewMode trackViewMode);

		public enum PlayerListModal
		{
			None,
			InvalidCharacterName,
			DuplicateCharacter,
			AddCharacterSuccess
		}

		private string _addPlayerInput = string.Empty;
		private int _currentHoverPlayer;
		private string _searchInput = string.Empty;
		private int _selectedWorld;
		private bool _usedHover;
		public string[] CategoryNames;
		public PlayerTrackConfig Configuration;
		public PlayerListModal CurrentModal = PlayerListModal.None;
		public List<TrackViewPlayer> Players;
		public TrackViewMode TrackViewMode = TrackViewMode.CurrentPlayers;
		public string[] WorldNames;
		public event ViewModeEventHandler ViewModeChanged;
		public event SearchEventHandler NewSearch;
		public event AddPlayerEventHandler AddPlayer;
		public event OpenPlayerEventHandler OpenPlayer;
		public event TargetPlayerEventHandler TargetPlayer;
		public event HoverPlayerEventHandler HoverPlayer;
		public event StopHoverPlayerEventHandler StopHoverPlayer;
		public event CategoryFilterEventHandler NewCategoryFilter;
		public event EventHandler<bool> ConfigUpdated;

		public override void DrawView()
		{
			if (!IsVisible) return;
			ImGui.SetNextWindowSize(new Vector2(200 * Scale, 250 * Scale), ImGuiCond.Always);
			if (ImGui.Begin(Loc.Localize("PlayerListView", "PlayerTrack") + "###PlayerTrack_PlayerList_View",
				CalcWindowFlags()))
			{
				SelectView();
				PlayerSearchInput();
				PlayersByCategory();
				PlayerList();
				PlayerAddInput();
				OpenModals();
			}

			ImGui.End();
		}

		private ImGuiWindowFlags CalcWindowFlags()
		{
			var flags = ImGuiWindowFlags.None | ImGuiWindowFlags.NoResize;
			if (Configuration.LockOverlay) flags |= ImGuiWindowFlags.NoMove;

			return flags;
		}

		private void OpenModals()
		{
			switch (CurrentModal)
			{
				case PlayerListModal.None:
					break;
				case PlayerListModal.InvalidCharacterName:
					InvalidCharacterNameModal();
					break;
				case PlayerListModal.DuplicateCharacter:
					DuplicateCharacterModal();
					break;
				case PlayerListModal.AddCharacterSuccess:
					AddCharacterSuccessModal();
					break;
			}
		}

		private void InvalidCharacterNameModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("InvalidCharacterNameTitle", "Invalid Name") + "###PlayerTrack_InvalidCharacterName_Modal",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("InvalidCharacterNameModalContent", "Invalid character name."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_InvalidCharacterNameModal_Button"))
				CurrentModal = PlayerListModal.None;
			ImGui.End();
		}

		private void DuplicateCharacterModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("DuplicateCharacterTitle", "Duplicate Player") + "###PlayerTrack_DuplicateCharacter_Modal",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("DuplicateCharacterModalContent",
				"There's already a player with that name/world."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_DuplicateCharacterModal_Button"))
				CurrentModal = PlayerListModal.None;
			ImGui.End();
		}

		private void AddCharacterSuccessModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("AddCharacterSuccessTitle", "Success") + "###PlayerTrack_AddCharacterSuccess_Modal",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("AddCharacterSuccessModalContent", "Character added successfully."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_AddCharacterSuccessModal_Button"))
			{
				ViewModeChanged?.Invoke(TrackViewMode);
				CurrentModal = PlayerListModal.None;
				_selectedWorld = 0;
				_addPlayerInput = string.Empty;
			}

			ImGui.End();
		}

		private void PlayerAddInput()
		{
			if (TrackViewMode.Code == TrackPlayerMode.AddPlayer.Code)
			{
				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale);
				if (ImGui.Combo("###PlayerTrack_PlayerAdd_Combo", ref _selectedWorld,
					WorldNames,
					WorldNames.Length))

					ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale / 1.3f);
				ImGui.InputTextWithHint("###PlayerTrack_PlayerNameAdd_Input",
					Loc.Localize("PlayerNameAddHint", "player name"), ref _addPlayerInput, 30);

				ImGui.SameLine();
				if (ImGui.Button(Loc.Localize("PlayerAdd", "Add") + "###PlayerTrack_PlayerAdd_Button"))
					AddPlayer?.Invoke(_addPlayerInput, WorldNames[_selectedWorld]);
			}
		}

		private void PlayerSearchInput()
		{
			if (TrackViewMode.Code == TrackPlayerMode.SearchForPlayers.Code)
			{
				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale / 1.5f);
				ImGui.InputTextWithHint("###PlayerTrack_PlayerNameSearch_Input",
					Loc.Localize("PlayerNameSearchHint", "player name"), ref _searchInput, 30);
				ImGui.SameLine();
				if (ImGui.Button(Loc.Localize("PlayerSearch", "Search") + "###PlayerTrack_PlayerSearch_Button"))
					NewSearch?.Invoke(_searchInput);
			}
		}

		private void PlayersByCategory()
		{
			if (TrackViewMode.Code == TrackPlayerMode.PlayersByCategory.Code)
			{
				var selectedCategory = Configuration.SelectedCategory;
				ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale);
				if (ImGui.Combo("###PlayerTrack_PlayersByCategory_Combo", ref selectedCategory,
					CategoryNames,
					CategoryNames.Length))
				{
					Configuration.SelectedCategory = selectedCategory;
					ConfigUpdated?.Invoke(this, true);
					NewCategoryFilter?.Invoke(selectedCategory);
				}
			}
		}

		private void SelectView()
		{
			var viewIndex = TrackViewMode.Index;
			ImGui.SetNextItemWidth((ImGui.GetWindowSize().X - 30f) * Scale);
			if (ImGui.Combo("###PlayerTrack_ViewMode_Combo", ref viewIndex,
				TrackPlayerMode.ViewNames.ToArray(),
				TrackPlayerMode.ViewNames.Count))
			{
				TrackViewMode = TrackViewMode.GetViewModeByIndex(viewIndex);
				ViewModeChanged?.Invoke(TrackViewMode);
			}
		}

		private void PlayerList()
		{
			if (TrackViewMode == TrackViewMode.AddPlayer) return;
			if (Players != null && Players.Count > 0)
			{
				ImGui.Spacing();
				var noHover = true;
				foreach (var player in Players)
				{
					ImGui.BeginGroup();
					ImGui.PushFont(UiBuilder.IconFont);
					ImGui.TextColored(player.Color, player.Icon);
					ImGui.PopFont();
					ImGui.SameLine();
					ImGui.TextColored(player.Color, player.Name);
					ImGui.EndGroup();
					if (ImGui.IsItemClicked(0)) OpenPlayer?.Invoke(player.Key);
					if (ImGui.IsItemClicked(1)) TargetPlayer?.Invoke(player.ActorId);
					if (ImGui.IsItemHovered())
					{
						noHover = false;
						_usedHover = true;
						if (player.ActorId != _currentHoverPlayer)
						{
							_currentHoverPlayer = player.ActorId;
							HoverPlayer?.Invoke(player.ActorId);
						}
					}
				}

				if (noHover && _usedHover)
				{
					_usedHover = false;
					_currentHoverPlayer = 0;
					StopHoverPlayer?.Invoke();
				}
			}
			else
			{
				ImGui.Text(Loc.Localize("NoPlayers", "No players to show..."));
			}
		}
	}
}