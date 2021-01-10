// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using CheapLoc;
using Dalamud.Interface;
using ImGuiNET;
using Enumerable = System.Linq.Enumerable;

namespace PlayerTrack
{
	public class SettingsWindow : WindowBase
	{
		private readonly List<Vector4> _colorPalette = ImGuiUtil.CreatePalette();
		private readonly string[] _iconNames;
		private readonly FontAwesomeIcon[] _icons;
		private readonly IPlayerTrackPlugin _playerTrackPlugin;
		private Tab _currentTab = Tab.General;
		private int _selectedContentIndex;
		private int _selectedDefaultIconIndex;
		private int _selectedIconIndex = 4;

		public SettingsWindow(IPlayerTrackPlugin playerTrackPlugin)
		{
			_playerTrackPlugin = playerTrackPlugin;
			_icons = FontAwesomeUtil.Icons;
			_iconNames = FontAwesomeUtil.IconNames;
		}

		public event EventHandler<bool> OverlayVisibilityUpdated;

		public void DrawWindow()
		{
			if (!_playerTrackPlugin.IsLoggedIn()) return;
			if (!IsVisible) return;
			ImGui.SetNextWindowSize(new Vector2(550 * Scale, 420 * Scale), ImGuiCond.Appearing);
			ImGui.Begin(Loc.Localize("SettingsWindow", "PlayerTrack Settings") + "###PlayerTrack_Settings_Window",
				ref IsVisible,
				ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);
			DrawTabs();
			OpenCurrentTab();
			ImGui.End();
		}

