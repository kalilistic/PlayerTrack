using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Enums;
using Dalamud.DrunkenToad.Gui.Windows;
using Dalamud.Interface;
using Dalamud.Utility;
using LiteDB;
using LiteHelper.Extensions;
using LiteHelper.Factory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using PlayerTrack.Models.Structs;

namespace PlayerTrack.Migration;

using System.Data;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.DrunkenToad.Helpers;
using Domain;
using LiteHelper.Exception;

public static class LiteDBMigrator
{
    private static MigrationWindow? migrationWindow;

    private static string ConfigDirectoryPath { get; set; } = null!;

    private static string BackupDirectoryPath { get; set; } = null!;

    private static string LegacyDataDirectoryPath { get; set; } = null!;

    private static string LegacyMetaFilePath { get; set; } = null!;

    private static string LegacyDatabaseFilePath { get; set; } = null!;

    private static string TempLegacyDatabaseFilePath { get; set; } = null!;

    private static Dictionary<string, int> PlayerKeyToIdMap { get; set; } = new();

    private static Dictionary<long, Encounter> EventIdToEncounterMap { get; set; } = new();

    private static Dictionary<int, int> CategoryLegacyIdToId { get; set; } = new();

    private static List<int> DefaultCategoryIds { get; set; } = new();

    private static Dictionary<string, Tag> TagTextToTags { get; set; } = new();

    private static HashSet<uint> WorldIds { get; set; } = new();

    private static HashSet<ushort> TerritoryTypeIds { get; set; } = new();

    public static void Dispose()
    {
        // ignored
    }

    public static bool Run()
    {
        try
        {
            SetupPaths();
            if (!IsLiteDB())
            {
                return true;
            }

            OpenMigrationWindow();
            PreparingGameData();
            CreateDirs();
            ArchiveJSONFormat();
            CopyLatestLiteDB();
            BackupLatestLiteDB();
            MoveAndCompressLiteDBBackups();
            if (!IsSchemaCompatible())
            {
                return true;
            }

            MigrateCategories();
            MigratePlayers();
            MigrateEncounters();
            FixCategoryRanks();
            ExterminateDefaultCategory();
            MigrateConfig();
            FinishMigration();
            return true;
        }
        catch (Exception ex)
        {
            LogError(ex, $"Migration failed.");
            migrationWindow?.StopMigration();
            return false;
        }
    }

    private static void ExterminateDefaultCategory()
    {
        try
        {
            var categories = RepositoryContext.CategoryRepository.GetAllCategories() ?? Array.Empty<Category>();
            if (DefaultCategoryIds.Count == 0)
            {
                LogWarning("No default category found, review your settings after.");
                return;
            }

            if (DefaultCategoryIds.Count > 1)
            {
                LogWarning("Found multiple default categories, review your settings after.");
                return;
            }

            var defaultCategoryId = DefaultCategoryIds.First();
            var defaultCategory = categories.SingleOrDefault(c => c.Id == defaultCategoryId);
            if (defaultCategory == null)
            {
                DalamudContext.PluginLog.Warning($"Failed to find default category with id {defaultCategoryId}.");
                return;
            }

            var defaultCategoryConfig = RepositoryContext.PlayerConfigRepository.GetPlayerConfigByCategoryId(defaultCategoryId);
            if (defaultCategoryConfig == null)
            {
                DalamudContext.PluginLog.Info($"No default config for {defaultCategoryId} so nothing to do.");
                return;
            }

            var defaultConfig = RepositoryContext.PlayerConfigRepository.GetDefaultPlayerConfig();
            if (defaultConfig == null)
            {
                throw new FileNotFoundException("Failed to get default config.");
            }

            defaultConfig.PlayerListIcon = defaultCategoryConfig.PlayerListIcon;
            defaultConfig.PlayerListNameColor = defaultCategoryConfig.PlayerListNameColor;
            defaultConfig.NameplateColor = defaultCategoryConfig.NameplateColor;
            defaultConfig.AlertProximity = defaultCategoryConfig.AlertProximity;
            defaultConfig.AlertNameChange = defaultCategoryConfig.AlertNameChange;
            defaultConfig.AlertWorldTransfer = defaultCategoryConfig.AlertWorldTransfer;
            defaultConfig.NameplateUseColor = defaultCategoryConfig.NameplateUseColor;

            RepositoryContext.PlayerConfigRepository.UpdatePlayerConfig(defaultConfig);
            ServiceContext.CategoryService.DeleteCategory(defaultCategory);

            Log("Removed default category and applied its settings to the default settings.");
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Warning($"Failed to exterminate default category: {ex.Message}");
        }
    }

    private static void FixCategoryRanks()
    {
        try
        {
            var categories = (RepositoryContext.CategoryRepository.GetAllCategories() ?? Array.Empty<Category>())
                .OrderBy(c => c.Rank)
                .ToList();

            var ranksHadIssues = false;

            for (var i = 0; i < categories.Count; i++)
            {
                if (categories[i].Rank != i)
                {
                    ranksHadIssues = true;
                    categories[i].Rank = i;
                    RepositoryContext.CategoryRepository.UpdateCategory(categories[i]);
                }
            }

            Log(ranksHadIssues ? "Found and fixed duplicate category ranks." : "Validated category ranks - no issues.");
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Warning("Failed to fix category ranks.", ex);
        }
    }

