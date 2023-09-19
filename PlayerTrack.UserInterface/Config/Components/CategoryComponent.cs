using System;
using System.Collections.Generic;
using Dalamud.DrunkenToad.Gui;
using Dalamud.DrunkenToad.Gui.Enums;
using Dalamud.Interface;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Config.Components;

using Dalamud.Logging;

public class CategoryComponent : ConfigViewComponent
{
    private string categoryInput = string.Empty;
    private Tuple<ActionRequest, Category>? categoryToDelete;

    public override void Draw()
    {
        if (ImGui.BeginTabBar("Categories_TabBar", ImGuiTabBarFlags.None))
        {
            var categories = ServiceContext.CategoryService.GetAllCategories();
            this.DrawCategoryManagementTab(categories);
            foreach (var category in categories)
            {
                this.DrawCategoryTab(category);
            }
        }

        ImGui.EndTabBar();
    }

    private void DrawCategoryTab(Category category)
    {
        if (LocGui.BeginTabItem(category.Name))
        {
            this.DrawTabBar(category);
            ImGui.EndTabItem();
        }
    }

    private void DrawTabBar(Category category)
    {
        ImGuiHelpers.ScaledDummy(3f);
        ImGuiHelpers.ScaledIndent(6f);
        if (ImGui.BeginTabBar("###PlayerOverrideTabBar", ImGuiTabBarFlags.None))
        {
            category.PlayerConfig.PlayerConfigType = PlayerConfigType.Category;
            category.PlayerConfig = PlayerConfigComponent.DrawCategoryConfigTabs(category);
            if (category.PlayerConfig.IsChanged)
            {
                category.PlayerConfig.IsChanged = false;
                PlayerConfigService.UpdateCategoryConfig(category.Id, category.PlayerConfig);
                this.NotifyConfigChanged();
            }
        }

        ImGui.EndTabBar();
    }

    private void DrawCategoryManagementTab(IEnumerable<Category> categories)
    {
        if (LocGui.BeginTabItem("Categories"))
        {
            this.DrawCategoriesAndNewInput(categories);
            ImGui.EndTabItem();
        }
    }

    private void DrawCategoriesAndNewInput(IEnumerable<Category> categories)
    {
        ImGuiHelpers.ScaledDummy(1f);
        foreach (var category in categories)
        {
            this.DrawCategoryItem(category);
        }

        this.DrawNewCategoryInput();
    }

    private void DrawCategoryItem(Category category)
    {
        this.DrawAndHandleEditInput(category);
        this.DrawAndHandleDeleteIcon(category);
        this.DrawAndHandleRankIcons(category);
    }

    private void DrawAndHandleEditInput(Category category)
    {
        var name = category.Name;
        ToadGui.SetNextItemWidth(240f);
        if (ToadGui.InputText("###EditCategoryInput" + category.Id, ref name, 20))
        {
            category.Name = name;
            ServiceContext.CategoryService.UpdateCategory(category);
            this.NotifyConfigChanged();
        }
    }

    private void DrawAndHandleDeleteIcon(Category category)
    {
        ImGui.SameLine();
        ToadGui.Confirm(category, FontAwesomeIcon.Trash, "ConfirmDelete", ref this.categoryToDelete);
        if (this.categoryToDelete?.Item1 == ActionRequest.Confirmed)
        {
            ServiceContext.CategoryService.DeleteCategory(this.categoryToDelete.Item2);
            this.categoryToDelete = null;
            this.NotifyConfigChanged();
        }
        else if (this.categoryToDelete?.Item1 == ActionRequest.None)
        {
            this.categoryToDelete = null;
        }
    }

    private void DrawAndHandleRankIcons(Category category)
    {
        ImGui.PushFont(UiBuilder.IconFont);
        this.DrawAndHandleRankUp(category);
        this.DrawAndHandleRankDown(category);
        ImGui.PopFont();
    }

    private void DrawAndHandleRankUp(Category category)
    {
        if (!ServiceContext.CategoryService.IsMinRankCategory(category))
        {
            ImGui.SameLine();
            LocGui.Text(FontAwesomeIcon.ArrowUp.ToIconString());
            if (ImGui.IsItemClicked())
            {
                ServiceContext.CategoryService.IncreaseCategoryRank(category.Id);
                this.NotifyConfigChanged();
            }
        }
    }

    private void DrawAndHandleRankDown(Category category)
    {
        if (!ServiceContext.CategoryService.IsMaxRankCategory(category))
        {
            ImGui.SameLine();
            LocGui.Text(FontAwesomeIcon.ArrowDown.ToIconString());
            if (ImGui.IsItemClicked())
            {
                ServiceContext.CategoryService.DecreaseCategoryRank(category.Id);
                this.NotifyConfigChanged();
            }
        }
    }

    private void DrawNewCategoryInput()
    {
        ToadGui.SetNextItemWidth(240f);
        LocGui.InputTextWithHint("###AddCategoryInput", "NewCategoryHint", ref this.categoryInput, 20);
        this.DrawAndHandleAddIcon();
    }

    private void DrawAndHandleAddIcon()
    {
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        LocGui.Text(FontAwesomeIcon.Plus.ToIconString());
        if (ImGui.IsItemClicked() && !string.IsNullOrEmpty(this.categoryInput))
        {
            ServiceContext.CategoryService.CreateCategory(this.categoryInput);
            this.categoryInput = string.Empty;
            this.NotifyConfigChanged();
        }

        ImGui.PopFont();
    }
}
