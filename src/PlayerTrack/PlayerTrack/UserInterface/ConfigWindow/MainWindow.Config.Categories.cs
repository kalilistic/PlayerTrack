using System;
using System.Collections.Generic;
using System.Linq;

using CheapLoc;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;

namespace PlayerTrack
{
    /// <summary>
    /// Category Config.
    /// </summary>
    public partial class ConfigWindow
    {
        private void CategoryConfig()
        {
             // get sorted categories
            var categories = this.Plugin.CategoryService.GetSortedCategories().ToList();

            // don't display if no categories (shouldn't happen in theory)
            if (!categories.Any()) return;

            // add category
            if (ImGui.SmallButton(Loc.Localize("AddCategory", "Add Category") + "###PlayerTrack_CategoryAdd_Button"))
            {
                this.Plugin.CategoryService.AddCategory();
            }

            // setup category table
            ImGui.Separator();
            ImGui.Columns(7, "###PlayerTrack_CategoryTable_Columns", true);
            var baseWidth = ImGui.GetWindowSize().X / 4 * ImGuiHelpers.GlobalScale;
            ImGui.SetColumnWidth(0, baseWidth + 20f);                 // name
            ImGui.SetColumnWidth(1, ImGuiHelpers.GlobalScale * 70f);  // isDefault
            ImGui.SetColumnWidth(2, ImGuiHelpers.GlobalScale * 100f); // alerts
            ImGui.SetColumnWidth(3, baseWidth + 80f);                 // list
            ImGui.SetColumnWidth(4, ImGuiHelpers.GlobalScale * 100f); // visibility
            ImGui.SetColumnWidth(5, ImGuiHelpers.GlobalScale * 90f);  // fcnamecolor
            ImGui.SetColumnWidth(6, baseWidth + 200f);                // controls

            // add table headings
            ImGui.Text(Loc.Localize("CategoryName", "Name"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryDefault", "IsDefault"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryAlerts", "Alerts"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryList", "List Color/Icon"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryVisibility", "Visibility"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryFCNameColor", "FCNameColor"));
            ImGui.NextColumn();
            ImGui.Text(Loc.Localize("CategoryAction", "Actions"));
            ImGui.NextColumn();
            ImGui.Separator();

            // loop through categories
            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i].Value;

                // category name
                var categoryName = category.Name;
                ImGui.SetNextItemWidth(baseWidth);
                if (ImGui.InputText("###PlayerTrack_CategoryName_Input" + i, ref categoryName, 20))
                {
                    category.Name = categoryName;
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                // category default
                ImGui.NextColumn();
                var isDefault = category.IsDefault;
                if (isDefault)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.Text(FontAwesomeIcon.Check.ToIconString());
                    ImGui.PopFont();
                }
                else
                {
                    if (ImGui.Checkbox(
                        "###PlayerTrack_CategoryDefault_Checkbox" + i,
                        ref isDefault))
                    {
                        var oldDefaultCategory = this.Plugin.CategoryService.GetDefaultCategory();
                        category.IsDefault = isDefault;
                        this.Plugin.CategoryService.SaveCategory(category);
                        oldDefaultCategory.IsDefault = false;
                        this.Plugin.CategoryService.SaveCategory(oldDefaultCategory);
                    }
                }

                // category alerts
                ImGui.NextColumn();
                var lastSeenAlerts = category.IsAlertEnabled;
                if (ImGui.Checkbox(
                    "###PlayerTrack_EnableCategoryAlerts_Checkbox" + i,
                    ref lastSeenAlerts))
                {
                    category.IsAlertEnabled = lastSeenAlerts;
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                    ImGui.TextUnformatted(Loc.Localize("CategorySendLastSeenAlert", "send alert with when and where you last saw the player"));
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                ImGui.SameLine();
                var sendNameChangeAlert = category.IsNameChangeAlertEnabled;
                if (ImGui.Checkbox(
                    "###PlayerTrack_SendNameChangeAlert_Checkbox" + i,
                    ref sendNameChangeAlert))
                {
                    category.IsNameChangeAlertEnabled = sendNameChangeAlert;
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                    ImGui.TextUnformatted(Loc.Localize("CategorySendNameChangeAlert", "send name change alert in chat when detected from lodestone lookup"));
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                ImGui.SameLine();
                var sendWorldTransferAlert = category.IsWorldTransferAlertEnabled;
                if (ImGui.Checkbox(
                    "###PlayerTrack_SendWorldTransferAlert_Checkbox" + i,
                    ref sendWorldTransferAlert))
                {
                    category.IsWorldTransferAlertEnabled = sendWorldTransferAlert;
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                    ImGui.TextUnformatted(Loc.Localize("CategorySendWorldTransferAlert", "send world transfer alert in chat when detected from lodestone lookup"));
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                // category list color
                ImGui.NextColumn();
                var categoryListColor = category.EffectiveListColor();
                if (ImGui.ColorButton("List Color###PlayerTrack_CategoryListColor_Button" + i, categoryListColor))
                    ImGui.OpenPopup("###PlayerTrack_CategoryListColor_Popup" + i);
                if (ImGui.BeginPopup("###PlayerTrack_CategoryListColor_Popup" + i))
                {
                    if (ImGui.ColorPicker4("List Color###PlayerTrack_CategoryListColor_ColorPicker" + i, ref categoryListColor))
                    {
                        category.ListColor = categoryListColor;
                        this.Plugin.CategoryService.SaveCategory(category);
                    }

                    this.CategoryListColorSwatchRow(category, category.Id, 0, 8);
                    this.CategoryListColorSwatchRow(category, category.Id, 8, 16);
                    this.CategoryListColorSwatchRow(category, category.Id, 16, 24);
                    this.CategoryListColorSwatchRow(category, category.Id, 24, 32);
                    ImGui.EndPopup();
                }

                // category icon
                ImGui.SameLine();
                var categoryIcon = category.Icon;
                var namesList = new List<string> { Loc.Localize("Default", "Default") };
                namesList.AddRange(this.Plugin.Configuration.EnabledIcons.ToList()
                                       .Select(icon => icon.ToString()));
                var names = namesList.ToArray();
                var codesList = new List<int>
                {
                    0,
                };
                codesList.AddRange(this.Plugin.Configuration.EnabledIcons.ToList().Select(icon => (int)icon));
                var codes = codesList.ToArray();
                var iconIndex = Array.IndexOf(codes, categoryIcon);
                ImGui.SetNextItemWidth(baseWidth);
                if (ImGui.Combo("###PlayerTrack_SelectCategoryIcon_Combo" + i, ref iconIndex, names, names.Length))
                {
                    category.Icon = codes[iconIndex];
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(categoryIcon != 0
                               ? ((FontAwesomeIcon)categoryIcon).ToIconString()
                               : FontAwesomeIcon.User.ToIconString());
                ImGui.PopFont();

                // visibility
                ImGui.NextColumn();
                if (!category.IsDefault)
                {
                    var visibilityType = (int)category.VisibilityType;
                    ImGui.SetNextItemWidth(-1);
                    if (ImGui.Combo(
                        "###PlayerTrack_SelectCategoryVisibilityType_Combo" + i,
                        ref visibilityType,
                        Enum.GetNames(typeof(VisibilityType)),
                        Enum.GetNames(typeof(VisibilityType)).Length))
                    {
                        category.VisibilityType = (VisibilityType)visibilityType;
                        this.Plugin.CategoryService.SaveCategory(category);
                        this.plugin.VisibilityService.SyncWithVisibility();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                        ImGui.TextUnformatted(Loc.Localize("IsHiddenInVisibility", "void or whitelist players with visibility"));
                        ImGui.PopTextWrapPos();
                        ImGui.EndTooltip();
                    }
                }

                // category fcnamecolor
                ImGui.NextColumn();
                var overrideFCNameColor = category.OverrideFCNameColor;
                if (ImGui.Checkbox(
                    "###PlayerTrack_CategoryOverrideFCNameColor_Checkbox" + i,
                    ref overrideFCNameColor))
                {
                    category.OverrideFCNameColor = overrideFCNameColor;
                    this.Plugin.CategoryService.SaveCategory(category);
                    this.plugin.FCNameColorService.SyncWithFCNameColor();
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
                    ImGui.TextUnformatted(Loc.Localize(
                                              "CategoryFCNameColorOverride",
                                              "override fcnamecolor nameplate settings for this category by adding players to ignore list"));
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                // category actions
                ImGui.NextColumn();
                if (category.Rank != 0)
                {
                    if (ImGuiComponents.IconButton(category.Id + 1, FontAwesomeIcon.ArrowUp))
                    {
                        this.Plugin.CategoryService.IncreaseCategoryRank(category.Id);
                    }

                    ImGui.SameLine();
                }

                if (category.Rank != this.Plugin.CategoryService.MaxRank())
                {
                    if (ImGuiComponents.IconButton(category.Id + 2, FontAwesomeIcon.ArrowDown))
                    {
                        this.Plugin.CategoryService.DecreaseCategoryRank(category.Id);
                    }

                    ImGui.SameLine();
                }

                if (ImGuiComponents.IconButton(category.Id, FontAwesomeIcon.Redo))
                {
                    category.Reset();
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                ImGui.SameLine();

                if (!category.IsDefault)
                {
                    if (ImGuiComponents.IconButton(category.Id + 3, FontAwesomeIcon.Trash))
                    {
                        this.Plugin.CategoryService.DeleteCategory(category.Id);
                    }
                }

                ImGui.NextColumn();
            }

            ImGui.Separator();
        }

        private void CategoryListColorSwatchRow(Category category, int id, int min, int max)
        {
            ImGui.Spacing();
            for (var i = min; i < max; i++)
            {
                if (ImGui.ColorButton("###PlayerTrack_CategoryListColor_Swatch_" + id + i, this.colorPalette[i]))
                {
                    category.ListColor = this.colorPalette[i];
                    this.Plugin.CategoryService.SaveCategory(category);
                }

                ImGui.SameLine();
            }
        }

        private void CategoryNamePlateColorSwatchRow(Category category, int id, int min, int max)
        {
            ImGui.Spacing();
            for (var i = min; i < max; i++)
            {
                if (ImGui.ColorButton("###PlayerTrack_CategoryNamePlateColor_Swatch_" + id + i, this.colorPalette[i]))
                {
                    category.NamePlateColor = this.colorPalette[i];
                    this.Plugin.CategoryService.SaveCategory(category);
                    this.plugin.NamePlateManager.ForceRedraw();
                }

                ImGui.SameLine();
            }
        }
    }
}
