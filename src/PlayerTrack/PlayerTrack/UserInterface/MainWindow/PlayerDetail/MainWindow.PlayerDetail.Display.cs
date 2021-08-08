using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.DrunkenToad;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Player Detail Display Options View.
    /// </summary>
    public partial class MainWindow
    {
        private readonly List<Vector4> colorPalette = ImGuiHelpers.DefaultColorPalette(36);

        private void PlayerDisplay()
        {
            if (this.SelectedPlayer == null) return;
            const float sameLineOffset = 100f;

            ImGui.Text(Loc.Localize("PlayerCategory", "Category"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            var categoryNames = this.plugin.CategoryService.GetCategoryNames().ToArray();
            var categoryIds = this.plugin.CategoryService.GetCategoryIds().ToArray();
            var currentCategory = this.plugin.CategoryService.GetCategory(this.SelectedPlayer.CategoryId);
            var categoryIndex = Array.IndexOf(categoryNames, currentCategory.Name);
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo(
                "###PlayerTrack_PlayerCategory_Combo",
                ref categoryIndex,
                categoryNames,
                categoryNames.Length))
            {
                this.SelectedPlayer.CategoryId = categoryIds[categoryIndex];
                this.Plugin.PlayerService.UpdatePlayerCategory(this.SelectedPlayer);
                this.plugin.NamePlateManager.ForceRedraw();
            }

            ImGuiHelpers.ScaledDummy(0.5f);
            ImGui.Separator();
            ImGui.TextColored(ImGuiColors2.ToadYellow, Loc.Localize(
                                  "OverrideNote",
                                  "These config will override category config."));
            ImGuiHelpers.ScaledDummy(1f);

            ImGui.Text(Loc.Localize("Title", "Title"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);

            var title = this.SelectedPlayer.Title;
            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText("###PlayerTrack_PlayerTitle_Input", ref title, 30))
            {
                this.SelectedPlayer.Title = title;
                this.Plugin.PlayerService.UpdatePlayerTitle(this.SelectedPlayer);
            }

            ImGui.Spacing();

            ImGui.Text(Loc.Localize("Icon", "Icon"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);

            ImGui.SetNextItemWidth(150f * ImGuiHelpers.GlobalScale);
            var iconIndex = this.plugin.IconListIndex(this.SelectedPlayer!.Icon);
            if (ImGui.Combo(
              "###PlayerTrack_PlayerIcon_Combo",
              ref iconIndex,
              this.plugin.IconListNames(),
              this.plugin.IconListNames().Length))
            {
              this.SelectedPlayer.Icon = this.plugin.IconListCodes()[iconIndex];
              this.Plugin.PlayerService.UpdatePlayerIcon(this.SelectedPlayer);
            }

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(
                ImGuiColors.White,
                ((FontAwesomeIcon)this.SelectedPlayer.Icon).ToIconString());
            ImGui.PopFont();

            ImGui.Spacing();
            ImGui.Text(Loc.Localize("List", "List"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            var listColor = this.SelectedPlayer.EffectiveListColor();
            if (ImGui.ColorButton("List Color###PlayerTrack_PlayerListColor_Button", listColor))
            {
                ImGui.OpenPopup("###PlayerTrack_PlayerListColor_Popup");
            }

            if (ImGui.BeginPopup("###PlayerTrack_PlayerListColor_Popup"))
            {
                if (ImGui.ColorPicker4("List Color###PlayerTrack_PlayerListColor_ColorPicker", ref listColor))
                {
                    this.SelectedPlayer.ListColor = listColor;
                    this.Plugin.PlayerService.UpdatePlayerListColor(this.SelectedPlayer);
                }

                this.PlayerOverride_ListColorSwatchRow(0, 8);
                this.PlayerOverride_ListColorSwatchRow(8, 16);
                this.PlayerOverride_ListColorSwatchRow(16, 24);
                this.PlayerOverride_ListColorSwatchRow(24, 32);
                ImGui.EndPopup();
            }

            ImGui.Spacing();
            ImGui.Text(Loc.Localize("Nameplate", "Nameplate"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            var namePlateColor = this.SelectedPlayer.EffectiveNamePlateColor();
            if (ImGui.ColorButton("NamePlate Color###PlayerTrack_PlayerNamePlateColor_Button", namePlateColor))
            {
                ImGui.OpenPopup("###PlayerTrack_PlayerNamePlateColor_Popup");
            }

            if (ImGui.BeginPopup("###PlayerTrack_PlayerNamePlateColor_Popup"))
            {
                if (ImGui.ColorPicker4("NamePlate Color###PlayerTrack_PlayerNamePlateColor_ColorPicker", ref namePlateColor))
                {
                    this.SelectedPlayer.NamePlateColor = namePlateColor;
                    this.Plugin.PlayerService.UpdatePlayerNamePlateColor(this.SelectedPlayer);
                }

                this.PlayerOverride_NamePlateColorSwatchRow(0, 8);
                this.PlayerOverride_NamePlateColorSwatchRow(8, 16);
                this.PlayerOverride_NamePlateColorSwatchRow(16, 24);
                this.PlayerOverride_NamePlateColorSwatchRow(24, 32);
                ImGui.EndPopup();
            }

            ImGui.Spacing();
            ImGui.Text(Loc.Localize("Alerts", "Alerts"));
            ImGuiHelpers.ScaledRelativeSameLine(sameLineOffset);
            var isAlertEnabled = this.SelectedPlayer.IsAlertEnabled;
            if (ImGui.Checkbox(
                "###PlayerTrack_PlayerAlerts_Checkbox",
                ref isAlertEnabled))
            {
                this.SelectedPlayer.IsAlertEnabled = isAlertEnabled;
                this.Plugin.PlayerService.UpdatePlayerAlert(this.SelectedPlayer);
            }

            ImGuiHelpers.ScaledDummy(5f);
            if (ImGui.Button(Loc.Localize("Reset", "Reset") + "###PlayerTrack_PlayerOverrideModalReset_Button"))
            {
                this.SelectedPlayer.Reset();
                this.Plugin.PlayerService.ResetPlayerOverrides(this.SelectedPlayer);
            }
        }

        private void PlayerOverride_ListColorSwatchRow(int min, int max)
        {
            ImGui.Spacing();
            for (var i = min; i < max; i++)
            {
                if (ImGui.ColorButton("###PlayerTrack_PlayerListColor_Swatch_" + i, this.colorPalette[i]))
                {
                    this.SelectedPlayer!.ListColor = this.colorPalette[i];
                    this.Plugin.PlayerService.UpdatePlayerListColor(this.SelectedPlayer);
                }

                ImGui.SameLine();
            }
        }

        private void PlayerOverride_NamePlateColorSwatchRow(int min, int max)
        {
            ImGui.Spacing();
            for (var i = min; i < max; i++)
            {
                if (ImGui.ColorButton("###PlayerTrack_PlayerNamePlateColor_Swatch_" + i, this.colorPalette[i]))
                {
                    this.SelectedPlayer!.NamePlateColor = this.colorPalette[i];
                    this.Plugin.PlayerService.UpdatePlayerNamePlateColor(this.SelectedPlayer);
                }

                ImGui.SameLine();
            }
        }
    }
}