		private void DrawTabs()
		{
			if (ImGui.BeginTabBar("###PlayerTrack_Settings_TabBar", ImGuiTabBarFlags.NoTooltip))
			{
				if (ImGui.BeginTabItem(Loc.Localize("General", "General") + "###PlayerTrack_General_Tab"))
				{
					_currentTab = Tab.General;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Display", "Display") + "###PlayerTrack_Display_Tab"))
				{
					_currentTab = Tab.Display;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Threshold", "Threshold") + "###PlayerTrack_Threshold_Tab"))
				{
					_currentTab = Tab.Threshold;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Filters", "Filters") + "###PlayerTrack_Filters_Tab"))
				{
					_currentTab = Tab.Filters;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Icons", "Icons") + "###PlayerTrack_Icons_Tab"))
				{
					_currentTab = Tab.Icons;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Lodestone", "Lodestone") + "###PlayerTrack_Lodestone_Tab"))
				{
					_currentTab = Tab.Lodestone;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Data", "Data") + "###PlayerTrack_Data_Tab"))
				{
					_currentTab = Tab.Data;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Alerts", "Alerts") + "###PlayerTrack_Alerts_Tab"))
				{
					_currentTab = Tab.Alerts;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Backup", "Backup") + "###PlayerTrack_Backup_Tab"))
				{
					_currentTab = Tab.Backup;
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem(Loc.Localize("Links", "Links") + "###PlayerTrack_Links_Tab"))
				{
					_currentTab = Tab.Links;
					ImGui.EndTabItem();
				}

				ImGui.EndTabBar();
				ImGui.Spacing();
			}
		}

		private void OpenCurrentTab()
		{
			switch (_currentTab)
			{
				case Tab.General:
				{
					DrawGeneral();
					break;
				}
				case Tab.Display:
				{
					DrawDisplay();
					break;
				}
				case Tab.Threshold:
				{
					DrawThreshold();
					break;
				}
				case Tab.Filters:
				{
					DrawFilters();
					break;
				}
				case Tab.Icons:
				{
					DrawIcons();
					break;
				}
				case Tab.Lodestone:
				{
					DrawLodestone();
					break;
				}
				case Tab.Data:
				{
					DrawData();
					break;
				}
				case Tab.Alerts:
				{
					DrawAlerts();
					break;
				}
				case Tab.Backup:
				{
					DrawBackup();
					break;
				}
				case Tab.Links:
				{
					DrawLinks();
					break;
				}
				default:
					DrawGeneral();
					break;
			}
		}

		private void DrawGeneral()
		{
			PluginEnabled();
			ShowOverlay();
			SetLanguage();
		}

		private void DrawDisplay()
		{
			PlayerTotal();
			ShowIcons();
			DefaultColor();
		}

		private void DrawThreshold()
		{
			RecentPlayerThreshold();
			NewEncounterThreshold();
		}

		private void DrawFilters()
		{
			RestrictInCombat();
			RestrictToContent();
			RestrictToHighEndDuty();
			RestrictToCustom();
		}

		private void DrawIcons()
		{
			DefaultIcon();
			IconList();
		}

		private void DrawLodestone()
		{
			SyncToLodestone();
			LodestoneMaxRetry();
			LodestoneRequestDelay();
			LodestoneTimeout();
			LodestoneUpdateFrequency();
			LodestoneCooldownDuration();
			LodestoneLocale();
		}

		private void DrawData()
		{
			Compressed();
			UpdateFrequency();
			SaveFrequency();
		}

		private void DrawAlerts()
		{
			EnableAlerts();
			EnableAlertsForAllPlayers();
			IncludeNotesInAlerts();
			AlertFrequency();
		}

		private void DrawBackup()
		{
			BackupFrequency();
			BackupRetention();
		}

		private void PluginEnabled()
		{
			var enabled = _playerTrackPlugin.Configuration.Enabled;
			if (ImGui.Checkbox(
				Loc.Localize("PluginEnabled", "Plugin Enabled") + "###PlayerTrack_PluginEnabled_Checkbox",
				ref enabled))
			{
				_playerTrackPlugin.Configuration.Enabled = enabled;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("PluginEnabled_HelpMarker",
				"toggle the plugin on/off"));
			ImGui.Spacing();
		}

		private void ShowOverlay()
		{
			var showOverlay = _playerTrackPlugin.Configuration.ShowOverlay;
			if (ImGui.Checkbox(Loc.Localize("ShowOverlay", "Show Overlay"),
				ref showOverlay))
			{
				_playerTrackPlugin.Configuration.ShowOverlay = showOverlay;
				OverlayVisibilityUpdated?.Invoke(this, showOverlay);
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("ShowOverlay_HelpMarker",
				"show overlay window"));
			ImGui.Spacing();
		}

		private void SetLanguage()
		{
			ImGui.Text(Loc.Localize("Language", "Language"));
			CustomWidgets.HelpMarker(Loc.Localize("Language_HelpMarker",
				"use default or override plugin ui language"));
			ImGui.Spacing();
			var pluginLanguage = _playerTrackPlugin.Configuration.PluginLanguage;
			if (ImGui.Combo("###PlayerTrack_Language_Combo", ref pluginLanguage,
				PluginLanguage.LanguageNames.ToArray(),
				PluginLanguage.LanguageNames.Count))
			{
				_playerTrackPlugin.Configuration.PluginLanguage = pluginLanguage;
				_playerTrackPlugin.SaveConfig();
				_playerTrackPlugin.Localization.SetLanguage(pluginLanguage);
			}
		}

		private void PlayerTotal()
		{
			var showPlayerTotal = _playerTrackPlugin.Configuration.ShowPlayerCount;
			if (ImGui.Checkbox(
				Loc.Localize("ShowPlayerTotal", "Show Player Total") + "###PlayerTrack_ShowPlayerTotal_Checkbox",
				ref showPlayerTotal))
			{
				_playerTrackPlugin.Configuration.ShowPlayerCount = showPlayerTotal;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("ShowPlayerTotal_HelpMarker",
				"show player totals on overlay"));
			ImGui.Spacing();
		}

		private void RecentPlayerThreshold()
		{
			ImGui.Text(Loc.Localize("RecentPlayerThreshold", "Recent Player Threshold (minutes)"));
			CustomWidgets.HelpMarker(Loc.Localize("RecentPlayerThreshold_HelpMarker",
				"amount of time players will appear on recent players list since last seen date"));
			var RecentPlayerThreshold =
				_playerTrackPlugin.Configuration.RecentPlayerThreshold.FromMillisecondsToMinutes();
			if (ImGui.SliderInt("###PlayerTrack_RecentPlayerThreshold_Slider", ref RecentPlayerThreshold, 1, 60))
			{
				_playerTrackPlugin.Configuration.RecentPlayerThreshold =
					RecentPlayerThreshold.FromMinutesToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void DefaultColor()
		{
			ImGui.Text(Loc.Localize("DefaultColor", "Default Color"));
			CustomWidgets.HelpMarker(Loc.Localize("DefaultColor_HelpMarker",
				"default color for players when not set specifically"));
			var color = _playerTrackPlugin.Configuration.DefaultColor;
			if (ImGui.ColorButton("###PlayerTrack_DefaultColor_Button", color)
			) ImGui.OpenPopup("###PlayerTrack_DefaultColor_Popup");
			if (ImGui.BeginPopup("###PlayerTrack_DefaultColor_Popup"))
			{
				if (ImGui.ColorPicker4("###PlayerTrack_DefaultColor_ColorPicker", ref color))
				{
					_playerTrackPlugin.Configuration.DefaultColor = color;
					_playerTrackPlugin.SaveConfig();
				}

				Palette();
				ImGui.EndPopup();
			}

			ImGui.SameLine();
			if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_DefaultColorReset_Button"))
			{
				_playerTrackPlugin.Configuration.DefaultColor = new Vector4(255, 255, 255, 1);
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void Palette()
		{
			SwatchRow(0, 8);
			SwatchRow(8, 16);
			SwatchRow(16, 24);
			SwatchRow(24, 32);
		}

		private void SwatchRow(int min, int max)
		{
			ImGui.Spacing();
			for (var i = min; i < max; i++)
			{
				if (ImGui.ColorButton("###PlayerTrack_DefaultColor_Swatch" + i, _colorPalette[i]))
				{
					_playerTrackPlugin.Configuration.DefaultColor = _colorPalette[i];
					_playerTrackPlugin.SaveConfig();
				}

				ImGui.SameLine();
			}
		}

		private void SyncToLodestone()
		{
			var syncToLodestone = _playerTrackPlugin.Configuration.SyncToLodestone;
			if (ImGui.Checkbox(Loc.Localize("SyncToLodestone", "Sync to Lodestone"),
				ref syncToLodestone))
			{
				_playerTrackPlugin.Configuration.SyncToLodestone = syncToLodestone;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("SyncToLodestone_HelpMarker",
				"pull player data from lodestone to track name/world changes"));
			ImGui.Spacing();
		}

		private void LodestoneTimeout()
		{
			ImGui.Text(Loc.Localize("LodestoneTimeout", "Request Timeout (minutes)"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneTimeout_HelpMarker",
				"timeout for lodestone requests"));
			var lodestoneTimeout = _playerTrackPlugin.Configuration.LodestoneTimeout.FromMillisecondsToSeconds();
			if (ImGui.SliderInt("###PlayerTrack_LodestoneTimeout_Slider", ref lodestoneTimeout, 10, 300))
			{
				_playerTrackPlugin.Configuration.LodestoneTimeout = lodestoneTimeout.FromSecondsToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void LodestoneRequestDelay()
		{
			ImGui.Text(Loc.Localize("LodestoneRequestDelay", "Request Delay (seconds)"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneRequestDelay_HelpMarker",
				"delay between lodestone requests to avoid rate limits"));
			var lodestoneRequestDelay =
				_playerTrackPlugin.Configuration.LodestoneRequestDelay.FromMillisecondsToSeconds();
			if (ImGui.SliderInt("###PlayerTrack_LodestoneRequestDelay_Slider", ref lodestoneRequestDelay, 10, 300))
			{
				_playerTrackPlugin.Configuration.LodestoneRequestDelay =
					lodestoneRequestDelay.FromSecondsToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void LodestoneUpdateFrequency()
		{
			ImGui.Text(Loc.Localize("LodestoneUpdateFrequency", "Update Frequency (days)"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneUpdateFrequency_HelpMarker",
				"frequency to retrieve latest data from lodestone for a given player"));
			var updateFrequency = _playerTrackPlugin.Configuration.LodestoneUpdateFrequency.FromMillisecondsToDays();
			if (ImGui.SliderInt("###PlayerTrack_LodestoneUpdateFrequency_Slider", ref updateFrequency, 1, 7))
			{
				_playerTrackPlugin.Configuration.LodestoneUpdateFrequency = updateFrequency.FromDaysToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void LodestoneCooldownDuration()
		{
			ImGui.Text(Loc.Localize("LodestoneCooldownDuration", "Cooldown Duration (hours)"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneCooldownDuration_HelpMarker",
				"duration to delay if lodestone is unavailable before trying again"));
			var cooldownDuration = _playerTrackPlugin.Configuration.LodestoneCooldownDuration.FromMillisecondsToHours();
			if (ImGui.SliderInt("###PlayerTrack_LodestoneCooldownDuration_Slider", ref cooldownDuration, 1, 12))
			{
				_playerTrackPlugin.Configuration.LodestoneCooldownDuration = cooldownDuration.FromHoursToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void LodestoneLocale()
		{
			ImGui.Text(Loc.Localize("LodestoneLocale", "Lodestone Locale"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneLocale_HelpMarker",
				"set locale for lodestone profile link"));
			ImGui.Spacing();
			var lodestoneLocale = (int) _playerTrackPlugin.Configuration.LodestoneLocale;
			if (ImGui.Combo("###PlayerTrack_LodestoneLocale_Combo", ref lodestoneLocale,
				Enum.GetNames(typeof(TrackLodestoneLocale)),
				Enum.GetNames(typeof(TrackLodestoneLocale)).Length))
			{
				_playerTrackPlugin.Configuration.LodestoneLocale = (TrackLodestoneLocale) lodestoneLocale;
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void LodestoneMaxRetry()
		{
			ImGui.Text(Loc.Localize("LodestoneMaxRetry", "Max Retries (count)"));
			CustomWidgets.HelpMarker(Loc.Localize("LodestoneMaxRetry_HelpMarker",
				"number of attempts to retry failed lodestone request"));
			var backupRetention = _playerTrackPlugin.Configuration.LodestoneMaxRetry;
			if (ImGui.SliderInt("###PlayerTrack_LodestoneMaxRetry_Slider", ref backupRetention, 3, 10))
			{
				_playerTrackPlugin.Configuration.LodestoneMaxRetry = backupRetention;
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void Compressed()
		{
			var compressed = _playerTrackPlugin.Configuration.Compressed;
			if (ImGui.Checkbox(
				Loc.Localize("Compressed", "Compress Data") + "###PlayerTrack_Compressed_Checkbox",
				ref compressed))
			{
				_playerTrackPlugin.Configuration.Compressed = compressed;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("Compressed_HelpMarker",
				"compress saved data for significantly smaller file sizes (recommended to keep on)"));
			ImGui.Spacing();
		}

		private void SaveFrequency()
		{
			ImGui.Text(Loc.Localize("SaveFrequency", "Save Frequency (minutes)"));
			CustomWidgets.HelpMarker(Loc.Localize("SaveFrequency_HelpMarker",
				"frequency to save current player data"));
			var saveFrequency = _playerTrackPlugin.Configuration.SaveFrequency.FromMillisecondsToMinutes();
			if (ImGui.SliderInt("###PlayerTrack_SaveFrequency_Slider", ref saveFrequency, 1, 10))
			{
				_playerTrackPlugin.Configuration.SaveFrequency = saveFrequency.FromMinutesToMilliseconds();
				_playerTrackPlugin.SaveConfig();
				_playerTrackPlugin.RestartTimers();
			}

			ImGui.Spacing();
		}

		private void UpdateFrequency()
		{
			ImGui.Text(Loc.Localize("UpdateFrequency", "Update Frequency (seconds)"));
			CustomWidgets.HelpMarker(Loc.Localize("UpdateFrequency_HelpMarker",
				"frequency to process and update latest player data"));
			var updateFrequency = _playerTrackPlugin.Configuration.UpdateFrequency.FromMillisecondsToSeconds();
			if (ImGui.SliderInt("###PlayerTrack_UpdateFrequency_Slider", ref updateFrequency, 1, 60))
			{
				_playerTrackPlugin.Configuration.UpdateFrequency = updateFrequency.FromSecondsToMilliseconds();
				_playerTrackPlugin.SaveConfig();
				_playerTrackPlugin.RestartTimers();
			}

			ImGui.Spacing();
		}

		private void BackupFrequency()
		{
			ImGui.Text(Loc.Localize("BackupFrequency", "Backup Frequency (hours)"));
			CustomWidgets.HelpMarker(Loc.Localize("BackupFrequency_HelpMarker",
				"frequency to backup player data in case something goes wrong"));
			var backupFrequency = _playerTrackPlugin.Configuration.BackupFrequency.FromMillisecondsToHours();
			if (ImGui.SliderInt("###PlayerTrack_BackupFrequency_Slider", ref backupFrequency, 1, 24))
			{
				_playerTrackPlugin.Configuration.BackupFrequency = backupFrequency.FromHoursToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void BackupRetention()
		{
			ImGui.Text(Loc.Localize("BackupRetention", "Backup Retention (count)"));
			CustomWidgets.HelpMarker(Loc.Localize("PlayerTrack_BackupRetention_HelpMarker",
				"number of backups to keep before deleting the oldest"));
			var backupRetention = _playerTrackPlugin.Configuration.BackupRetention;
			if (ImGui.SliderInt("###PlayerTrack_BackupRetention_Slider", ref backupRetention, 3, 20))
			{
				_playerTrackPlugin.Configuration.BackupRetention = backupRetention;
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void NewEncounterThreshold()
		{
			ImGui.Text(Loc.Localize("NewEncounterThreshold", "New Encounter Threshold (hours)"));
			CustomWidgets.HelpMarker(Loc.Localize("NewEncounterThreshold_HelpMarker",
				"threshold for creating new encounter for player in same location"));
			var newEncounterThreshold =
				_playerTrackPlugin.Configuration.NewEncounterThreshold.FromMillisecondsToHours();
			if (ImGui.SliderInt("###PlayerTrack_NewEncounterThreshold_Slider", ref newEncounterThreshold, 1, 24))
			{
				_playerTrackPlugin.Configuration.NewEncounterThreshold =
					newEncounterThreshold.FromHoursToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void RestrictInCombat()
		{
			var restrictInCombat = _playerTrackPlugin.Configuration.RestrictInCombat;
			if (ImGui.Checkbox(
				Loc.Localize("RestrictInCombat", "Don't process in combat") +
				"###PlayerTrack_RestrictInCombat_Checkbox",
				ref restrictInCombat))
			{
				_playerTrackPlugin.Configuration.RestrictInCombat = restrictInCombat;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("RestrictInCombat_HelpMarker",
				"stop processing data while in combat"));
			ImGui.Spacing();
		}

		private void RestrictToContent()
		{
			var restrictToContent = _playerTrackPlugin.Configuration.RestrictToContent;
			if (ImGui.Checkbox(
				Loc.Localize("RestrictToContent", "Content Only") + "###PlayerTrack_RestrictToContent_Checkbox",
				ref restrictToContent))
			{
				_playerTrackPlugin.Configuration.RestrictToContent = restrictToContent;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("RestrictToContent_HelpMarker",
				"restrict to instanced content and exclude overworld encounters"));
			ImGui.Spacing();
		}

		private void RestrictToHighEndDuty()
		{
			var restrictToHighEndDuty = _playerTrackPlugin.Configuration.RestrictToHighEndDuty;
			if (ImGui.Checkbox(
				Loc.Localize("RestrictToHighEndDuty", "High-End Duty Only") +
				"###PlayerTrack_RestrictToHighEndDuty_Checkbox",
				ref restrictToHighEndDuty))
			{
				_playerTrackPlugin.Configuration.RestrictToHighEndDuty = restrictToHighEndDuty;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("RestrictToHighEndDuty_HelpMarker",
				"restrict to high-end duties only (e.g. savage)"));
			ImGui.Spacing();
		}

		private void RestrictToCustom()
		{
			var restrictToCustomList = _playerTrackPlugin.Configuration.RestrictToCustom;
			if (ImGui.Checkbox(
				Loc.Localize("RestrictToCustom", "Restrict to Following Content") +
				"###PlayerTrack_RestrictToCustom_Checkbox",
				ref restrictToCustomList))
			{
				_playerTrackPlugin.Configuration.RestrictToCustom = restrictToCustomList;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("RestrictToCustom_HelpMarker",
				"add content to list by using dropdown or remove by clicking on them"));
			ImGui.Spacing();

			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * Scale);
			ImGui.Combo("###PlayerTrack_Content_Combo", ref _selectedContentIndex,
				_playerTrackPlugin.GetContentNames(),
				_playerTrackPlugin.GetContentIds().Length);
			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("Add", "Add") + "###PlayerTrack_ContentAdd_Button"))
			{
				if (_playerTrackPlugin.Configuration.PermittedContent.Contains(
					_playerTrackPlugin.GetContentIds()[_selectedContentIndex]))
				{
					ImGui.OpenPopup("###PlayerTrack_DupeContent_Popup");
				}
				else
				{
					_playerTrackPlugin.Configuration.PermittedContent.Add(
						_playerTrackPlugin.GetContentIds()[_selectedContentIndex]);
					_playerTrackPlugin.SaveConfig();
				}
			}

			ImGui.SameLine();
			if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_ContentReset_Button"))
			{
				_selectedContentIndex = 0;
				_playerTrackPlugin.Configuration.PermittedContent = new List<uint>();
				_playerTrackPlugin.SaveConfig();
			}

			if (ImGui.BeginPopup("###PlayerTrack_DupeContent_Popup"))
			{
				ImGui.Text(Loc.Localize("DupeContent", "This content is already added!"));
				ImGui.EndPopup();
			}

			ImGui.Spacing();

			foreach (var permittedContent in Enumerable.ToList(_playerTrackPlugin.Configuration.PermittedContent))
			{
				var index = Array.IndexOf(_playerTrackPlugin.GetContentIds(), permittedContent);
				ImGui.Text(_playerTrackPlugin.GetContentNames()[index]);
				if (ImGui.IsItemClicked())
				{
					_playerTrackPlugin.Configuration.PermittedContent.Remove(permittedContent);
					_playerTrackPlugin.SaveConfig();
				}
			}

			ImGui.Spacing();
		}

		private void ShowIcons()
		{
			var showIcons = _playerTrackPlugin.Configuration.ShowIcons;
			if (ImGui.Checkbox(
				Loc.Localize("ShowIcons", "Show Icons") + "###PlayerTrack_ShowIcons_Checkbox",
				ref showIcons))
			{
				_playerTrackPlugin.Configuration.ShowIcons = showIcons;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("ShowIcons_HelpMarker",
				"turn icons on/off on overlay"));
			ImGui.Spacing();
		}

		private void EnableAlerts()
		{
			var enableAlerts = _playerTrackPlugin.Configuration.EnableAlerts;
			if (ImGui.Checkbox(
				Loc.Localize("EnableAlerts", "Enable Alerts") + "###PlayerTrack_EnableAlerts_Checkbox",
				ref enableAlerts))
			{
				_playerTrackPlugin.Configuration.EnableAlerts = enableAlerts;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("EnableAlerts_HelpMarker",
				"globally disable alerts even if set for player"));
			ImGui.Spacing();
		}

		private void EnableAlertsForAllPlayers()
		{
			var enableAlerts = _playerTrackPlugin.Configuration.EnableAlertsForAllPlayers;
			if (ImGui.Checkbox(
				Loc.Localize("EnableAlertsForAllPlayers", "Enable Alerts for All Players") +
				"###PlayerTrack_EnableAlertsForAllPlayers_Checkbox",
				ref enableAlerts))
			{
				_playerTrackPlugin.Configuration.EnableAlertsForAllPlayers = enableAlerts;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("EnableAlertsForAllPlayers_HelpMarker",
				"enable alerts for all players even if not enabled for a specific player"));
			ImGui.Spacing();
		}

		private void IncludeNotesInAlerts()
		{
			var includeNotesInAlert = _playerTrackPlugin.Configuration.IncludeNotesInAlert;
			if (ImGui.Checkbox(
				Loc.Localize("IncludeNotesInAlert", "Include Notes in Alerts") +
				"###PlayerTrack_IncludeNotesInAlert_Checkbox",
				ref includeNotesInAlert))
			{
				_playerTrackPlugin.Configuration.IncludeNotesInAlert = includeNotesInAlert;
				_playerTrackPlugin.SaveConfig();
			}

			CustomWidgets.HelpMarker(Loc.Localize("IncludeNotesInAlert_HelpMarker",
				"include notes in alert message if available"));
			ImGui.Spacing();
		}

		private void AlertFrequency()
		{
			ImGui.Text(Loc.Localize("AlertFrequency", "Alert Frequency (hours)"));
			CustomWidgets.HelpMarker(Loc.Localize("AlertFrequency_HelpMarker",
				"frequency to send player alert"));
			var alertFrequency = _playerTrackPlugin.Configuration.AlertFrequency.FromMillisecondsToHours();
			if (ImGui.SliderInt("###PlayerTrack_AlertFrequency_Slider", ref alertFrequency, 1, 24))
			{
				_playerTrackPlugin.Configuration.AlertFrequency = alertFrequency.FromHoursToMilliseconds();
				_playerTrackPlugin.SaveConfig();
			}

			ImGui.Spacing();
		}

		private void DefaultIcon()
		{
			_selectedDefaultIconIndex =
				Array.IndexOf(_iconNames, _playerTrackPlugin.Configuration.DefaultIcon.ToString());
			ImGui.Text(Loc.Localize("DefaultIcon", "Default Icon"));
			CustomWidgets.HelpMarker(Loc.Localize("DefaultIcon_HelpMarker",
				"default icon used when one is not set"));
			ImGui.Spacing();
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * Scale);
			if (ImGui.Combo("###PlayerTrack_DefaultIcon_Combo", ref _selectedDefaultIconIndex,
				_iconNames,
				_icons.Length))
			{
				_playerTrackPlugin.Configuration.DefaultIcon = _icons[_selectedDefaultIconIndex];
				_playerTrackPlugin.SaveConfig();
			}

			;
			ImGui.SameLine();
			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.Text(_icons[_selectedDefaultIconIndex].ToIconString());
			ImGui.PopFont();
			ImGui.Spacing();
		}

		private void IconList()
		{
			ImGui.Text(Loc.Localize("Icons", "Add / Remove Icons"));
			CustomWidgets.HelpMarker(Loc.Localize("AddRemoveIcons",
				"add new icons using dropdown or remove icons by clicking on them"));
			ImGui.Spacing();
			ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * Scale);
			ImGui.Combo("###PlayerTrack_Icon_Combo", ref _selectedIconIndex,
				_iconNames,
				Enumerable.Count(_icons));
			ImGui.SameLine();

			ImGui.PushFont(UiBuilder.IconFont);
			ImGui.Text(_icons[_selectedIconIndex].ToIconString());
			ImGui.PopFont();
			ImGui.SameLine();

			if (ImGui.SmallButton(Loc.Localize("Add", "Add") + "###PlayerTrack_IconAdd_Button"))
			{
				if (_playerTrackPlugin.Configuration.EnabledIcons.Contains(_icons[_selectedIconIndex]))
				{
					ImGui.OpenPopup("###PlayerTrack_DupeIcon_Popup");
				}
				else
				{
					_playerTrackPlugin.Configuration.EnabledIcons.Add(_icons[_selectedIconIndex]);
					_playerTrackPlugin.SaveConfig();
				}
			}

			ImGui.SameLine();
			if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_IconReset_Button"))
			{
				_selectedIconIndex = 4;
				_playerTrackPlugin.Configuration.ShowIcons = true;
				_playerTrackPlugin.Configuration.DefaultIcon = FontAwesomeIcon.User;
				_playerTrackPlugin.SetDefaultIcons();
				_playerTrackPlugin.SaveConfig();
			}

			if (ImGui.BeginPopup("###PlayerTrack_DupeIcon_Popup"))
			{
				ImGui.Text(Loc.Localize("DupeIcon", "This icon is already added!"));
				ImGui.EndPopup();
			}

			ImGui.Spacing();

			foreach (var enabledIcon in Enumerable.ToList(_playerTrackPlugin.Configuration.EnabledIcons))
			{
				ImGui.BeginGroup();
				ImGui.PushFont(UiBuilder.IconFont);
				ImGui.Text(enabledIcon.ToIconString());
				ImGui.PopFont();
				ImGui.SameLine();
				ImGui.Text(enabledIcon.ToString());
				ImGui.EndGroup();
				if (ImGui.IsItemClicked())
				{
					_playerTrackPlugin.Configuration.EnabledIcons.Remove(enabledIcon);
					_playerTrackPlugin.SaveConfig();
				}
			}

			ImGui.Spacing();
		}

		public void DrawLinks()
		{
			var buttonSize = new Vector2(120f * Scale, 25f * Scale);
			if (ImGui.Button(Loc.Localize("OpenGithub", "Github") + "###PlayerTrack_OpenGithub_Button", buttonSize))
				Process.Start("https://github.com/kalilistic/PlayerTrack");
			if (ImGui.Button(Loc.Localize("PrintHelp", "Instructions") + "###PlayerTrack_PrintHelp_Button", buttonSize))
				_playerTrackPlugin.PrintHelpMessage();
		}

		private enum Tab
		{
			General,
			Display,
			Threshold,
			Filters,
			Icons,
			Lodestone,
			Data,
			Alerts,
			Backup,
			Links
		}
	}
}