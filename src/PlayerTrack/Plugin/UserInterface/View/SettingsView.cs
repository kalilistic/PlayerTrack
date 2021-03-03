// ReSharper disable InconsistentNaming
// ReSharper disable InvertIf
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
    public class SettingsView : WindowBase
    {
        private readonly List<Vector4> _colorPalette = ImGuiUtil.CreatePalette();
        private Modal _currentModal = Modal.None;
        private Tab _currentTab = Tab.General;
        private int _selectedCategoryIndex;
        private int _selectedContentIndex;
        private int _selectedIconIndex = 4;
        public List<TrackCategory> Categories;
        public PlayerTrackConfig Configuration;
        public uint[] ContentIds;
        public string[] ContentNames;
        public string[] IconNames;
        public FontAwesomeIcon[] Icons;
        public bool IsCategoryDataUpdated = true;
        public bool IsInitialized;
        public List<TrackCategory> NextCategories;
        public event EventHandler<bool> ConfigUpdated;
        public event EventHandler<int> LanguageUpdated;
        public event EventHandler<int> RequestCategoryDelete;
        public event EventHandler<bool> RequestCategoryReset;
        public event EventHandler<bool> RequestCategoryAdd;
        public event EventHandler<TrackCategory> RequestCategoryUpdate;
        public event EventHandler<int> RequestCategoryMoveUp;
        public event EventHandler<int> RequestCategoryMoveDown;
        public event EventHandler<bool> RequestResetIcons;
        public event EventHandler<bool> RequestPrintHelp;
        public event EventHandler<bool> RequestToggleOverlay;

        public override void DrawView()
        {
            if (!IsInitialized) return;
            if (!IsVisible) return;
            var isVisible = IsVisible;
            if (IsCategoryDataUpdated)
            {
                Categories = NextCategories;
                IsCategoryDataUpdated = false;
            }

            ImGui.SetNextWindowSize(new Vector2(640 * Scale, 320 * Scale), ImGuiCond.Appearing);
            if (ImGui.Begin(Loc.Localize("SettingsView", "PlayerTrack Settings") + "###PlayerTrack_Settings_View",
                ref isVisible, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar))
                IsVisible = isVisible;

            DrawTabs();
            OpenCurrentTab();
            OpenModals();
            ImGui.End();
        }

        private void OpenModals()
        {
            switch (_currentModal)
            {
                case Modal.None:
                    break;
                case Modal.Delete:
                    DeleteModal();
                    break;
                case Modal.Reset:
                    ResetModal();
                    break;
            }
        }

        private void DeleteModal()
        {
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
                ImGuiCond.Appearing);
            ImGui.Begin(Loc.Localize("DeleteModalTitle", "Delete Confirmation") + "###PlayerTrack_DeleteModal_Window",
                ImGuiUtil.ModalWindowFlags());
            ImGui.Text(Loc.Localize("DeleteModalContent", "Are you sure you want to delete?"));
            ImGui.Spacing();
            if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_DeleteModalOK_Button"))
            {
                _currentModal = Modal.None;
                RequestCategoryDelete?.Invoke(this, Categories[_selectedCategoryIndex].Id);
                _selectedCategoryIndex = 0;
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("Cancel", "Cancel") + "###PlayerTrack_DeleteModalCancel_Button"))
            {
                _currentModal = Modal.None;
                _selectedCategoryIndex = 0;
            }

            ImGui.End();
        }

        private void ResetModal()
        {
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetIO().DisplaySize.X * 0.5f, ImGui.GetIO().DisplaySize.Y * 0.5f),
                ImGuiCond.Appearing);
            ImGui.Begin(Loc.Localize("ResetModalTitle", "Reset Confirmation") + "###PlayerTrack_ResetModal_Window",
                ImGuiUtil.ModalWindowFlags());
            ImGui.Text(Loc.Localize("ResetModalContent", "Are you sure you want to reset?"));
            ImGui.Spacing();
            if (ImGui.Button(Loc.Localize("OK", "OK") + "###PlayerTrack_ResetModalOK_Button"))
            {
                RequestCategoryReset?.Invoke(this, true);
                _currentModal = Modal.None;
            }

            ImGui.SameLine();
            if (ImGui.Button(Loc.Localize("Cancel", "Cancel") + "###PlayerTrack_ResetModalCancel_Button"))
                _currentModal = Modal.None;

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

                if (ImGui.BeginTabItem(Loc.Localize("Alerts", "Alerts") + "###PlayerTrack_Alerts_Tab"))
                {
                    _currentTab = Tab.Alerts;
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("Categories", "Categories") + "###PlayerTrack_Categories_Tab"))
                {
                    _currentTab = Tab.Categories;
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.Localize("Targets", "Targets") + "###PlayerTrack_Targets_Tab"))
                {
                    _currentTab = Tab.Targets;
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
                case Tab.Alerts:
                {
                    DrawAlerts();
                    break;
                }
                case Tab.Categories:
                {
                    DrawCategories();
                    break;
                }
                case Tab.Targets:
                {
                    DrawTargets();
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
            Compressed();
            SetLanguage();
        }

        private void DrawDisplay()
        {
            ShowOverlay();
            LockOverlay();
            ShowPlayerCharacterDetails();
            ShowPlayerOverride();
            DefaultView();
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
            IconList();
        }

        private void DrawLodestone()
        {
            SyncToLodestone();
            LodestoneLocale();
        }

        private void DrawAlerts()
        {
            EnableAlerts();
            IncludeNotesInAlerts();
            AlertFrequency();
        }

        private void DrawCategories()
        {
            CategoryControls();
            CategoryTable();
        }

        private void DrawTargets()
        {
            EnableCurrentTarget();
            EnableFocusTarget();
        }

        private void DrawBackup()
        {
            BackupFrequency();
            BackupRetention();
        }

        private void PluginEnabled()
        {
            var enabled = Configuration.Enabled;
            if (ImGui.Checkbox(
                Loc.Localize("PluginEnabled", "Plugin Enabled") + "###PlayerTrack_PluginEnabled_Checkbox",
                ref enabled))
            {
                Configuration.Enabled = enabled;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("PluginEnabled_HelpMarker",
                "toggle the plugin on/off"));
            ImGui.Spacing();
        }

        private void ShowOverlay()
        {
            var showOverlay = Configuration.ShowOverlay;
            if (ImGui.Checkbox(Loc.Localize("ShowOverlay", "Show Overlay") + "###PlayerTrack_ShowOverlay_Checkbox",
                ref showOverlay))
                RequestToggleOverlay?.Invoke(this, true);

            CustomWidgets.HelpMarker(Loc.Localize("ShowOverlay_HelpMarker",
                "show overlay window"));
            ImGui.Spacing();
        }

        private void LockOverlay()
        {
            var lockOverlay = Configuration.LockOverlay;
            if (ImGui.Checkbox(
                Loc.Localize("LockOverlay", "Lock Overlay") + "###PlayerTrack_LockOverlay_Checkbox",
                ref lockOverlay))
            {
                Configuration.LockOverlay = lockOverlay;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("LockOverlay_HelpMarker",
                "keep the overlay windows in-place"));
            ImGui.Spacing();
        }

        private void DefaultView()
        {
            ImGui.Text(Loc.Localize("DefaultView", "Default View"));
            CustomWidgets.HelpMarker(Loc.Localize("DefaultView_HelpMarker",
                "set the default view to show on start up"));
            ImGui.Spacing();
            var defaultViewMode = Configuration.DefaultViewMode;
            if (ImGui.Combo("###PlayerTrack_DefaultView_Combo", ref defaultViewMode,
                TrackViewMode.ViewNames.ToArray(),
                TrackViewMode.ViewNames.Count))
            {
                Configuration.DefaultViewMode = defaultViewMode;
                ConfigUpdated?.Invoke(this, true);
            }
        }

        private void ShowPlayerCharacterDetails()
        {
            var showPlayerCharacterDetails = Configuration.ShowPlayerCharacterDetails;
            if (ImGui.Checkbox(
                Loc.Localize("ShowPlayerCharacterDetails", "Show Player Character Details") +
                "###PlayerTrack_ShowPlayerCharacterDetails_Checkbox",
                ref showPlayerCharacterDetails))
            {
                Configuration.ShowPlayerCharacterDetails = showPlayerCharacterDetails;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("ShowPlayerCharacterDetails_HelpMarker",
                "show details about character such as tribe and gender"));
            ImGui.Spacing();
        }

        private void ShowPlayerOverride()
        {
            var showPlayerOverride = Configuration.ShowPlayerOverride;
            if (ImGui.Checkbox(
                Loc.Localize("ShowPlayerOverride", "Show Player Display Options") +
                "###PlayerTrack_ShowPlayerOverride_Checkbox",
                ref showPlayerOverride))
            {
                Configuration.ShowPlayerOverride = showPlayerOverride;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("ShowPlayerOverride_HelpMarker",
                "show options to override display settings such as color and icon"));
            ImGui.Spacing();
        }

        private void SetLanguage()
        {
            ImGui.Text(Loc.Localize("Language", "Language"));
            CustomWidgets.HelpMarker(Loc.Localize("Language_HelpMarker",
                "use default or override plugin ui language"));
            ImGui.Spacing();
            var pluginLanguage = Configuration.PluginLanguage;
            if (ImGui.Combo("###PlayerTrack_Language_Combo", ref pluginLanguage,
                PluginLanguage.LanguageNames.ToArray(),
                PluginLanguage.LanguageNames.Count))
            {
                Configuration.PluginLanguage = pluginLanguage;
                ConfigUpdated?.Invoke(this, true);
                LanguageUpdated?.Invoke(this, pluginLanguage);
            }
        }

        private void RecentPlayerThreshold()
        {
            ImGui.Text(Loc.Localize("RecentPlayerThreshold", "Recent Player Threshold (minutes)"));
            CustomWidgets.HelpMarker(Loc.Localize("RecentPlayerThreshold_HelpMarker",
                "amount of time players will appear on recent players list since last seen date"));
            var RecentPlayerThreshold =
                Configuration.RecentPlayerThreshold.FromMillisecondsToMinutes();
            if (ImGui.SliderInt("###PlayerTrack_RecentPlayerThreshold_Slider", ref RecentPlayerThreshold, 1, 60))
            {
                Configuration.RecentPlayerThreshold =
                    RecentPlayerThreshold.FromMinutesToMilliseconds();
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void CategoryPalette(int id)
        {
            CategorySwatchRow(id, 0, 8);
            CategorySwatchRow(id, 8, 16);
            CategorySwatchRow(id, 16, 24);
            CategorySwatchRow(id, 24, 32);
        }

        private void CategorySwatchRow(int id, int min, int max)
        {
            ImGui.Spacing();
            for (var i = min; i < max; i++)
            {
                if (ImGui.ColorButton("###PlayerTrack_CategoryColor_Swatch_" + id + i, _colorPalette[i]))
                {
                    Categories[id].Color = _colorPalette[i];
                    RequestCategoryUpdate?.Invoke(this, Categories[id]);
                    ConfigUpdated?.Invoke(this, true);
                }

                ImGui.SameLine();
            }
        }

        private void SyncToLodestone()
        {
            var syncToLodestone = Configuration.SyncToLodestone;
            if (ImGui.Checkbox(Loc.Localize("SyncToLodestone", "Sync to Lodestone"),
                ref syncToLodestone))
            {
                Configuration.SyncToLodestone = syncToLodestone;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("SyncToLodestone_HelpMarker",
                "pull player data from lodestone to track name/world changes"));
            ImGui.Spacing();
        }

        private void LodestoneLocale()
        {
            ImGui.Text(Loc.Localize("LodestoneLocale", "Lodestone Locale"));
            CustomWidgets.HelpMarker(Loc.Localize("LodestoneLocale_HelpMarker",
                "set locale for lodestone profile link"));
            ImGui.Spacing();
            var lodestoneLocale = (int) Configuration.LodestoneLocale;
            if (ImGui.Combo("###PlayerTrack_LodestoneLocale_Combo", ref lodestoneLocale,
                Enum.GetNames(typeof(TrackLodestoneLocale)),
                Enum.GetNames(typeof(TrackLodestoneLocale)).Length))
            {
                Configuration.LodestoneLocale = (TrackLodestoneLocale) lodestoneLocale;
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void Compressed()
        {
            var compressed = Configuration.Compressed;
            if (ImGui.Checkbox(
                Loc.Localize("Compressed", "Compress Data") + "###PlayerTrack_Compressed_Checkbox",
                ref compressed))
            {
                Configuration.Compressed = compressed;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("Compressed_HelpMarker",
                "compress saved data for significantly smaller file sizes (recommended to keep on)"));
            ImGui.Spacing();
        }

        private void BackupFrequency()
        {
            ImGui.Text(Loc.Localize("BackupFrequency", "Backup Frequency (hours)"));
            CustomWidgets.HelpMarker(Loc.Localize("BackupFrequency_HelpMarker",
                "frequency to backup player data in case something goes wrong"));
            var backupFrequency = Configuration.BackupFrequency.FromMillisecondsToHours();
            if (ImGui.SliderInt("###PlayerTrack_BackupFrequency_Slider", ref backupFrequency, 1, 24))
            {
                Configuration.BackupFrequency = backupFrequency.FromHoursToMilliseconds();
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void BackupRetention()
        {
            ImGui.Text(Loc.Localize("BackupRetention", "Backup Retention (count)"));
            CustomWidgets.HelpMarker(Loc.Localize("PlayerTrack_BackupRetention_HelpMarker",
                "number of backups to keep before deleting the oldest"));
            var backupRetention = Configuration.BackupRetention;
            if (ImGui.SliderInt("###PlayerTrack_BackupRetention_Slider", ref backupRetention, 3, 20))
            {
                Configuration.BackupRetention = backupRetention;
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void NewEncounterThreshold()
        {
            ImGui.Text(Loc.Localize("NewEncounterThreshold", "New Encounter Threshold (hours)"));
            CustomWidgets.HelpMarker(Loc.Localize("NewEncounterThreshold_HelpMarker",
                "threshold for creating new encounter for player in same location"));
            var newEncounterThreshold =
                Configuration.NewEncounterThreshold.FromMillisecondsToHours();
            if (ImGui.SliderInt("###PlayerTrack_NewEncounterThreshold_Slider", ref newEncounterThreshold, 1, 24))
            {
                Configuration.NewEncounterThreshold =
                    newEncounterThreshold.FromHoursToMilliseconds();
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void RestrictInCombat()
        {
            var restrictInCombat = Configuration.RestrictInCombat;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictInCombat", "Don't process in combat") +
                "###PlayerTrack_RestrictInCombat_Checkbox",
                ref restrictInCombat))
            {
                Configuration.RestrictInCombat = restrictInCombat;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("RestrictInCombat_HelpMarker",
                "stop processing data while in combat"));
            ImGui.Spacing();
        }

        private void RestrictToContent()
        {
            var restrictToContent = Configuration.RestrictToContent;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictToContent", "Content Only") + "###PlayerTrack_RestrictToContent_Checkbox",
                ref restrictToContent))
            {
                Configuration.RestrictToContent = restrictToContent;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("RestrictToContent_HelpMarker",
                "restrict to instanced content and exclude overworld encounters"));
            ImGui.Spacing();
        }

        private void RestrictToHighEndDuty()
        {
            var restrictToHighEndDuty = Configuration.RestrictToHighEndDuty;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictToHighEndDuty", "High-End Duty Only") +
                "###PlayerTrack_RestrictToHighEndDuty_Checkbox",
                ref restrictToHighEndDuty))
            {
                Configuration.RestrictToHighEndDuty = restrictToHighEndDuty;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("RestrictToHighEndDuty_HelpMarker",
                "restrict to high-end duties only (e.g. savage)"));
            ImGui.Spacing();
        }

        private void RestrictToCustom()
        {
            var restrictToCustomList = Configuration.RestrictToCustom;
            if (ImGui.Checkbox(
                Loc.Localize("RestrictToCustom", "Restrict to Following Content") +
                "###PlayerTrack_RestrictToCustom_Checkbox",
                ref restrictToCustomList))
            {
                Configuration.RestrictToCustom = restrictToCustomList;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("RestrictToCustom_HelpMarker",
                "add content to list by using dropdown or remove by clicking on them"));
            ImGui.Spacing();

            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * Scale);
            ImGui.Combo("###PlayerTrack_Content_Combo", ref _selectedContentIndex,
                ContentNames, ContentIds.Length);
            ImGui.SameLine();

            if (ImGui.SmallButton(Loc.Localize("Add", "Add") + "###PlayerTrack_ContentAdd_Button"))
            {
                if (Configuration.PermittedContent.Contains(
                    ContentIds[_selectedContentIndex]))
                {
                    ImGui.OpenPopup("###PlayerTrack_DupeContent_Popup");
                }
                else
                {
                    Configuration.PermittedContent.Add(
                        ContentIds[_selectedContentIndex]);
                    ConfigUpdated?.Invoke(this, true);
                }
            }

            ImGui.SameLine();
            if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_ContentReset_Button"))
            {
                _selectedContentIndex = 0;
                Configuration.PermittedContent = new List<uint>();
                ConfigUpdated?.Invoke(this, true);
            }

            if (ImGui.BeginPopup("###PlayerTrack_DupeContent_Popup"))
            {
                ImGui.Text(Loc.Localize("DupeContent", "This content is already added!"));
                ImGui.EndPopup();
            }

            ImGui.Spacing();

            foreach (var permittedContent in Configuration.PermittedContent.ToList())
            {
                var index = Array.IndexOf(ContentIds, permittedContent);
                ImGui.Text(ContentNames[index]);
                if (ImGui.IsItemClicked())
                {
                    Configuration.PermittedContent.Remove(permittedContent);
                    ConfigUpdated?.Invoke(this, true);
                }
            }

            ImGui.Spacing();
        }

        private void EnableAlerts()
        {
            var enableAlerts = Configuration.EnableAlerts;
            if (ImGui.Checkbox(
                Loc.Localize("EnableAlerts", "Enable Alerts") + "###PlayerTrack_EnableAlerts_Checkbox",
                ref enableAlerts))
            {
                Configuration.EnableAlerts = enableAlerts;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("EnableAlerts_HelpMarker",
                "globally disable alerts even if set for player"));
            ImGui.Spacing();
        }

        private void IncludeNotesInAlerts()
        {
            var includeNotesInAlert = Configuration.IncludeNotesInAlert;
            if (ImGui.Checkbox(
                Loc.Localize("IncludeNotesInAlert", "Include Notes in Alerts") +
                "###PlayerTrack_IncludeNotesInAlert_Checkbox",
                ref includeNotesInAlert))
            {
                Configuration.IncludeNotesInAlert = includeNotesInAlert;
                ConfigUpdated?.Invoke(this, true);
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
            var alertFrequency = Configuration.AlertFrequency.FromMillisecondsToHours();
            if (ImGui.SliderInt("###PlayerTrack_AlertFrequency_Slider", ref alertFrequency, 1, 24))
            {
                Configuration.AlertFrequency = alertFrequency.FromHoursToMilliseconds();
                ConfigUpdated?.Invoke(this, true);
            }

            ImGui.Spacing();
        }

        private void CategoryControls()
        {
            if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_CategoryReset_Button"))
                _currentModal = Modal.Reset;
            ImGui.SameLine();
            if (ImGui.SmallButton(Loc.Localize("Add", "Add") + "###PlayerTrack_CategoryAdd_Button"))
                RequestCategoryAdd?.Invoke(this, true);

            ImGui.Separator();
        }

        private void CategoryTable()
        {
            if (Categories == null) return;

            // define table
            ImGui.Columns(5, "###PlayerTrack_CategoryTable_Columns", true);
            ImGui.SetColumnWidth(0, ImGui.GetWindowSize().X / 4 * Scale + 20f);
            ImGui.SetColumnWidth(1, 50f * Scale);
            ImGui.SetColumnWidth(2, 50f * Scale);
            ImGui.SetColumnWidth(3, ImGui.GetWindowSize().X / 4 * Scale + 40f);
            ImGui.SetColumnWidth(4, ImGui.GetWindowSize().X / 4 * Scale + 40f);

            // column headings
            ImGui.Text(Loc.Localize("CategoryName", "Name"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryColor", "Color"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryAlerts", "Alerts"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryIcon", "Icon"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryAction", "Actions"));
            ImGui.NextColumn();
            ImGui.Separator();

            // current categories
            for (var i = 0; i < Categories.Count; i++)
            {
                var categoryName = Categories[i].Name;
                ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 4 * Scale);
                if (Categories[i].IsDefault)
                {
                    ImGui.InputText("###PlayerTrack_CategoryName_Input" + i, ref categoryName, 20,
                        ImGuiInputTextFlags.ReadOnly);
                }
                else
                {
                    if (ImGui.InputText("###PlayerTrack_CategoryName_Input" + i, ref categoryName, 20))
                    {
                        Categories[i].Name = categoryName;
                        RequestCategoryUpdate?.Invoke(this, Categories[i]);
                    }
                }

                ImGui.NextColumn();

                var categoryColor = Categories[i].Color;
                if (ImGui.ColorButton("###PlayerTrack_CategoryColor_Button" + i, categoryColor)
                )
                    ImGui.OpenPopup("###PlayerTrack_CategoryColor_Popup" + i);
                if (ImGui.BeginPopup("###PlayerTrack_CategoryColor_Popup" + i))
                {
                    if (ImGui.ColorPicker4("###PlayerTrack_CategoryColor_ColorPicker" + i, ref categoryColor))
                    {
                        Categories[i].Color = categoryColor;
                        RequestCategoryUpdate?.Invoke(this, Categories[i]);
                    }

                    CategoryPalette(i);
                    ImGui.EndPopup();
                }

                ImGui.NextColumn();

                var enableAlerts = Categories[i].EnableAlerts;
                if (ImGui.Checkbox("###PlayerTrack_EnableCategoryAlerts_Checkbox" + i,
                    ref enableAlerts))
                {
                    Categories[i].EnableAlerts = enableAlerts;
                    RequestCategoryUpdate?.Invoke(this, Categories[i]);
                }

                ImGui.NextColumn();

                var categoryIcon = Categories[i].Icon;
                var namesList = new List<string> {Loc.Localize("Default", "Default")};
                namesList.AddRange(Configuration.EnabledIcons.ToList()
                    .Select(icon => icon.ToString()));
                var names = namesList.ToArray();
                var codesList = new List<int> {0};
                codesList.AddRange(Configuration.EnabledIcons.ToList().Select(icon => (int) icon));
                var codes = codesList.ToArray();
                var iconIndex = Array.IndexOf(codes, categoryIcon);
                ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 4 * Scale);
                if (ImGui.Combo("###PlayerTrack_SelectCategoryIcon_Combo" + i, ref iconIndex,
                    names,
                    names.Length))
                {
                    Categories[i].Icon = codes[iconIndex];
                    RequestCategoryUpdate?.Invoke(this, Categories[i]);
                }

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(categoryIcon != 0
                    ? ((FontAwesomeIcon) categoryIcon).ToIconString()
                    : FontAwesomeIcon.User.ToIconString());
                ImGui.PopFont();
                ImGui.NextColumn();

                if (i != 0)
                {
                    if (CustomWidgets.IconButton(FontAwesomeIcon.ArrowUp, "###PlayerTrack_MoveUpCategory_Button" + i))
                        RequestCategoryMoveUp?.Invoke(this, Categories[i].Id);

                    ImGui.SameLine();
                }

                if (i != Categories.Count - 1)
                {
                    if (CustomWidgets.IconButton(FontAwesomeIcon.ArrowDown,
                        "###PlayerTrack_MoveDownCategory_Button" + i))
                        RequestCategoryMoveDown?.Invoke(this, Categories[i].Id);

                    ImGui.SameLine();
                }

                if (!Categories[i].IsDefault)
                    if (CustomWidgets.IconButton(FontAwesomeIcon.Trash, "###PlayerTrack_DeleteCategory_Button" + i))
                    {
                        _selectedCategoryIndex = i;
                        _currentModal = Modal.Delete;
                    }

                ImGui.NextColumn();
            }
        }

        private void IconList()
        {
            ImGui.Text(Loc.Localize("Icons", "Add / Remove Icons"));
            CustomWidgets.HelpMarker(Loc.Localize("AddRemoveIcons",
                "add new icons using dropdown or remove icons by clicking on them"));
            ImGui.Spacing();
            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2 * Scale);
            ImGui.Combo("###PlayerTrack_Icon_Combo", ref _selectedIconIndex,
                IconNames,
                Icons.Count());
            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(Icons[_selectedIconIndex].ToIconString());
            ImGui.PopFont();
            ImGui.SameLine();

            if (ImGui.SmallButton(Loc.Localize("Add", "Add") + "###PlayerTrack_IconAdd_Button"))
            {
                if (Configuration.EnabledIcons.Contains(Icons[_selectedIconIndex]))
                {
                    ImGui.OpenPopup("###PlayerTrack_DupeIcon_Popup");
                }
                else
                {
                    Configuration.EnabledIcons.Add(Icons[_selectedIconIndex]);
                    ConfigUpdated?.Invoke(this, true);
                }
            }

            ImGui.SameLine();
            if (ImGui.SmallButton(Loc.Localize("Reset", "Reset") + "###PlayerTrack_IconReset_Button"))
            {
                _selectedIconIndex = 4;
                RequestResetIcons?.Invoke(this, true);
                ConfigUpdated?.Invoke(this, true);
            }

            if (ImGui.BeginPopup("###PlayerTrack_DupeIcon_Popup"))
            {
                ImGui.Text(Loc.Localize("DupeIcon", "This icon is already added!"));
                ImGui.EndPopup();
            }

            ImGui.Spacing();

            foreach (var enabledIcon in Configuration.EnabledIcons.ToList())
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
                    Configuration.EnabledIcons.Remove(enabledIcon);
                    ConfigUpdated?.Invoke(this, true);
                }
            }

            ImGui.Spacing();
        }

        private void EnableCurrentTarget()
        {
            var enableCurrentTarget = Configuration.SetCurrentTargetOnRightClick;
            if (ImGui.Checkbox(
                Loc.Localize("EnableCurrentTarget", "Set Current Target on Right Click") +
                "###PlayerTrack_EnableCurrentTarget_Checkbox",
                ref enableCurrentTarget))
            {
                Configuration.SetCurrentTargetOnRightClick = enableCurrentTarget;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("EnableCurrentTarget_HelpMarker",
                "right click on a player name to target them"));
            ImGui.Spacing();
        }

        private void EnableFocusTarget()
        {
            var enableFocusTarget = Configuration.SetFocusTargetOnHover;
            if (ImGui.Checkbox(
                Loc.Localize("EnableFocusTarget", "Set Focus Target on Hover") +
                "###PlayerTrack_EnableFocusTarget_Checkbox",
                ref enableFocusTarget))
            {
                Configuration.SetFocusTargetOnHover = enableFocusTarget;
                ConfigUpdated?.Invoke(this, true);
            }

            CustomWidgets.HelpMarker(Loc.Localize("EnableFocusTarget_HelpMarker",
                "hover on a player name to focus target them"));
            ImGui.Spacing();
        }

        public void DrawLinks()
        {
            var buttonSize = new Vector2(120f * Scale, 25f * Scale);
            if (ImGui.Button(Loc.Localize("OpenGithub", "Github") + "###PlayerTrack_OpenGithub_Button", buttonSize))
                Process.Start("https://github.com/kalilistic/PlayerTrack");
            if (ImGui.Button(Loc.Localize("PrintHelp", "Instructions") + "###PlayerTrack_PrintHelp_Button", buttonSize))
                RequestPrintHelp?.Invoke(this, true);
            if (ImGui.Button(
                Loc.Localize("ImproveTranslation", "Translations") + "###PlayerTrack_ImproveTranslation_Button",
                buttonSize))
                Process.Start("https://crowdin.com/project/playertrack");
        }

        private enum Tab
        {
            General,
            Display,
            Threshold,
            Filters,
            Icons,
            Lodestone,
            Alerts,
            Categories,
            Targets,
            Backup,
            Links
        }


        private enum Modal
        {
            None,
            Delete,
            Reset
        }
    }
}