using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Gui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Loc.ImGui;
using ImGuiNET;
using PlayerTrack.Domain;
using PlayerTrack.Models;
using PlayerTrack.Models.Structs;

namespace PlayerTrack.UserInterface.Components;

using System.Numerics;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Interface.Utility;

using ViewModels;

public static class PlayerConfigComponent
{
    private static char[] enabledIconCodes = null!;
    private static string[] enabledIconNames = null!;

    public static string[] InheritOrOverride { get; } =
    {
        InheritOverride.Inherit.ToString(),
        InheritOverride.Override.ToString(),
    };

    public static PlayerConfig DrawPlayerConfigTabs(PlayerView player)
    {
        PrepareSettings();

        var playerConfigSet = new PlayerConfigSet
        {
            PlayerConfigType = PlayerConfigType.Player,
            CurrentPlayerConfig = player.PlayerConfig,
            CategoryPlayerConfigs = new List<PlayerConfig>(),
        };

        foreach (var category in player.AssignedCategories)
        {
            playerConfigSet.CategoryPlayerConfigs.Add(category.PlayerConfig);
        }

        return DrawConfigTabs(playerConfigSet);
    }

    public static PlayerConfig DrawCategoryConfigTabs(Category category)
    {
        PrepareSettings();
        var playerConfigSet = new PlayerConfigSet
        {
            PlayerConfigType = PlayerConfigType.Category,
            CurrentPlayerConfig = category.PlayerConfig,
        };

        return DrawConfigTabs(playerConfigSet);
    }

    public static PlayerConfig DrawDefaultConfigTabs()
    {
        PrepareSettings();
        var playerConfigSet = new PlayerConfigSet
        {
            PlayerConfigType = PlayerConfigType.Default,
            CurrentPlayerConfig = ServiceContext.ConfigService.GetConfig().PlayerConfig,
        };

        return DrawConfigTabs(playerConfigSet);
    }

    private static void PrepareSettings()
    {
        var enabledIcons = ServiceContext.ConfigService.GetConfig().Icons;
        enabledIconCodes = enabledIcons.Select(icon => icon.ToIconChar()).ToArray();
        enabledIconNames = enabledIcons.Select(icon => icon.ToString()).ToArray();
    }

