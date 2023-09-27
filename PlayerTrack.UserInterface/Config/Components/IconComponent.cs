using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Interface.Utility;

public class IconComponent : ConfigViewComponent
{
    private readonly List<FontAwesomeIcon> icons;
    private readonly Dictionary<string, List<FontAwesomeIcon>> categorizedIcons;
    private readonly string[] iconCategories;
    private readonly List<FontAwesomeIcon> enabledIcons;
    private int selectedIconCategory;
    private string iconSearchInput = string.Empty;
    private bool onlyShowEnabledIcons;

    public IconComponent()
    {
        this.iconCategories = FontAwesomeHelpers.GetCategories();
        this.icons = FontAwesomeHelpers.GetIcons();
        this.icons.Remove(FontAwesomeIcon.None);
        this.enabledIcons = this.config.Icons;
        this.categorizedIcons = SetupCategorizedIcons(this.icons, this.iconCategories);
    }

    public override void Draw()
    {
        this.DrawIconSelectionControls();
        this.DrawIconsTable();
    }

    private static Dictionary<string, List<FontAwesomeIcon>> SetupCategorizedIcons(List<FontAwesomeIcon> iconsToCategorize, IEnumerable<string> categories)
    {
        var iconsDict = categories.ToDictionary(category => category, _ => new List<FontAwesomeIcon>());

        foreach (var icon in iconsToCategorize)
        {
            foreach (var category in icon.GetCategories())
            {
                iconsDict[category].Add(icon);
            }
        }

        return iconsDict;
    }

    private void DrawIconSelectionControls()
    {
        ToadGui.Combo("####FontAwesomeCategorySearch", ref this.selectedIconCategory, this.iconCategories, 160);
        ToadGui.SameLine(170f);
        ToadGui.SetNextItemWidth(180f);
        ImGui.InputTextWithHint($"###FontAwesomeInputSearch", this.loc.GetString("SearchIconsHint"), ref this.iconSearchInput, 50);
        ImGui.SameLine();
        ToadGui.Checkbox("OnlyShowEnabledIcons", ref this.onlyShowEnabledIcons);
    }

    private void DrawIconsTable()
    {
        var filteredIcons = this.FilterIcons();
        ImGuiHelpers.ScaledDummy(2f);
        if (ImGui.BeginTable("IconTable", 3, ImGuiTableFlags.None))
        {
            foreach (var icon in filteredIcons)
            {
                this.DrawIconCell(icon);
                ImGui.TableNextColumn();
            }

            ImGui.EndTable();
        }
    }

    private IEnumerable<FontAwesomeIcon> FilterIcons()
    {
        var category = this.iconCategories[this.selectedIconCategory];
        var visibleIcons = this.selectedIconCategory == 0 ? this.icons : this.categorizedIcons[category];
        return visibleIcons.Where(icon =>
            (!this.onlyShowEnabledIcons || this.enabledIcons.Contains(icon)) &&
            (string.IsNullOrEmpty(this.iconSearchInput) || Enum.GetName(icon)!.Contains(this.iconSearchInput, StringComparison.OrdinalIgnoreCase)));
    }

    private void DrawIconCell(FontAwesomeIcon icon)
    {
        var isEnabledIcon = this.enabledIcons.Contains(icon);
        if (isEnabledIcon)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
        }

        ImGui.BeginGroup();
        ImGui.PushFont(UiBuilder.IconFont);
        LocGui.Text(icon.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        LocGui.Text(Enum.GetName(icon)!);

        if (isEnabledIcon)
        {
            ImGui.PopStyleColor();
        }

        ImGui.EndGroup();
        if (ImGui.IsItemClicked())
        {
            this.ToggleIcon(icon);
        }
    }

    private void ToggleIcon(FontAwesomeIcon icon)
    {
        if (this.enabledIcons.Contains(icon))
        {
            this.enabledIcons.Remove(icon);
        }
        else
        {
            this.enabledIcons.Add(icon);
        }

        this.config.Icons = this.enabledIcons;
        ServiceContext.ConfigService.SaveConfig(this.config);
        this.NotifyConfigChanged();
    }
}