    private static void MigrateConfig()
    {
        try
        {
            var path = DalamudContext.PluginInterface.ConfigFile.FullName;
            var jsonText = File.ReadAllText(path);

            // save a copy just in case
            var configArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Config, Data = jsonText, };

            RepositoryContext.ArchiveRecordRepository.CreateArchiveRecord(configArchive);
            DalamudContext.PluginLog.Info($"Archived plugin config.");

            var jsonObject = JObject.Parse(jsonText);

            // get new plugin config
            var pluginConfig = RepositoryContext.ConfigRepository.GetPluginConfig();
            if (pluginConfig == null)
            {
                throw new FileNotFoundException("Failed to get plugin config.");
            }

            var enabledIcons = jsonObject["EnabledIcons"]?.ToObject<List<FontAwesomeIcon>>();
            if (enabledIcons != null)
            {
                pluginConfig.Icons = enabledIcons;
            }

            var lodestoneLocale = jsonObject.Value<int>("LodestoneLocale");
            if (lodestoneLocale == 2)
            {
                lodestoneLocale = 4; // expanded locale list
            }

            pluginConfig.LodestoneLocale = (LodestoneLocale)lodestoneLocale;

            pluginConfig.SearchType = (SearchType)jsonObject.Value<int>("SearchType");
            pluginConfig.LodestoneEnableLookup = jsonObject.Value<bool>("SyncToLodestone");
            pluginConfig.ShowOpenInPlayerTrack = jsonObject.Value<bool>("ShowAddShowInfoContextMenu");
            pluginConfig.ShowOpenLodestone = jsonObject.Value<bool>("ShowOpenLodestoneContextMenu");
            pluginConfig.ShowSearchBox = jsonObject.Value<bool>("ShowSearchBox");
            pluginConfig.IsWindowSizeLocked = jsonObject.Value<bool>("LockWindow");
            pluginConfig.IsWindowPositionLocked = jsonObject.Value<bool>("LockWindow");
            pluginConfig.IsWindowCombined = jsonObject.Value<bool>("CombinedPlayerDetailWindow");

            var restrictAddUpdatePlayers = jsonObject.Value<int>("RestrictAddUpdatePlayers");
            if (restrictAddUpdatePlayers == 0)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddPlayers = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddPlayers = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddPlayers = true;
            }
            else if (restrictAddUpdatePlayers == 1)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddPlayers = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddPlayers = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddPlayers = true;
            }
            else if (restrictAddUpdatePlayers == 2)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddPlayers = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddPlayers = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddPlayers = true;
            }
            else if (restrictAddUpdatePlayers == 3)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddPlayers = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddPlayers = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddPlayers = false;
            }

            var restrictAddEncounters = jsonObject.Value<int>("RestrictAddEncounters");
            if (restrictAddEncounters == 0)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddEncounters = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddEncounters = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddEncounters = true;
            }
            else if (restrictAddEncounters == 1)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddEncounters = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddEncounters = true;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddEncounters = true;
            }
            else if (restrictAddEncounters == 2)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddEncounters = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddEncounters = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddEncounters = true;
            }
            else if (restrictAddEncounters == 3)
            {
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Overworld).AddEncounters = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.Content).AddEncounters = false;
                pluginConfig.GetTrackingLocationConfig(ToadLocationType.HighEndContent).AddEncounters = false;
            }

            RepositoryContext.ConfigRepository.UpdatePluginConfig(pluginConfig);
            DalamudContext.PluginLog.Info($"Migrated plugin config.");

            // get new default player config
            var defaultConfig = RepositoryContext.PlayerConfigRepository.GetDefaultPlayerConfig();
            if (defaultConfig == null)
            {
                throw new FileNotFoundException("Failed to get default config.");
            }

            var useNamePlateColors = jsonObject.Value<bool>("UseNamePlateColors");
            defaultConfig.NameplateUseColor = new ConfigValue<bool>(InheritOverride.None, useNamePlateColors);

            var disableNamePlateColorIfDead = jsonObject.Value<bool>("DisableNamePlateColorIfDead");
            defaultConfig.NameplateUseColorIfDead = new ConfigValue<bool>(InheritOverride.None, disableNamePlateColorIfDead);

            RepositoryContext.PlayerConfigRepository.UpdatePlayerConfig(defaultConfig);
            DalamudContext.PluginLog.Info($"Migrated default player config.");
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to migrate config.");
            throw new FileLoadException("Failed to migrate config.", ex);
        }
    }

    private static void PreparingGameData()
    {
        WorldIds = DalamudContext.DataManager.Worlds.Select(world => world.Key).ToHashSet();
        TerritoryTypeIds = DalamudContext.DataManager.Locations.Select(loc => loc.Value.TerritoryId).ToHashSet();
    }

    private static void FinishMigration()
    {
        Log("Migration is complete! You may close this window.");
        try
        {
            ServiceContext.CategoryService.RefreshCategories();
            ServiceContext.TagService.RefreshTags();
            ServiceContext.PlayerDataService.RefreshAllPlayers();
            File.Delete(TempLegacyDatabaseFilePath);
            File.Delete(DalamudContext.PluginInterface.ConfigFile.FullName);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to delete legacy files, can delete manually later if you want.");
        }

        migrationWindow?.StopMigration();
    }

    private static LiteDatabaseFactory GetLiteDBFactory() => new(DalamudContext.PluginInterface.GetPluginConfigDirectory(), "litedb", "direct");

    private static void MapPlayerDirectFields(Player player, BsonDocument oldestPlayer)
    {
        player.Created = oldestPlayer.GetValueOrDefault<long>("Created");
        player.Updated = oldestPlayer.GetValueOrDefault<long>("Updated");
        player.Key = oldestPlayer.GetValueOrDefault<string>("Key");
        player.Name = oldestPlayer.GetValueOrDefault<List<string>>("Names").First();
        player.WorldId = oldestPlayer.GetValueOrDefault<List<KeyValuePair<uint, string>>>("HomeWorlds").First().Key;
        player.LastAlertSent = oldestPlayer.GetValueOrDefault<long>("SendNextAlert"); // close enough
        player.LastSeen = oldestPlayer.GetValueOrDefault<long>("Updated"); // new field
        player.Customize = oldestPlayer.GetValueOrDefault<byte[]?>("Customize");
        player.SeenCount = oldestPlayer.GetValueOrDefault<int>("SeenCount");
        player.Notes = oldestPlayer.GetValueOrDefault<string>("Notes");
        player.LodestoneId = oldestPlayer.GetValueOrDefault<uint>("LodestoneId");
        player.LastTerritoryType = oldestPlayer.GetValueOrDefault<ushort>("LastTerritoryType");
    }

    private static PlayerConfig? CreatePlayerLevelConfig(Player player, BsonDocument oldestPlayer)
    {
        var hasNonDefaultSetting = false;
        var playerConfig = new PlayerConfig(PlayerConfigType.Player) { PlayerId = player.Id };

        var visibilityType = 0;
        if (oldestPlayer.TryGetValue("VisibilityType", out var bsonValue))
        {
            visibilityType = (int)bsonValue.RawValue;
        }

        if (visibilityType > 0)
        {
            playerConfig.VisibilityType = new ConfigValue<VisibilityType>(InheritOverride.Override, (VisibilityType)visibilityType);
            hasNonDefaultSetting = true;
        }

        var icon = oldestPlayer.GetValueOrDefault<int>("Icon");
        if (icon != 0)
        {
            playerConfig.PlayerListIcon = new ConfigValue<char>(InheritOverride.Override, (char)icon);
            hasNonDefaultSetting = true;
        }

        var listColor = oldestPlayer.GetValueOrDefault<Vector4?>("ListColor");
        if (listColor != null)
        {
            var uiColor = DalamudContext.DataManager.FindClosestUIColor(listColor.Value).Id;
            playerConfig.PlayerListNameColor = new ConfigValue<uint>(InheritOverride.Override, uiColor);
            hasNonDefaultSetting = true;
        }

        var nameplateColor = oldestPlayer.GetValueOrDefault<Vector4?>("NamePlateColor");
        if (nameplateColor != null)
        {
            var uiColor = DalamudContext.DataManager.FindClosestUIColor(nameplateColor.Value).Id;
            playerConfig.NameplateUseColor = new ConfigValue<bool>(InheritOverride.Override, true);
            playerConfig.NameplateColor = new ConfigValue<uint>(InheritOverride.Override, uiColor);
            hasNonDefaultSetting = true;
        }

        var title = oldestPlayer.GetValueOrDefault<string>("Title");
        if (!string.IsNullOrEmpty(title))
        {
            playerConfig.NameplateTitleType = new ConfigValue<NameplateTitleType>(InheritOverride.Override, NameplateTitleType.CustomTitle);
            playerConfig.NameplateCustomTitle = new ConfigValue<string>(InheritOverride.Override, title);
            hasNonDefaultSetting = true;
        }

        var isAlertEnabled = oldestPlayer.GetValueOrDefault<bool>("IsAlertEnabled");
        if (isAlertEnabled)
        {
            playerConfig.AlertNameChange = new ConfigValue<bool>(InheritOverride.Override, true);
            playerConfig.AlertWorldTransfer = new ConfigValue<bool>(InheritOverride.Override, true);
            playerConfig.AlertProximity = new ConfigValue<bool>(InheritOverride.Override, true);
            hasNonDefaultSetting = true;
        }

        if (!hasNonDefaultSetting)
        {
            return null;
        }

        return playerConfig;
    }

    private static PlayerConfig? CreateCategoryLevelConfig(Category category, BsonDocument originalCategory)
    {
        var hasNonDefaultSetting = false;
        var playerConfig = new PlayerConfig(PlayerConfigType.Category) { CategoryId = category.Id };

        var icon = originalCategory.GetValueOrDefault<int>("Icon");
        if (icon != 0)
        {
            playerConfig.PlayerListIcon = new ConfigValue<char>(InheritOverride.Override, (char)icon);
            hasNonDefaultSetting = true;
        }

        var listColor = originalCategory.GetValueOrDefault<Vector4?>("ListColor");
        if (listColor != null)
        {
            var uiColor = DalamudContext.DataManager.FindClosestUIColor(listColor.Value).Id;
            playerConfig.PlayerListNameColor = new ConfigValue<uint>(InheritOverride.Override, uiColor);
            hasNonDefaultSetting = true;
        }

        var nameplateColor = originalCategory.GetValueOrDefault<Vector4?>("NamePlateColor");
        if (nameplateColor != null)
        {
            var uiColor = DalamudContext.DataManager.FindClosestUIColor(nameplateColor.Value).Id;
            playerConfig.NameplateUseColor = new ConfigValue<bool>(InheritOverride.Override, true);
            playerConfig.NameplateColor = new ConfigValue<uint>(InheritOverride.Override, uiColor);
            hasNonDefaultSetting = true;
        }

        var isAlertEnabled = originalCategory.GetValueOrDefault<bool>("IsAlertEnabled");
        if (isAlertEnabled)
        {
            playerConfig.AlertProximity = new ConfigValue<bool>(InheritOverride.Override, true);
            hasNonDefaultSetting = true;
        }

        var isNameChangeAlertEnabled = originalCategory.GetValueOrDefault<bool>("IsNameChangeAlertEnabled");
        if (isNameChangeAlertEnabled)
        {
            playerConfig.AlertNameChange = new ConfigValue<bool>(InheritOverride.Override, true);
            hasNonDefaultSetting = true;
        }

        var isWorldTransferAlertEnabled = originalCategory.GetValueOrDefault<bool>("IsWorldTransferAlertEnabled");
        if (isWorldTransferAlertEnabled)
        {
            playerConfig.AlertWorldTransfer = new ConfigValue<bool>(InheritOverride.Override, true);
            hasNonDefaultSetting = true;
        }

        var isNamePlateColorEnabled = originalCategory.GetValueOrDefault<bool>("IsNamePlateColorEnabled");
        if (isNamePlateColorEnabled)
        {
            playerConfig.NameplateUseColor = new ConfigValue<bool>(InheritOverride.Override, true);
            hasNonDefaultSetting = true;
        }

        var visibilityType = 0;
        if (originalCategory.TryGetValue("VisibilityType", out var bsonValue))
        {
            visibilityType = (int)bsonValue.RawValue;
        }

        if (visibilityType > 0)
        {
            playerConfig.VisibilityType = new ConfigValue<VisibilityType>(InheritOverride.Override, (VisibilityType)visibilityType);
            hasNonDefaultSetting = true;
        }

        if (!hasNonDefaultSetting)
        {
            return null;
        }

        return playerConfig;
    }

    private static void MapCategoryDirectFields(Category category, BsonDocument originalCategory)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
        category.Created = currentTime; // new field
        category.Updated = currentTime; // new field
        category.Name = originalCategory.GetValueOrDefault<string>("Name");
        category.Rank = originalCategory.GetValueOrDefault<int>("Rank");
    }

    private static ArchiveRecord CreateEncounterArchive(int encounterId, BsonDocument originalEncounter)
    {
        var migrationArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Encounter };
        dynamic data = new ExpandoObject();
        data.EncounterId = encounterId;
        data.LegacyId = originalEncounter.GetValueOrDefault<int>("_id");
        data.EventId = originalEncounter.GetValueOrDefault<long?>("EventId") ?? 0;
        data.Reason = "Unused";
        migrationArchive.Data = JsonConvert.SerializeObject(data);
        return migrationArchive;
    }

    private static ArchiveRecord CreateCategoryArchive(int newCategoryId, BsonDocument originalCategory)
    {
        var migrationArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Category };
        dynamic data = new ExpandoObject();
        data.CategoryId = newCategoryId;
        data.LegacyId = originalCategory.GetValueOrDefault<int>("_id");
        data.IsDefault = originalCategory.GetValueOrDefault<string>("IsDefault");
        data.OverrideFCNameColor = originalCategory.GetValueOrDefault<bool>("OverrideFCNameColor");
        data.FCLodestoneId = originalCategory.GetValueOrDefault<string>("FCLodestoneId");
        data.Reason = "Unused";
        var listColor = originalCategory.GetValueOrDefault<Vector4?>("ListColor");
        if (listColor != null)
        {
            data.ListColor = listColor; // backup since we converted to UIColor
        }

        var nameplateColor = originalCategory.GetValueOrDefault<Vector4?>("NamePlateColor");
        if (nameplateColor != null)
        {
            data.NameplateColor = nameplateColor; // backup since we converted to UIColor
        }

        migrationArchive.Data = JsonConvert.SerializeObject(data);
        return migrationArchive;
    }

    private static ArchiveRecord CreatePlayerArchive(int newPlayerId, BsonDocument oldestPlayer)
    {
        var migrationArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Player };
        dynamic data = new ExpandoObject();
        data.PlayerId = newPlayerId;
        data.LegacyId = oldestPlayer.GetValueOrDefault<int>("_id");
        data.OverrideFCNameColor = oldestPlayer.GetValueOrDefault<bool>("OverrideFCNameColor");
        data.Reason = "Unused";
        var listColor = oldestPlayer.GetValueOrDefault<Vector4?>("ListColor");
        if (listColor != null)
        {
            data.ListColor = listColor; // backup since we converted to UIColor
        }

        var nameplateColor = oldestPlayer.GetValueOrDefault<Vector4?>("NamePlateColor");
        if (nameplateColor != null)
        {
            data.NameplateColor = nameplateColor; // backup since we converted to UIColor
        }

        migrationArchive.Data = JsonConvert.SerializeObject(data);
        return migrationArchive;
    }

    private static ArchiveRecord CreatePlayerArchive(BsonDocument oldestPlayer)
    {
        var migrationArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Player };
        dynamic data = new ExpandoObject();
        data.LegacyId = oldestPlayer.GetValueOrDefault<int>("_id");
        data.Key = oldestPlayer.GetValueOrDefault<string>("Key");
        data.Reason = "Invalid";
        data.Bson = oldestPlayer.ToDebugString();
        migrationArchive.Data = JsonConvert.SerializeObject(data);
        return migrationArchive;
    }

    private static void MapEncounterDirectFields(Encounter encounter, BsonDocument originalEncounter)
    {
        encounter.TerritoryTypeId = originalEncounter.GetValueOrDefault<ushort>("TerritoryType");
        encounter.Created = originalEncounter.GetValueOrDefault<long>("Created");
        encounter.Updated = originalEncounter.GetValueOrDefault<long>("Updated");
        encounter.Ended = encounter.Updated; // new field
    }

    private static void MapPlayerLodestone(Player player, BsonDocument oldestPlayer)
    {
        var lodestoneStatus = oldestPlayer.GetValueOrDefault<int>("LodestoneStatus");
        var lodestoneFailureCount = oldestPlayer.GetValueOrDefault<int>("LodestoneFailureCount");
        var hasLodestoneId = player.LodestoneId != 0;

        player.LodestoneStatus = lodestoneStatus switch
        {
            2 when hasLodestoneId => LodestoneStatus.Verified,
            5 when lodestoneFailureCount > 2 => LodestoneStatus.Banned,
            0 or 1 or 3 or 4 => LodestoneStatus.Unverified,
            _ => LodestoneStatus.Unverified,
        };

        if (hasLodestoneId)
        {
            player.LodestoneVerifiedOn = oldestPlayer.GetValueOrDefault<long>("LodestoneLastUpdated");
        }
    }

    private static void MapPlayerFreeCompany(Player player, BsonDocument oldestPlayer)
    {
        var fc = oldestPlayer.GetValueOrDefault<string>("FreeCompany");
        FreeCompanyState state;
        string companyTag;

        if (fc.Equals("N/A", StringComparison.Ordinal) || string.IsNullOrEmpty(fc))
        {
            state = FreeCompanyState.Unknown;
            companyTag = string.Empty;
        }
        else if (fc.Equals("None", StringComparison.Ordinal))
        {
            state = FreeCompanyState.NotInFC;
            companyTag = string.Empty;
        }
        else
        {
            state = FreeCompanyState.InFC;
            companyTag = fc;
        }

        player.FreeCompany = new KeyValuePair<FreeCompanyState, string>(state, companyTag);
    }

    private static void ExtractNewTags(BsonDocument oldestPlayer)
    {
        var tags = oldestPlayer.GetValueOrDefault<List<string>?>("Tags");
        if (tags == null || tags.Count == 0)
        {
            return;
        }

        foreach (var tagText in tags)
        {
            if (string.IsNullOrEmpty(tagText))
            {
                continue;
            }

            if (TagTextToTags.ContainsKey(tagText.ToLowerInvariant()))
            {
                continue;
            }

            var currentTime = UnixTimestampHelper.CurrentTime();
            TagTextToTags.Add(
                tagText.ToLowerInvariant(),
                new Tag
            {
                Id = TagTextToTags.Count + 1,
                Name = tagText,
                Created = currentTime,
                Updated = currentTime,
                Color = DalamudContext.DataManager.GetRandomUIColor().Id,
            });
        }
    }

    private static void MigrateCategories()
    {
        BsonDocument? lastCategory = null;
        var categoryCount = 0;
        try
        {
            using var dbFactory = GetLiteDBFactory();
            var categories = new List<Category>();
            var playerConfigs = new List<PlayerConfig>();
            var categoryArchives = new List<ArchiveRecord>();
            ILiteCollection<BsonDocument> originalCategoryCollection = dbFactory.Database.GetCollection("Category");
            if (originalCategoryCollection != null)
            {
                var originalCategories = originalCategoryCollection.FindAll().ToList();
                foreach (var originalCategory in originalCategories)
                {
                    lastCategory = originalCategory;
                    var category = new Category
                    {
                        Id = categoryCount + 1, // start at 1 and increment
                    };

                    MapCategoryDirectFields(category, originalCategory);

                    categories.Add(category);
                    CategoryLegacyIdToId.Add(originalCategory.GetValueOrDefault<int>("_id"), category.Id);

                    if (originalCategory.GetValueOrDefault<string>("IsDefault") == "True")
                    {
                        DefaultCategoryIds.Add(category.Id);
                    }

                    var playerConfig = CreateCategoryLevelConfig(category, originalCategory);
                    if (playerConfig != null)
                    {
                        playerConfigs.Add(playerConfig);
                    }

                    var migrationArchive = CreateCategoryArchive(category.Id, originalCategory);
                    categoryArchives.Add(migrationArchive);

                    categoryCount++;
                }
            }

            var savedCategories = RepositoryContext.CategoryRepository.SaveCategories(categories);
            if (!savedCategories)
            {
                throw new DataException("Failed to insert batch categories.");
            }

            Log($"Migrated {categoryCount:N0} categories.");
            var savedPlayerConfigs = RepositoryContext.PlayerConfigRepository.CreatePlayerConfigs(playerConfigs);
            if (!savedPlayerConfigs)
            {
                throw new DataException("Failed to insert batch player configs.");
            }

            Log($"Migrated {playerConfigs.Count:N0} category player configs.");
            var savedCategoryArchives = RepositoryContext.ArchiveRecordRepository.CreateArchiveRecords(categoryArchives);
            if (!savedCategoryArchives)
            {
                throw new DataException("Failed to insert batch category archives.");
            }

            DalamudContext.PluginLog.Info($"Migrated {categoryArchives.Count:N0} archived category data records.");
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to migrate categories. {Environment.NewLine}Failed on: {lastCategory?.ToDebugString()}");
            migrationWindow?.StopMigration();
            throw new DataException("Failed to migrate categories.", ex);
        }
    }

    private static void MigrateEncounters()
    {
        BsonDocument? lastEncounter = null;
        try
        {
            using var dbFactory = GetLiteDBFactory();
            ILiteCollection<BsonDocument> originalEncounterCollection = dbFactory.Database.GetCollection("Encounter");
            if (originalEncounterCollection != null)
            {
                var encounters = new List<Encounter>();
                var playerEncounters = new List<PlayerEncounter>();
                var encounterArchives = new List<ArchiveRecord>();
                var originalEncounters = originalEncounterCollection.FindAll().ToList();
                var invalidEncounterCount = 0;
                var invalidPlayerEncounterCount = 0;
                foreach (var originalEncounter in originalEncounters)
                {
                    lastEncounter = originalEncounter;
                    var eventId = originalEncounter.GetValueOrDefault<long?>("EventId");
                    var territoryType = originalEncounter.GetValueOrDefault<ushort>("TerritoryType");
                    if (eventId == null || eventId == 0 || !EventIdToEncounterMap.ContainsKey(eventId.Value))
                    {
                        // check if valid
                        if (!IsValidPlayer("PlayerKey", originalEncounter) || !TerritoryTypeIds.Contains(territoryType))
                        {
                            var invalidMigrationArchive = CreateEncounterArchive(originalEncounter);
                            encounterArchives.Add(invalidMigrationArchive);
                            DalamudContext.PluginLog.Info($"Skipping invalid encounter: {originalEncounter.ToDebugString()}");
                            invalidEncounterCount++;
                            continue;
                        }

                        // create new encounter and player encounter
                        var encounter = new Encounter
                        {
                            Id = encounters.Count + 1, // start at 1 and increment
                        };

                        MapEncounterDirectFields(encounter, originalEncounter);
                        encounters.Add(encounter);

                        var migrationArchive = CreateEncounterArchive(encounter.Id, originalEncounter);
                        encounterArchives.Add(migrationArchive);

                        var playerEncounter = CreatePlayerEncounter(encounter, originalEncounter);
                        if (playerEncounter != null)
                        {
                            playerEncounters.Add(playerEncounter);
                        }
                        else
                        {
                            invalidPlayerEncounterCount++;
                        }

                        if (eventId != null && eventId != 0)
                        {
                            // save event id since can reuse unlike legacy 1-to-1 scenarios
                            EventIdToEncounterMap.TryAdd(eventId.Value, encounter);
                        }
                    }
                    else if (EventIdToEncounterMap.TryGetValue(eventId.Value, out var encounter))
                    {
                        // check if valid
                        if (!IsValidPlayer("PlayerKey", originalEncounter) || !TerritoryTypeIds.Contains(territoryType))
                        {
                            var invalidMigrationArchive = CreateEncounterArchive(originalEncounter);
                            encounterArchives.Add(invalidMigrationArchive);
                            DalamudContext.PluginLog.Info($"Skipping invalid encounter: {originalEncounter.ToDebugString()}");
                            invalidEncounterCount++;
                            continue;
                        }

                        // use existing encounter but new player encounter
                        if (encounter == null)
                        {
                            throw new DataException($"Failed to get encounter with event id {eventId.Value}.");
                        }

                        var playerEncounter = CreatePlayerEncounter(encounter, originalEncounter);
                        if (playerEncounter != null)
                        {
                            playerEncounters.Add(playerEncounter);
                        }
                        else
                        {
                            invalidPlayerEncounterCount++;
                        }
                    }
                }

                var savedEncounters = RepositoryContext.EncounterRepository.CreateEncounters(encounters);
                if (!savedEncounters)
                {
                    throw new DataException("Failed to insert batch encounters.");
                }

                Log($"Migrated {encounters.Count:N0} encounters.");
                var savedPlayerEncounters = RepositoryContext.PlayerEncounterRepository.CreatePlayerEncounters(playerEncounters);
                if (!savedPlayerEncounters)
                {
                    throw new DataException("Failed to insert batch player encounters.");
                }

                Log($"Migrated {playerEncounters.Count:N0} player encounters.");
                var savedEncounterArchives = RepositoryContext.ArchiveRecordRepository.CreateArchiveRecords(encounterArchives);
                if (!savedEncounterArchives)
                {
                    throw new DataException("Failed to insert batch encounter archives.");
                }

                Log($"Skipped {invalidPlayerEncounterCount:N0} invalid player encounters.");

                DalamudContext.PluginLog.Info($"Migrated {encounterArchives.Count:N0} archived encounter data records.");
                Log($"Skipped {invalidEncounterCount:N0} invalid encounters.");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to migrate encounters. {Environment.NewLine}Failed on: {lastEncounter?.ToDebugString()}");
            migrationWindow?.StopMigration();
            throw new DataException("Failed to migrate encounters.", ex);
        }
    }

    private static ArchiveRecord CreateEncounterArchive(BsonDocument originalEncounter)
    {
        var migrationArchive = new ArchiveRecord { ArchiveType = ArchiveType.MigrationToV3Encounter };
        dynamic data = new ExpandoObject();
        data.LegacyId = originalEncounter.GetValueOrDefault<int>("_id");
        data.Key = originalEncounter.GetValueOrDefault<string>("PlayerKey");
        data.TerritoryType = originalEncounter.GetValueOrDefault<ushort>("TerritoryType");
        data.Reason = "Invalid";
        data.Bson = originalEncounter.ToDebugString();
        migrationArchive.Data = JsonConvert.SerializeObject(data);
        return migrationArchive;
    }

    private static void MigratePlayers()
    {
        BsonDocument? lastPlayer = null;
        var playerCount = 0;
        var invalidPlayerCount = 0;
        try
        {
            using var dbFactory = GetLiteDBFactory();
            ILiteCollection<BsonDocument> oldestPlayerCollection = dbFactory.Database.GetCollection("Player");
            if (oldestPlayerCollection != null)
            {
                var players = new List<Player>();
                var playerConfigs = new List<PlayerConfig>();
                var playerCategories = new List<PlayerCategory>();
                var playerArchives = new List<ArchiveRecord>();
                var playerTags = new List<PlayerTag>();
                var playerCustomizeHistoriesList = new List<PlayerCustomizeHistory>();
                var playerNameWorldHistoriesList = new List<PlayerNameWorldHistory>();
                var oldestPlayers = oldestPlayerCollection.FindAll().ToList();
                var invalidCategories = new HashSet<int>();
                var skippedPlayerCategoryCount = 0;
                var dupePlayerKeyCount = 0;
                foreach (var oldestPlayer in oldestPlayers)
                {
                    lastPlayer = oldestPlayer;
                    if (!IsValidPlayer("Key", oldestPlayer))
                    {
                        var migrationArchive = CreatePlayerArchive(oldestPlayer);
                        playerArchives.Add(migrationArchive);
                        DalamudContext.PluginLog.Info($"Skipping invalid player: {oldestPlayer.ToDebugString()}");
                        invalidPlayerCount++;
                        continue;
                    }

                    var player = new Player
                    {
                        Id = playerCount + 1, // start at 1 and increment
                    };
                    MapPlayerDirectFields(player, oldestPlayer);
                    MapPlayerLodestone(player, oldestPlayer);
                    MapPlayerFreeCompany(player, oldestPlayer);
                    players.Add(player);

                    var playerConfig = CreatePlayerLevelConfig(player, oldestPlayer);
                    if (playerConfig != null)
                    {
                        playerConfigs.Add(playerConfig);
                    }

                    var playerArchive = CreatePlayerArchive(player.Id, oldestPlayer);
                    playerArchives.Add(playerArchive);

                    var playerCategory = CreatePlayerCategory(player, oldestPlayer);
                    if (playerCategory != null)
                    {
                        playerCategories.Add(playerCategory);
                    }
                    else
                    {
                        skippedPlayerCategoryCount++;
                        invalidCategories.Add(oldestPlayer.GetValueOrDefault<int>("CategoryId"));
                    }

                    ExtractNewTags(oldestPlayer);
                    var playerTagsForPlayer = CreatePlayerTags(player, oldestPlayer);
                    playerTags.AddRange(playerTagsForPlayer);

                    var playerCustomizeHistories = CreateCustomizeHistory(player, oldestPlayer);
                    playerCustomizeHistoriesList.AddRange(playerCustomizeHistories);

                    var nameWorldHistories = CreateNameWorldHistory(player, oldestPlayer);
                    playerNameWorldHistoriesList.AddRange(nameWorldHistories);

                    if (!PlayerKeyToIdMap.ContainsKey(player.Key))
                    {
                        PlayerKeyToIdMap.Add(player.Key, player.Id); // save for encounters
                    }
                    else
                    {
                        DalamudContext.PluginLog.Warning($"Found duplicate player, so skipping {player.Key}");
                        dupePlayerKeyCount++;
                    }

                    playerCount++;
                }

                if (dupePlayerKeyCount > 0)
                {
                    LogWarning($"Skipped {dupePlayerKeyCount:N0} duplicate player keys.");
                }

                if (skippedPlayerCategoryCount > 0)
                {
                    LogWarning($"Skipped assigning {skippedPlayerCategoryCount:N0} players to {invalidCategories.Count:N0} invalid categories.");
                }

                var failedPlayerCount = 0;
                foreach (var player in players)
                {
                    var id = RepositoryContext.PlayerRepository.CreateExistingPlayer(player);

                    // check if id is 0, if so, failed to insert
                    if (id != 0)
                    {
                        // ensure id matches since it's used for other inserts
                        if (id != player.Id)
                        {
                            throw new DataException($"Player ID mismatch, failing out on {player.Key}. Expected {player.Id}; got {id}.");
                        }

                        continue;
                    }

                    playerConfigs.RemoveAll(config => config.PlayerId == player.Id);
                    playerCategories.RemoveAll(category => category.PlayerId == player.Id);
                    playerTags.RemoveAll(tag => tag.PlayerId == player.Id);
                    playerCustomizeHistoriesList.RemoveAll(history => history.PlayerId == player.Id);
                    playerNameWorldHistoriesList.RemoveAll(history => history.PlayerId == player.Id);
                    failedPlayerCount++;
                    LogWarning($"Failed to migrate player {player.Key}.");
                }

                if (failedPlayerCount > 10)
                {
                    LogError($"Failed to insert over 10 players: {failedPlayerCount}");
                    throw new DataException($"Failed to insert over 10 players: {failedPlayerCount}");
                }

                Log($"Migrated {players.Count:N0} players.");
                var savedPlayerConfigs = RepositoryContext.PlayerConfigRepository.CreatePlayerConfigs(playerConfigs);
                if (!savedPlayerConfigs)
                {
                    throw new DataException("Failed to insert batch player configs.");
                }

                Log($"Migrated {playerConfigs.Count:N0} player configs.");
                var savedPlayerCategories = RepositoryContext.PlayerCategoryRepository.CreatePlayerCategories(playerCategories);
                if (!savedPlayerCategories)
                {
                    throw new DataException("Failed to insert batch player categories.");
                }

                Log($"Migrated {playerCategories.Count:N0} player categories.");
                var savedTags = RepositoryContext.TagRepository.CreateTags(TagTextToTags.Values.ToList());
                if (!savedTags)
                {
                    throw new DataException("Failed to insert batch tags.");
                }

                Log($"Migrated {TagTextToTags.Count:N0} tags.");
                var savedPlayerTags = RepositoryContext.PlayerTagRepository.CreatePlayerTags(playerTags);
                if (!savedPlayerTags)
                {
                    throw new DataException("Failed to insert batch player tags.");
                }

                Log($"Migrated {playerTags.Count:N0} player tags.");
                var savedCustomizeHistory = RepositoryContext.PlayerCustomizeHistoryRepository.CreatePlayerCustomizeHistories(playerCustomizeHistoriesList);
                if (!savedCustomizeHistory)
                {
                    throw new DataException("Failed to insert batch player customize histories.");
                }

                Log($"Migrated {playerCustomizeHistoriesList.Count:N0} player customize histories.");
                var savedNameWorldHistory = RepositoryContext.PlayerNameWorldHistoryRepository.CreatePlayerNameWorldHistories(playerNameWorldHistoriesList);
                if (!savedNameWorldHistory)
                {
                    throw new DataException("Failed to insert batch player name world histories.");
                }

                Log($"Migrated {playerNameWorldHistoriesList.Count:N0} player name/world histories.");
                var savedPlayerArchives = RepositoryContext.ArchiveRecordRepository.CreateArchiveRecords(playerArchives);
                if (!savedPlayerArchives)
                {
                    throw new DataException("Failed to insert batch player archives.");
                }

                DalamudContext.PluginLog.Info($"Created {playerArchives.Count:N0} archived player data records.");
                Log($"Skipped {invalidPlayerCount:N0} invalid players.");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, $"Failed to migrate players. {Environment.NewLine}Failed on: {lastPlayer?.ToDebugString()}");
            migrationWindow?.StopMigration();
            throw new DataException("Failed to migrate players.", ex);
        }
    }

    private static IEnumerable<PlayerNameWorldHistory> CreateNameWorldHistory(Player player, BsonDocument oldestPlayer)
    {
        var nameWorldHistories = new List<PlayerNameWorldHistory>();
        var names = oldestPlayer.GetValueOrDefault<List<string>>("Names").Skip(1).ToList();
        var worldIds = oldestPlayer.GetValueOrDefault<List<KeyValuePair<uint, string>>>("HomeWorlds").Skip(1).ToList();

        if (names.Any())
        {
            names.Reverse(); // reverse to get oldest first
            foreach (var name in names)
            {
                if (!name.IsValidCharacterName())
                {
                    DalamudContext.PluginLog.Info($"Skipping invalid previous name: {name}");
                    continue;
                }

                var nameWorldHistory = new PlayerNameWorldHistory
                {
                    Created = player.Created,
                    Updated = player.Updated,
                    PlayerId = player.Id,
                    PlayerName = name,
                    WorldId = 0, // use 0 for unknown
                    IsMigrated = true,
                };
                nameWorldHistories.Add(nameWorldHistory);
            }
        }

        if (worldIds.Any())
        {
            worldIds.Reverse(); // reverse to get oldest first
            foreach (var worldId in worldIds)
            {
                if (!WorldIds.Contains(worldId.Key))
                {
                    DalamudContext.PluginLog.Info($"Skipping invalid previous world id: {worldId.Key}");
                    continue;
                }

                var nameWorldHistory = new PlayerNameWorldHistory
                {
                    Created = player.Created,
                    Updated = player.Updated,
                    PlayerId = player.Id,
                    PlayerName = string.Empty, // use empty for unknown
                    WorldId = worldId.Key,
                    IsMigrated = true,
                };
                nameWorldHistories.Add(nameWorldHistory);
            }
        }

        return nameWorldHistories;
    }

    private static IEnumerable<PlayerCustomizeHistory> CreateCustomizeHistory(Player player, BsonDocument oldestPlayer)
    {
        var customizeHistory = new List<PlayerCustomizeHistory>();
        var customizeArrays = oldestPlayer.GetValueOrDefault<List<byte[]?>?>("CustomizeHistory");
        if (customizeArrays == null)
        {
            return customizeHistory;
        }

        customizeArrays.Reverse(); // reverse to get oldest first
        foreach (var customizeArray in customizeArrays)
        {
            if (customizeArray == null)
            {
                continue;
            }

            var customize = new PlayerCustomizeHistory
            {
                Created = player.Created, Updated = player.Updated, PlayerId = player.Id, Customize = customizeArray,
            };
            customizeHistory.Add(customize);
        }

        return customizeHistory;
    }

    private static Tuple<bool, string, uint> ExtractFromKey(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                return Tuple.Create(false, string.Empty, 0U);
            }

            var lastIndex = key.LastIndexOf('_');
            if (lastIndex == -1 || lastIndex == key.Length - 1)
            {
                return Tuple.Create(false, string.Empty, 0U);
            }

            var name = key[..lastIndex].Replace('_', ' ');
            var worldIdString = key[(lastIndex + 1)..];

            if (uint.TryParse(worldIdString, out var worldId) && name.Split(' ').Length > 1)
            {
                return Tuple.Create(true, name, worldId);
            }

            return Tuple.Create(false, string.Empty, 0U);
        }
        catch
        {
            return Tuple.Create(false, string.Empty, 0U);
        }
    }

    private static bool IsValidPlayer(string keyField, BsonDocument doc)
    {
        var key = doc.GetValueOrDefault<string>(keyField);
        var (isValid, name, worldId) = ExtractFromKey(key);
        if (!isValid)
        {
            return false;
        }

        if (!name.IsValidCharacterName())
        {
            return false;
        }

        if (!WorldIds.Contains(worldId))
        {
            return false;
        }

        return true;
    }

    private static PlayerCategory? CreatePlayerCategory(Player player, BsonDocument oldestPlayer)
    {
        var oldCategoryId = oldestPlayer.GetValueOrDefault<int>("CategoryId");
        if (CategoryLegacyIdToId.TryGetValue(oldCategoryId, out var value))
        {
            return new PlayerCategory
            {
                Created = player.Created,
                Updated = player.Updated,
                PlayerId = player.Id,
                CategoryId = value,
            };
        }

        DalamudContext.PluginLog.Info($"Skipping invalid category id: {oldCategoryId}, so won't create player category.");
        return null;
    }

    private static IEnumerable<PlayerTag> CreatePlayerTags(Player player, BsonDocument oldestPlayer)
    {
        var playerTags = new List<PlayerTag>();
        var tagTexts = oldestPlayer.GetValueOrDefault<List<string>?>("Tags");
        if (tagTexts == null || tagTexts.Count == 0)
        {
            return playerTags;
        }

        foreach (var tagText in tagTexts)
        {
            if (string.IsNullOrEmpty(tagText))
            {
                continue;
            }

            var tag = TagTextToTags.TryGetValue(tagText.ToLowerInvariant(), out var tagOut) ? tagOut : null;
            if (tag == null)
            {
                continue;
            }

            var currentTime = UnixTimestampHelper.CurrentTime();
            playerTags.Add(new PlayerTag { Created = currentTime, Updated = currentTime, PlayerId = player.Id, TagId = tag.Id });
        }

        return playerTags;
    }

    private static PlayerEncounter? CreatePlayerEncounter(Encounter encounter, BsonDocument originalEncounter)
    {
        var playerKey = originalEncounter.GetValueOrDefault<string>("PlayerKey");
        if (!PlayerKeyToIdMap.ContainsKey(playerKey))
        {
            DalamudContext.PluginLog.Info($"Skipping invalid player key: {playerKey}, so won't create encounter.");
            return null;
        }

        return new PlayerEncounter
        {
            Created = encounter.Created,
            Updated = encounter.Updated,
            PlayerId = PlayerKeyToIdMap[playerKey],
            EncounterId = encounter.Id,
            JobId = originalEncounter.GetValueOrDefault<uint>("JobId"),
            JobLvl = originalEncounter.GetValueOrDefault<byte>("JobLvl"),
            Ended = encounter.Ended,
        };
    }

    private static bool IsSchemaCompatible()
    {
        int currentVersion;
        try
        {
            using var dbFactory = GetLiteDBFactory();
            currentVersion = dbFactory.Database.UserVersion;
        }
        catch (Exception ex)
        {
            LogError(ex, $"Could not open LiteDB database.");
            migrationWindow?.StopMigration();
            throw new DatabaseAccessException("Could not open LiteDB database.", ex);
        }

        switch (currentVersion)
        {
            case 0:
                LogWarning("LiteDB schema version is missing. Will try to migrate anyway.");
                return true;
            case < 3:
                LogWarning($"LiteDB schema version v{currentVersion} is too old. Will try to migrate anyway.");
                return true;
            default:
                Log($"Detected LiteDB schema v{currentVersion}.");
                return true;
        }
    }

    private static bool IsLiteDB()
    {
        if (File.Exists(LegacyDatabaseFilePath))
        {
            DalamudContext.PluginLog.Info("Detected LiteDB file, will need to migrate.");
            return true;
        }

        return false;
    }

    private static void SetupPaths()
    {
        ConfigDirectoryPath = DalamudContext.PluginInterface.GetPluginConfigDirectory();
        BackupDirectoryPath = DalamudContext.PluginInterface.PluginBackupDirectory();
        LegacyDataDirectoryPath = Path.Combine(ConfigDirectoryPath, "data");
        LegacyDatabaseFilePath = Path.Combine(LegacyDataDirectoryPath, "data.db");
        LegacyMetaFilePath = Path.Combine(LegacyDataDirectoryPath, "data.meta");
        TempLegacyDatabaseFilePath = Path.Combine(ConfigDirectoryPath, "litedb.db");
    }

    private static void CreateDirs()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectoryPath);
            Directory.CreateDirectory(BackupDirectoryPath);
        }
        catch (Exception ex)
        {
            LogError(ex, "Could not create directories for migration.");
            migrationWindow?.StopMigration();
            throw new FileLoadException("Could not create directories for migration.", ex);
        }
    }

    private static void ArchiveJSONFormat()
    {
        try
        {
            if (File.Exists(LegacyMetaFilePath))
            {
                Log("Found old JSON format backup files.");
                var zipName = $"JSON_{UnixTimestampHelper.CurrentTime()}.zip";
                CopyLegacyConfigFile(LegacyDataDirectoryPath);
                FileHelper.MoveAndCompressFiles(
                    LegacyDataDirectoryPath,
                    new List<string> { "data.meta", "players.dat", "data.meta", },
                    BackupDirectoryPath,
                    zipName);
                Log("Compressed and backed up old JSON files.");
            }
            else
            {
                Log("No old JSON format backup files found.");
            }
        }
        catch (Exception ex)
        {
            LogError(ex, "Could not archive old JSON format backup files.");
            migrationWindow?.StopMigration();
            throw new FileLoadException("Could not archive old JSON format backup files.", ex);
        }
    }

    private static void CopyLatestLiteDB()
    {
        try
        {
            Log($"Moving latest DB to pluginConfigs\\PlayerTrack\\litedb.db for migration.");
            File.Copy(LegacyDatabaseFilePath, Path.Combine(TempLegacyDatabaseFilePath), true);
        }
        catch (Exception ex)
        {
            LogError(ex, $"Could not move latest DB to pluginConfigs\\PlayerTrack\\litedb.db for migration.");
            migrationWindow?.StopMigration();
            throw new FileLoadException($"Could not move latest DB to pluginConfigs\\PlayerTrack\\litedb.db for migration.", ex);
        }
    }

    private static void BackupLatestLiteDB()
    {
        try
        {
            const string backupName = "UPGRADE_V2_FINAL";
            var backupSubDirPath = Path.Combine(BackupDirectoryPath, backupName);
            var backupFilePath = Path.Combine(backupSubDirPath, "litedb.db");
            Log($"Backing up latest DB to backup directory.");
            Directory.CreateDirectory(backupSubDirPath);
            if (File.Exists(backupFilePath))
            {
                LogWarning($"Backup file already exists at {backupFilePath}. Will add timestamp to file name.");
                backupFilePath = Path.Combine(backupSubDirPath, $"litedb_{UnixTimestampHelper.CurrentTime()}.db");
            }

            File.Copy(LegacyDatabaseFilePath, backupFilePath);
            CopyLegacyConfigFile(backupSubDirPath);
            DeleteOldLocFiles();
            FileHelper.MoveAndCompressDirectory(backupSubDirPath, BackupDirectoryPath, $"{backupName}.zip");
        }
        catch (Exception ex)
        {
            LogError(ex, "Could not backup latest DB to backup directory.");
            migrationWindow?.StopMigration();
            throw new FileLoadException("Could not backup latest to DB to backup directory.", ex);
        }
    }

    private static void MoveAndCompressLiteDBBackups()
    {
        Log($"Moving and compressing old backups to new backup directory.");
        CleanupOldBackupDirectory(Path.Combine(LegacyDataDirectoryPath, "upgrade"), "UPGRADE");
        CleanupOldBackupDirectory(LegacyDataDirectoryPath, "AUTOMATIC");
    }

    private static void CleanupOldBackupDirectory(string sourceDirectory, string prefix)
    {
        try
        {
            if (Directory.Exists(sourceDirectory))
            {
                var subdirectories = Directory.GetDirectories(sourceDirectory);
                foreach (var subdirectory in subdirectories)
                {
                    var subDirPath = Path.GetFullPath(subdirectory);
                    var subDirName = Path.GetFileName(subdirectory);
                    var outputZipFileName = $"{prefix}_{subDirName}.zip";
                    CopyLegacyConfigFile(subDirPath);
                    FileHelper.MoveAndCompressDirectory(subDirPath, BackupDirectoryPath, outputZipFileName);
                }

                Directory.Delete(sourceDirectory, true);
            }
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, $"Could not cleanup old backup directory {sourceDirectory}.");
            if (ex.InnerException != null)
            {
                DalamudContext.PluginLog.Error(ex.InnerException, $"Could not cleanup old backup directory {sourceDirectory}.");
            }

            LogWarning($"Failed to cleanup old backup directory {prefix}.");
        }
    }

    private static void OpenMigrationWindow()
    {
        migrationWindow = new MigrationWindow(DalamudContext.PluginInterface);
        Log("Starting migration.");
    }

    private static void Log(string msg)
    {
        DalamudContext.PluginLog.Info(msg);
        migrationWindow?.LogInfo(msg);
    }

    private static void LogWarning(string msg)
    {
        DalamudContext.PluginLog.Info(msg);
        migrationWindow?.LogWarning(msg);
    }

    private static void LogError(Exception ex, string msg)
    {
        DalamudContext.PluginLog.Error(ex, msg);
        migrationWindow?.LogError(msg);
        migrationWindow?.LogError(ex.Message);
        migrationWindow?.LogError(ex.StackTrace ?? "No stack trace");
    }

    private static void LogError(string msg)
    {
        DalamudContext.PluginLog.Error(msg);
        migrationWindow?.LogError(msg);
    }

    private static void CopyLegacyConfigFile(string destDir)
    {
        var destPath = Path.Combine(destDir, "PlayerTrack.json");
        if (!File.Exists(destPath))
        {
            File.Copy(DalamudContext.PluginInterface.ConfigFile.FullName, destPath);
        }
    }

    private static void DeleteOldLocFiles()
    {
        try
        {
            var locPath = Path.Combine(DalamudContext.PluginInterface.GetPluginConfigDirectory(), "loc");
            if (Directory.Exists(locPath))
            {
                Directory.Delete(locPath, true);
                DalamudContext.PluginLog.Info($"Deleted old loc directory.");
            }

            var versionPath = Path.Combine(DalamudContext.PluginInterface.GetPluginConfigDirectory(), "version");
            if (File.Exists(versionPath))
            {
                File.Delete(versionPath);
            }
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Warning($"Failed to delete old loc files. {ex.Message}");
        }
    }
}
