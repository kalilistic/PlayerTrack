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
	public class PlayerDetailView : WindowBase
	{
		public delegate void DeletePlayerEventHandler(string playerKey);

		public delegate void ResetPlayerEventHandler(string playerKey);

		public delegate void SavePlayerEventHandler(TrackViewPlayerDetail player);

		public enum PlayerDetailModal
		{
			None,
			LodestoneUnavailable,
			ConfirmDelete,
			ConfirmReset
		}

		private readonly List<Vector4> _colorPalette = ImGuiUtil.CreatePalette();
		public PlayerTrackConfig Configuration;
		public PlayerDetailModal CurrentModal = PlayerDetailModal.None;
		public TrackViewPlayerDetail Player;
		public int SelectedCategory;
		public int SelectedIcon;
		public event ResetPlayerEventHandler ResetPlayer;
		public event DeletePlayerEventHandler DeletePlayer;
		public event SavePlayerEventHandler SavePlayer;

		public override void DrawView()
		{
			if (!IsVisible) return;
			if (Player == null) return;
			var isVisible = IsVisible;
			ImGui.SetNextWindowSize(new Vector2(460 * Scale, CalcHeight()), ImGuiCond.Always);
			if (ImGui.Begin(Loc.Localize("PlayerDetailView", "PlayerTrack") + "###PlayerTrack_PlayerDetail_View",
				ref isVisible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar))
			{
				IsVisible = isVisible;
				Controls();
				PlayerInfo();
				PlayerCharacterDetails();
				DisplaySettings();
				PlayerNotes();
				PlayerEncounters();
				OpenModals();
			}

			ImGui.End();
		}

		private float CalcHeight()
		{
			var baseHeight = 450;
			if (Configuration.ShowPlayerOverride) baseHeight += 30;
			if (Configuration.ShowPlayerCharacterDetails) baseHeight += 70;
			return baseHeight * Scale;
		}

		private void OpenModals()
		{
			switch (CurrentModal)
			{
				case PlayerDetailModal.None:
					break;
				case PlayerDetailModal.LodestoneUnavailable:
					LodestoneModal();
					break;
				case PlayerDetailModal.ConfirmReset:
					ResetConfirmationModal();
					break;
				case PlayerDetailModal.ConfirmDelete:
					DeleteConfirmationModal();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void Controls()
		{
			if (ImGui.SmallButton(Loc.Localize("Save", "Save") + "###PlayerTrack_Save_Button"))
			{
				SavePlayer?.Invoke(Player);
				HideView();
			}

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("Cancel", "Cancel") + "###PlayerTrack_Cancel_Button")) HideView();

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("DeleteConfirmation", "Delete") + "###PlayerTrack_Delete_Button"))
				CurrentModal = PlayerDetailModal.ConfirmDelete;

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("ResetConfirmation", "Reset") + "###PlayerTrack_DetailsReset_Button"))
				CurrentModal = PlayerDetailModal.ConfirmReset;

			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("Lodestone", "Lodestone") + "###PlayerTrack_Lodestone_Button"))
			{
				if (!string.IsNullOrEmpty(Player.LodestoneUrl))
					Process.Start(Player.LodestoneUrl);
				else
					CurrentModal = PlayerDetailModal.LodestoneUnavailable;
			}


			ImGui.Spacing();
			ImGui.Separator();
		}

		private void PlayerInfo()
		{
			// headings
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerInfo", "Player Info"));
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerStats", "Player Stats"));

			// row 1
			if (!string.IsNullOrEmpty(Player.PreviousNames))
			{
				ImGui.Text(Loc.Localize("PlayerName", "Name") + ": ");
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGui.Text(Player.Name);
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.TextColored(UIColor.Yellow, FontAwesomeIcon.InfoCircle.ToIconString());
				ImGui.PopFont();
				ImGui.EndGroup();
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip(string.Format(Loc.Localize("PlayerPreviousNames", "Previously known as {0}"),
						Player.PreviousNames));
			}
			else
			{
				CustomWidgets.Text(Loc.Localize("PlayerName", "Name"), Player.Name);
			}

			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("PlayerFirstSeen", "First Seen"), Player.FirstSeen);

			// row 2
			if (!string.IsNullOrEmpty(Player.PreviousHomeWorlds))
			{
				ImGui.Text(Loc.Localize("PlayerHomeWorld", "World") + ": ");
				ImGui.SameLine();
				ImGui.BeginGroup();
				ImGui.Text(Player.HomeWorld);
				ImGui.SameLine();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.TextColored(UIColor.Yellow, FontAwesomeIcon.InfoCircle.ToIconString());
				ImGui.PopFont();
				ImGui.EndGroup();
				if (ImGui.IsItemHovered())
					ImGui.SetTooltip(string.Format(Loc.Localize("PlayerPreviousWorlds", "Previously on {0}"),
						Player.PreviousHomeWorlds));
			}
			else
			{
				CustomWidgets.Text(Loc.Localize("PlayerHomeWorld", "World"), Player.HomeWorld);
			}

			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("PlayerLastSeen", "Last Seen"), Player.LastSeen);

			// row 3
			CustomWidgets.Text(Loc.Localize("PlayerFreeCompany", "Free Company"),
				Player.FreeCompany);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			CustomWidgets.Text(Loc.Localize("PlayerSeenCount", "Seen Count"), Player.SeenCount);

			// row 4
			CustomWidgets.Text(Loc.Localize("LodestoneStatus", "Lodestone Status"), Player.LodestoneStatus);

			ImGui.Spacing();
		}

		private void PlayerCharacterDetails()
		{
			if (!Configuration.ShowPlayerCharacterDetails) return;

			// headings 2
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerCustomize", "Player Character"));

			// row 5
			CustomWidgets.Text(Loc.Localize("PlayerRace", "Race"), Player.Race);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			if (Player.Gender.Equals("N/A"))
			{
				CustomWidgets.Text(Loc.Localize("PlayerGender", "Gender"), Player.Gender);
			}
			else
			{
				CustomWidgets.Text(Loc.Localize("PlayerGender", "Gender"), string.Empty);
				ImGui.SameLine(Scale * 300f);
				ImGui.PushFont(UiBuilder.IconFont);
				CustomWidgets.Text(Loc.Localize("PlayerGender", "Gender"), Player.Gender);
				ImGui.PopFont();
			}

			// row 6
			CustomWidgets.Text(Loc.Localize("PlayerTribe", "Tribe"), Player.Tribe);
			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			if (Player.Height.Equals("N/A"))
				CustomWidgets.Text(Loc.Localize("PlayerHeight", "Height"), Player.Height);
			else
				CustomWidgets.Text(Loc.Localize("PlayerHeight", "Height"), string.Format(
					Loc.Localize("PlayerHeightValue", "{0} in"),
					Player.Height));

			ImGui.Spacing();
		}

		private void DisplaySettings()
		{
			ImGui.TextColored(UIColor.Violet, Loc.Localize("DisplayOptions", "Display Options"));
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
			if (ImGui.Combo("###PlayerTrack_DisplaySettings_Combo", ref SelectedCategory,
				Player.CategoryNames,
				Player.CategoryNames.Length))
				Player.CategoryIndex = SelectedCategory;
			if (Configuration.ShowPlayerOverride) PlayerOverride();
			ImGui.Spacing();
		}

		private void PlayerOverride()
		{
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 3);
			if (ImGui.Combo("###PlayerTrack_SelectIcon_Combo", ref SelectedIcon,
				Player.IconNames,
				Player.IconNames.Length))
			{
				Player.IconIndex = SelectedIcon;
				Player.Icon = ((FontAwesomeIcon) Player.IconCodes[SelectedIcon]).ToIconString();
			}

			ImGui.SameLine();
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.TextColored(Player.Color, Player.Icon);
			ImGui.PopFont();
			ImGui.SameLine();
			ImGui.Spacing();
			ImGui.SameLine();

			var color = Player.Color;
			if (ImGui.ColorButton("###PlayerTrack_PlayerColor_Button", color)
			) ImGui.OpenPopup("###PlayerTrack_PlayerColor_Popup");

			if (ImGui.BeginPopup("###PlayerTrack_PlayerColor_Popup"))
			{
				if (ImGui.ColorPicker4("###PlayerTrack_PlayerColor_ColorPicker", ref color)) Player.Color = color;
				PlayerSwatchRow(0, 8);
				PlayerSwatchRow(8, 16);
				PlayerSwatchRow(16, 24);
				PlayerSwatchRow(24, 32);
				ImGui.EndPopup();
			}

			ImGui.SameLine(ImGui.GetWindowSize().X / 2);
			var enableAlerts = Player.AlertEnabled;
			if (ImGui.Checkbox(
				Loc.Localize("EnablePlayerAlerts", "Enable Alerts") + "###PlayerTrack_PlayerAlerts_Checkbox",
				ref enableAlerts))
				Player.AlertEnabled = enableAlerts;
		}

		private void PlayerSwatchRow(int min, int max)
		{
			ImGui.Spacing();
			for (var i = min; i < max; i++)
			{
				if (ImGui.ColorButton("###PlayerTrack_PlayerColor_Swatch_" + i, _colorPalette[i]))
					Player.Color = _colorPalette[i];
				ImGui.SameLine();
			}
		}

		private void PlayerNotes()
		{
			ImGui.TextColored(UIColor.Violet, Loc.Localize("PlayerNotes", "Notes"));
			var notes = Player.Notes;
			if (ImGui.InputTextMultiline("###PlayerTrack_PlayerNotes_InputText", ref notes, 128,
				new Vector2(ImGui.GetWindowSize().X - 25f * Scale, 80f * Scale)))
				Player.Notes = notes;

			ImGui.Spacing();
		}

		private void PlayerEncounters()
		{
			ImGui.BeginChild("###PlayerTrack_Encounters_Widget",
				new Vector2(ImGui.GetWindowSize().X - 10f * Scale, 130f * Scale),
				false);
			ImGui.TextColored(UIColor.Violet, Loc.Localize("Encounters", "Encounters"));

			if (Player.Encounters.Count > 0)
			{
				var col1 = 60f * Scale;
				var col2 = 130f * Scale;
				var col3 = 170f * Scale;
				var col4 = 200f * Scale;

				ImGui.Text(Loc.Localize("EncounterTime", "Time"));
				ImGui.SameLine(col1);
				ImGui.Text(Loc.Localize("EncounterDuration", "Duration"));
				ImGui.SameLine(col2);
				ImGui.Text(Loc.Localize("EncounterJob", "Job"));
				ImGui.SameLine(col3);
				ImGui.Text(Loc.Localize("EncounterJobLevel", "Lvl"));
				ImGui.SameLine(col4);
				ImGui.Text(Loc.Localize("EncounterLocation", "Location"));

				foreach (var encounter in Player.Encounters.ToList())
				{
					ImGui.Text(encounter.Time);
					ImGui.SameLine(col1);
					ImGui.Text(encounter.Duration);
					ImGui.SameLine(col2);
					ImGui.Text(encounter.JobCode);
					ImGui.SameLine(col3);
					ImGui.Text(encounter.JobLvl);
					ImGui.SameLine(col4);
					ImGui.Text(encounter.Location);
				}
			}
			else
			{
				ImGui.Text(Loc.Localize("NoEncounters", "No encounters to show..."));
			}


			ImGui.EndChild();
		}

		private void LodestoneModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(Loc.Localize("LodestoneModalTitle", "Lodestone") + "###PlayerTrack_LodestoneModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("LodestoneModalContent", "Lodestone verification pending."));
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_LodestoneModal_Button"))
				CurrentModal = PlayerDetailModal.None;
			ImGui.End();
		}

		private void ResetConfirmationModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("ResetConfirmationModalTitle", "ResetConfirmation Confirmation") +
				"###PlayerTrack_ResetConfirmationModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("ResetConfirmationModalContent", "Are you sure you want to reset?"));
			ImGui.Spacing();
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_ResetConfirmationModalOK_Button"))
			{
				ResetPlayer?.Invoke(Player.Key);
				CurrentModal = PlayerDetailModal.None;
				SelectedIcon = 0;
				SelectedCategory = 0;
			}

			ImGui.SameLine();
			if (ImGui.Button(Loc.Localize("Cancel", "Cancel") + "###PlayerTrack_ResetConfirmationModalCancel_Button"))
				CurrentModal = PlayerDetailModal.None;
			ImGui.End();
		}

		private void DeleteConfirmationModal()
		{
			ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
				ImGuiCond.Appearing);
			ImGui.Begin(
				Loc.Localize("DeleteConfirmationModalTitle", "Delete Confirmation") +
				"###PlayerTrack_DeleteConfirmationModal_Window",
				ImGuiUtil.ModalWindowFlags());
			ImGui.Text(Loc.Localize("DeleteConfirmationModalContent", "Are you sure you want to delete?"));
			ImGui.Spacing();
			if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_DeleteConfirmationModalOK_Button"))
			{
				DeletePlayer?.Invoke(Player.Key);
				CurrentModal = PlayerDetailModal.None;
				HideView();
			}

			ImGui.SameLine();
			if (ImGui.Button(Loc.Localize("Cancel", "Cancel") +
			                 "###PlayerTrack_DeleteConfirmationModalCancel_Button"))
				CurrentModal = PlayerDetailModal.None;
			ImGui.End();
		}
	}
}