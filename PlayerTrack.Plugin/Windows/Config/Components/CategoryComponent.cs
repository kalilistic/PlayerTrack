using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Enums;
using PlayerTrack.Models;
using PlayerTrack.Resource;
using PlayerTrack.Windows.Components;

namespace PlayerTrack.Windows.Config.Components;

public class CategoryComponent : ConfigViewComponent
{
    private string CategoryInput = string.Empty;
    private Tuple<ActionRequest, Category>? CategoryToDelete;
    private int SelectedCategoryIndex;

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("Categories_TabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        var categories = ServiceContext.CategoryService.GetCategories();
        DrawCategoryManagementTab(categories);
        DrawEditCategoriesTab(categories);
    }

    private void DrawEditCategoriesTab(IReadOnlyList<Category> categories)
    {
        if (categories.Count == 0)
            return;

        using var tabItem = ImRaii.TabItem(Language.Settings);
        if (!tabItem.Success)
            return;

        var categoryNames = categories.Select(category => category.Name).ToList();
        Helper.Combo(Language.SelectCategory, ref SelectedCategoryIndex, categoryNames, 250);
        if (SelectedCategoryIndex >= 0 && SelectedCategoryIndex < categories.Count)
            DrawTabBar(categories[SelectedCategoryIndex]);
    }

    private void DrawTabBar(Category category)
    {
        ImGuiHelpers.ScaledDummy(3f);
        ImGuiHelpers.ScaledIndent(6f);
        using var tabBar = ImRaii.TabBar("PlayerOverrideTabBar", ImGuiTabBarFlags.None);
        if (!tabBar.Success)
            return;

        category.PlayerConfig.PlayerConfigType = PlayerConfigType.Category;
        category.PlayerConfig = PlayerConfigComponent.DrawCategoryConfigTabs(category);
        if (category.PlayerConfig.IsChanged)
        {
            category.PlayerConfig.IsChanged = false;
            PlayerConfigService.UpdateCategoryConfig(category.Id, category.PlayerConfig);
            NotifyConfigChanged();
        }
    }

    private void DrawCategoryManagementTab(IEnumerable<Category> categories)
    {
        using var tabItem = ImRaii.TabItem(Language.Categories);
        if (!tabItem.Success)
            return;

        DrawCategoriesAndNewInput(categories);
        DrawNoCategoryPlacement();
    }

    private void DrawNoCategoryPlacement()
    {
        var noCategoryPlacement = Config.NoCategoryPlacement;

        using var tabItem = ImRaii.TabItem(Language.Categories);
        if (!tabItem.Success)
            return;

        if (Helper.Combo(Language.NoCategoryPlacement, ref noCategoryPlacement, 80))
        {
            Config.NoCategoryPlacement = noCategoryPlacement;
            ServiceContext.ConfigService.SaveConfig(Config);
            NotifyConfigChanged();
        }
    }

    private void DrawCategoriesAndNewInput(IEnumerable<Category> categories)
    {
        ImGuiHelpers.ScaledDummy(1f);
        foreach (var category in categories)
            DrawCategoryItem(category);

        DrawNewCategoryInput();
    }

    private void DrawCategoryItem(Category category)
    {
        DrawAndHandleEditInput(category);
        DrawAndHandleDeleteIcon(category);
        DrawAndHandleResetIcon(category);
        DrawAndHandleRankIcons(category);
    }

    private void DrawAndHandleEditInput(Category category)
    {
        var name = category.Name;
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        if (ImGui.InputText($"###EditCategoryInput{category.Id}", ref name, 50))
        {
            category.Name = name;
            ServiceContext.CategoryService.UpdateCategory(category);
            NotifyConfigChanged();
        }
    }

    private void DrawAndHandleDeleteIcon(Category category)
    {
        if (category.IsDynamicCategory())
            return;

        ImGui.SameLine();
        Helper.Confirm(category, FontAwesomeIcon.Trash, Language.ConfirmDelete, ref CategoryToDelete);
        if (CategoryToDelete?.Item1 == ActionRequest.Confirmed)
        {
            ServiceContext.CategoryService.DeleteCategory(CategoryToDelete.Item2);
            CategoryToDelete = null;
            NotifyConfigChanged();
        }
        else if (CategoryToDelete?.Item1 == ActionRequest.None)
        {
            CategoryToDelete = null;
        }
    }

    private void DrawAndHandleResetIcon(Category category)
    {
        if (!category.IsDynamicCategory())
            return;

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Redo.ToIconString());
        }

        if (ImGui.IsItemClicked())
            SocialListService.ResetCategoryName(category);

        if (ImGui.IsItemHovered())
            Helper.Tooltip(string.Format(Language.ResetCategoryTooltip, SocialListService.GetCategoryName(category)));
    }

    private void DrawAndHandleRankIcons(Category category)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            DrawAndHandleRankUp(category);
            DrawAndHandleRankDown(category);
        }
    }

    private void DrawAndHandleRankUp(Category category)
    {
        if (!ServiceContext.CategoryService.IsMinRankCategory(category))
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(FontAwesomeIcon.ArrowUp.ToIconString());
            if (ImGui.IsItemClicked())
            {
                ServiceContext.CategoryService.IncreaseCategoryRank(category.Id);
                NotifyConfigChanged();
            }
        }
    }

    private void DrawAndHandleRankDown(Category category)
    {
        if (!ServiceContext.CategoryService.IsMaxRankCategory(category))
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(FontAwesomeIcon.ArrowDown.ToIconString());
            if (ImGui.IsItemClicked())
            {
                ServiceContext.CategoryService.DecreaseCategoryRank(category.Id);
                NotifyConfigChanged();
            }
        }
    }

    private void DrawNewCategoryInput()
    {
        ImGui.SetNextItemWidth(240f * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("###AddCategoryInput", Language.NewCategoryHint, ref CategoryInput, 20);
        DrawAndHandleAddIcon();
    }

    private void DrawAndHandleAddIcon()
    {
        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.Plus.ToIconString());
            if (ImGui.IsItemClicked() && !string.IsNullOrEmpty(CategoryInput))
            {
                ServiceContext.CategoryService.CreateCategory(CategoryInput);
                CategoryInput = string.Empty;
                NotifyConfigChanged();
            }
        }
    }
}
