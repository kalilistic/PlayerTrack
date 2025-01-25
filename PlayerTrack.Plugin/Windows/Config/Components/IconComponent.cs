using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Resource;

namespace PlayerTrack.Windows.Config.Components;

public class IconComponent : ConfigViewComponent
{
    private readonly List<FontAwesomeIcon> Icons;
    private readonly Dictionary<string, List<FontAwesomeIcon>> CategorizedIcons;
    private readonly string[] IconCategories;
    private readonly List<FontAwesomeIcon> EnabledIcons;
    private int SelectedIconCategory;
    private string IconSearchInput = string.Empty;
    private bool OnlyShowEnabledIcons;

    public IconComponent()
    {
        IconCategories = FontAwesomeHelpers.GetCategories();
        Icons = FontAwesomeHelpers.GetIcons();
        Icons.Remove(FontAwesomeIcon.None);
        EnabledIcons = Config.Icons;
        CategorizedIcons = SetupCategorizedIcons(Icons, IconCategories);
    }

    public override void Draw()
    {
        DrawIconSelectionControls();
        DrawIconsTable();
    }

    private static Dictionary<string, List<FontAwesomeIcon>> SetupCategorizedIcons(List<FontAwesomeIcon> iconsToCategorize, IEnumerable<string> categories)
    {
        var iconsDict = categories.ToDictionary(category => category, _ => new List<FontAwesomeIcon>());

        foreach (var icon in iconsToCategorize)
            foreach (var category in icon.GetCategories())
                iconsDict[category].Add(icon);

        return iconsDict;
    }

    private void DrawIconSelectionControls()
    {
        Helper.Combo("###FontAwesomeCategorySearch", ref SelectedIconCategory, IconCategories, 160);
        ImGui.SameLine(170f * ImGuiHelpers.GlobalScale);
        ImGui.SetNextItemWidth(180f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("###FontAwesomeInputSearch", Language.SearchIconsHint, ref IconSearchInput, 50);
        ImGui.SameLine();
        Helper.Checkbox(Language.OnlyShowEnabledIcons, ref OnlyShowEnabledIcons);
    }

    private void DrawIconsTable()
    {
        var filteredIcons = FilterIcons();
        ImGuiHelpers.ScaledDummy(2f);

        using var table = ImRaii.Table("IconTable", 3, ImGuiTableFlags.None);
        if (!table.Success)
            return;

        foreach (var icon in filteredIcons)
        {
            DrawIconCell(icon);
            ImGui.TableNextColumn();
        }
    }

    private IEnumerable<FontAwesomeIcon> FilterIcons()
    {
        var category = IconCategories[SelectedIconCategory];
        var visibleIcons = SelectedIconCategory == 0 ? Icons : CategorizedIcons[category];
        return visibleIcons.Where(icon =>
            (!OnlyShowEnabledIcons || EnabledIcons.Contains(icon)) &&
            (string.IsNullOrEmpty(IconSearchInput) || Enum.GetName(icon)!.Contains(IconSearchInput, StringComparison.OrdinalIgnoreCase)));
    }

    private void DrawIconCell(FontAwesomeIcon icon)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen, EnabledIcons.Contains(icon)))
        using (ImRaii.Group())
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
                ImGui.TextUnformatted(icon.ToIconString());

            ImGui.SameLine();

            ImGui.TextUnformatted(Enum.GetName(icon)!);
        }

        if (ImGui.IsItemClicked())
            ToggleIcon(icon);
    }

    private void ToggleIcon(FontAwesomeIcon icon)
    {
        if (!EnabledIcons.Remove(icon))
            EnabledIcons.Add(icon);

        Config.Icons = EnabledIcons;
        ServiceContext.ConfigService.SaveConfig(Config);
        NotifyConfigChanged();
    }
}