    private static PlayerConfig DrawConfigTabs(PlayerConfigSet playerConfigSet)
    {
        if (LocGui.BeginTabItem("Display"))
        {
            ImGuiHelpers.ScaledDummy(2f);
            DrawColorPicker("NameColor", playerConfigSet, pc => pc.PlayerListNameColor, ref playerConfigSet.CurrentPlayerConfig.PlayerListNameColor, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            DrawIconPicker("Icon", playerConfigSet, pc => pc.PlayerListIcon, ref playerConfigSet.CurrentPlayerConfig.PlayerListIcon, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            ImGui.EndTabItem();
        }

        if (LocGui.BeginTabItem("Nameplate"))
        {
            ImGuiHelpers.ScaledDummy(1f);
            ToadGui.Section("NameplateColors", () =>
            {
                DrawCheckbox("NameplateUseColor", playerConfigSet, pc => pc.NameplateUseColor, ref playerConfigSet.CurrentPlayerConfig.NameplateUseColor, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
                DrawCheckbox("NameplateUseColorIfDead", playerConfigSet, pc => pc.NameplateUseColorIfDead, ref playerConfigSet.CurrentPlayerConfig.NameplateUseColorIfDead, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            
                if (playerConfigSet.CurrentPlayerConfig.NameplateUseColor.Value)
                {
                    DrawColorPicker("NameplateColor", playerConfigSet, pc => pc.NameplateColor, ref playerConfigSet.CurrentPlayerConfig.NameplateColor, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
                }
            });
            
            ToadGui.Section("NameplateTitle", () =>
            {
                DrawCombo("NameplateTitle", playerConfigSet, pc => pc.NameplateTitleType, ref playerConfigSet.CurrentPlayerConfig.NameplateTitleType, ref playerConfigSet.CurrentPlayerConfig.IsChanged, true);
                if (playerConfigSet.CurrentPlayerConfig.NameplateTitleType.Value == NameplateTitleType.CustomTitle)
                {
                    DrawTextConfig("CustomTitle", playerConfigSet, pc => pc.NameplateCustomTitle, ref playerConfigSet.CurrentPlayerConfig.NameplateCustomTitle, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
                }
            });
            
            ToadGui.Section("NameplateConditions", () =>
            {
                DrawCheckbox("ShowInOverworld", playerConfigSet, pc => pc.NameplateShowInOverworld, ref playerConfigSet.CurrentPlayerConfig.NameplateShowInOverworld, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
                DrawCheckbox("ShowInContent", playerConfigSet, pc => pc.NameplateShowInContent, ref playerConfigSet.CurrentPlayerConfig.NameplateShowInContent, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
                DrawCheckbox("ShowInHighEndContent", playerConfigSet, pc => pc.NameplateShowInHighEndContent, ref playerConfigSet.CurrentPlayerConfig.NameplateShowInHighEndContent, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            });

            ImGui.EndTabItem();
        }

        if (LocGui.BeginTabItem("Alerts"))
        {
            ImGuiHelpers.ScaledDummy(2f);
            DrawCheckbox("NameChangeAlert", playerConfigSet, pc => pc.AlertNameChange, ref playerConfigSet.CurrentPlayerConfig.AlertNameChange, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            DrawCheckbox("WorldTransferAlert", playerConfigSet, pc => pc.AlertWorldTransfer, ref playerConfigSet.CurrentPlayerConfig.AlertWorldTransfer, ref playerConfigSet.CurrentPlayerConfig.IsChanged);
            DrawCheckbox("ProximityAlert", playerConfigSet, pc => pc.AlertProximity, ref playerConfigSet.CurrentPlayerConfig.AlertProximity, ref playerConfigSet.CurrentPlayerConfig.IsChanged);

            ImGui.EndTabItem();
        }

        if (playerConfigSet.PlayerConfigType != PlayerConfigType.Default)
        {
            if (LocGui.BeginTabItem("Integrations"))
            {
                ImGuiHelpers.ScaledDummy(2f);
                DrawCombo("VisibilityType", playerConfigSet, pc => pc.VisibilityType, ref playerConfigSet.CurrentPlayerConfig.VisibilityType, ref playerConfigSet.CurrentPlayerConfig.IsChanged, false);

                ImGui.EndTabItem();
            }
        }

        return playerConfigSet.CurrentPlayerConfig;
    }

    private static void DrawCheckbox<T>(
        string key,
        PlayerConfigSet playerConfigSet,
        Func<PlayerConfig, ConfigValue<T>> propertySelector,
        ref ConfigValue<bool> config,
        ref bool isChanged)
    {
        var extractedProperty = PlayerConfigService.ExtractProperty(playerConfigSet, propertySelector);
        if (extractedProperty.PropertyValue is not bool boolValue)
        {
            return;
        }

        DrawSourceIndicator(playerConfigSet.PlayerConfigType, extractedProperty.PlayerConfigType, extractedProperty.CategoryId);
        DrawInheritOverrideCombo(key, playerConfigSet.PlayerConfigType, ref config.InheritOverride, ref isChanged);

        if (ToadGui.Checkbox(key, ref boolValue))
        {
            config.InheritOverride = playerConfigSet.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Override;
            config.Value = boolValue;
            isChanged = true;
        }
    }

    private static void DrawTextConfig<T>(
        string key,
        PlayerConfigSet playerConfigSet,
        Func<PlayerConfig, ConfigValue<T>> propertySelector,
        ref ConfigValue<string> config,
        ref bool isChanged)
    {
        var extractedProperty = PlayerConfigService.ExtractProperty(playerConfigSet, propertySelector);
        if (extractedProperty.PropertyValue is not string stringValue)
        {
            return;
        }

        DrawSourceIndicator(playerConfigSet.PlayerConfigType, extractedProperty.PlayerConfigType, extractedProperty.CategoryId);
        DrawInheritOverrideCombo(key, playerConfigSet.PlayerConfigType, ref config.InheritOverride, ref isChanged);

        ImGui.SetNextItemWidth(ImGuiUtil.CalcScaledComboWidth(150f));
        if (ToadGui.InputText(key, ref stringValue, 150))
        {
            config.InheritOverride = playerConfigSet.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Override;
            config.Value = stringValue;
            isChanged = true;
        }
    }

    private static void DrawColorPicker<T>(
        string key,
        PlayerConfigSet playerConfigSet,
        Func<PlayerConfig, ConfigValue<T>> propertySelector,
        ref ConfigValue<uint> config,
        ref bool isChanged)
    {
        var extractedProperty = PlayerConfigService.ExtractProperty(playerConfigSet, propertySelector);
        if (extractedProperty.PropertyValue is not uint uintValue)
        {
            return;
        }

        DrawSourceIndicator(playerConfigSet.PlayerConfigType, extractedProperty.PlayerConfigType, extractedProperty.CategoryId);
        DrawInheritOverrideCombo(key, playerConfigSet.PlayerConfigType, ref config.InheritOverride, ref isChanged);

        var color = DalamudContext.DataManager.GetUIColorAsVector4(uintValue);
        if (ToadGui.SimpleUIColorPicker(key, uintValue, ref color))
        {
            config.InheritOverride = playerConfigSet.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Override;
            config.Value = DalamudContext.DataManager.FindClosestUIColor(color).Id;
            isChanged = true;
        }
    }

    private static void DrawIconPicker<T>(
        string key,
        PlayerConfigSet playerConfigSet,
        Func<PlayerConfig, ConfigValue<T>> propertySelector,
        ref ConfigValue<char> config,
        ref bool isChanged)
    {
        var extractedProperty = PlayerConfigService.ExtractProperty(playerConfigSet, propertySelector);
        if (extractedProperty.PropertyValue is not char charValue)
        {
            return;
        }

        DrawSourceIndicator(playerConfigSet.PlayerConfigType, extractedProperty.PlayerConfigType, extractedProperty.CategoryId);
        DrawInheritOverrideCombo(key, playerConfigSet.PlayerConfigType, ref config.InheritOverride, ref isChanged);

        if (ToadGui.IconPicker(key, ref charValue, enabledIconCodes, enabledIconNames))
        {
            config.InheritOverride = playerConfigSet.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Override;
            config.Value = charValue;
            isChanged = true;
        }
    }

    private static void DrawCombo<TEnum>(
        string key,
        PlayerConfigSet playerConfigSet,
        Func<PlayerConfig, ConfigValue<TEnum>> propertySelector,
        ref ConfigValue<TEnum> config,
        ref bool isChanged,
        bool isAvailableForDefault) where TEnum : Enum
    {
        var extractedProperty = PlayerConfigService.ExtractProperty(playerConfigSet, propertySelector);
        object displayValue = config.InheritOverride == InheritOverride.Inherit ? extractedProperty.PropertyValue : config.Value;
        if (!isAvailableForDefault && extractedProperty.PlayerConfigType == PlayerConfigType.Default)
        {
            extractedProperty.PlayerConfigType = PlayerConfigType.Category;
        }

        if (displayValue is not TEnum enumValue)
        {
            DalamudContext.PluginLog.Error($"Failed to cast {displayValue} to {typeof(TEnum)}");
            return;
        }

        if (isAvailableForDefault || !isAvailableForDefault && playerConfigSet.PlayerConfigType == PlayerConfigType.Player)
        {
            DrawSourceIndicator(playerConfigSet.PlayerConfigType, extractedProperty.PlayerConfigType, extractedProperty.CategoryId);
            DrawInheritOverrideCombo(key, playerConfigSet.PlayerConfigType, ref config.InheritOverride, ref isChanged);
        }

        var enumNames = Enum.GetNames(typeof(TEnum));
        var selectedIndex = Array.IndexOf(enumNames, enumValue.ToString());

        ImGui.SetNextItemWidth(ImGuiUtil.CalcScaledComboWidth(150f));
        if (ToadGui.Combo(key, ref selectedIndex, enumNames, 150, false))
        {
            var selectedEnum = (TEnum)Enum.Parse(typeof(TEnum), enumNames[selectedIndex]);
            config.Value = selectedEnum;
            isChanged = true;
            config.InheritOverride = playerConfigSet.PlayerConfigType == PlayerConfigType.Default ? InheritOverride.None : InheritOverride.Override;
        }
    }

    private static void DrawSourceIndicator(PlayerConfigType currentPlayerConfigType, PlayerConfigType sourcePlayerConfigType, int categoryId)
    {
        if (currentPlayerConfigType == PlayerConfigType.Default)
        {
            return;
        }

        const float iconOffset = 2.0f;
        if (currentPlayerConfigType == sourcePlayerConfigType)
        {
            ImGui.BeginGroup();
            var currentPosY = ImGui.GetCursorPosY();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + iconOffset * ImGuiHelpers.GlobalScale);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 19f * ImGuiHelpers.GlobalScale);
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextColored(Vector4.Zero, FontAwesomeIcon.None.ToIconString());
            ImGui.PopFont();
            ImGui.SetCursorPosY(currentPosY);
            ImGui.EndGroup();
            return;
        }

        switch (sourcePlayerConfigType)
        {
            case PlayerConfigType.Category:
                var category = ServiceContext.CategoryService.GetCategoryById(categoryId);
                var categoryName = category == null ? string.Empty : category.Name;
                DrawSourceIndicatorIcon(string.Format(ServiceContext.Localization.GetString("CategorySpecificSetting"), categoryName), FontAwesomeIcon.FolderOpen, iconOffset);
                break;
            case PlayerConfigType.Default:
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 3f * ImGuiHelpers.GlobalScale);
                DrawSourceIndicatorIcon(ServiceContext.Localization.GetString("DefaultSetting"), FontAwesomeIcon.GlobeAmericas, iconOffset);
                break;
            case PlayerConfigType.Player:
            default:
                throw new ArgumentOutOfRangeException(nameof(sourcePlayerConfigType), sourcePlayerConfigType, null);
        }
    }

    private static void DrawSourceIndicatorIcon(string text, FontAwesomeIcon icon, float offset)
    {
        ImGui.BeginGroup();
        var currentPosY = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + offset * ImGuiHelpers.GlobalScale);
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(icon.ToIconString());
        ImGui.PopFont();
        ImGui.SetCursorPosY(currentPosY);
        ImGui.EndGroup();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(text);
        }

        ImGui.SameLine();
    }

    private static void DrawInheritOverrideCombo(string key, PlayerConfigType playerConfigType, ref InheritOverride option, ref bool isChanged)
    {
        if (playerConfigType == PlayerConfigType.Default)
        {
            return;
        }

        var optionIndex = Array.IndexOf(InheritOrOverride, option.ToString());
        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGuiUtil.CalcScaledComboWidth(90f));
        if (ToadGui.Combo($"###{key}_InheritOverrideCombo", ref optionIndex, InheritOrOverride, 100, false, false))
        {
            option = (InheritOverride)Enum.Parse(typeof(InheritOverride), InheritOrOverride[optionIndex]);
            isChanged = true;
        }

        ImGui.SameLine();
    }
}
