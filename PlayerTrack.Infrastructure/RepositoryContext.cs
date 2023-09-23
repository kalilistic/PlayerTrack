using System;
using System.Data;
using AutoMapper;
using FluentDapperLite.Extension;

namespace PlayerTrack.Infrastructure;

using Dalamud.DrunkenToad.Helpers;
using Dalamud.Logging;
using FluentDapperLite.Maintenance;

public static class RepositoryContext
{
    public static BackupRepository BackupRepository { get; set; } = null!;

    public static CategoryRepository CategoryRepository { get; set; } = null!;

    public static EncounterRepository EncounterRepository { get; set; } = null!;

    public static LodestoneLookupRepository LodestoneRepository { get; set; } = null!;

    public static PlayerRepository PlayerRepository { get; set; } = null!;

    public static ConfigRepository ConfigRepository { get; set; } = null!;

    public static TagRepository TagRepository { get; set; } = null!;

    public static PlayerEncounterRepository PlayerEncounterRepository { get; set; } = null!;

    public static PlayerNameWorldHistoryRepository PlayerNameWorldHistoryRepository { get; set; } = null!;

    public static PlayerCustomizeHistoryRepository PlayerCustomizeHistoryRepository { get; set; } = null!;

    public static PlayerCategoryRepository PlayerCategoryRepository { get; set; } = null!;

    public static PlayerTagRepository PlayerTagRepository { get; set; } = null!;

    public static PlayerConfigRepository PlayerConfigRepository { get; set; } = null!;

    public static ArchiveRecordRepository ArchiveRecordRepository { get; set; } = null!;

    private static IDbConnection Database { get; set; } = null!;

    private static IMapper Mapper { get; set; } = null!;

    public static void Initialize(string path)
    {
        Database = SQLiteDbConnectionBuilder.Build(path);
        Mapper = CreateMapper();
        BackupRepository = new BackupRepository(Database, Mapper);
        CategoryRepository = new CategoryRepository(Database, Mapper);
        EncounterRepository = new EncounterRepository(Database, Mapper);
        LodestoneRepository = new LodestoneLookupRepository(Database, Mapper);
        PlayerRepository = new PlayerRepository(Database, Mapper);
        ConfigRepository = new ConfigRepository(Database, Mapper);
        PlayerEncounterRepository = new PlayerEncounterRepository(Database, Mapper);
        TagRepository = new TagRepository(Database, Mapper);
        PlayerNameWorldHistoryRepository = new PlayerNameWorldHistoryRepository(Database, Mapper);
        PlayerCustomizeHistoryRepository = new PlayerCustomizeHistoryRepository(Database, Mapper);
        PlayerCategoryRepository = new PlayerCategoryRepository(Database, Mapper);
        PlayerTagRepository = new PlayerTagRepository(Database, Mapper);
        PlayerConfigRepository = new PlayerConfigRepository(Database, Mapper);
        ArchiveRecordRepository = new ArchiveRecordRepository(Database, Mapper);
        RunMaintenanceChecks();
    }

    public static void Dispose()
    {
        try
        {
            Database.Dispose();
        }
        catch (Exception)
        {
            PluginLog.LogWarning("Failed to dispose RepositoryContext");
        }
    }

    private static IMapper CreateMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<BackupMappingProfile>();
            cfg.AddProfile<CategoryMappingProfile>();
            cfg.AddProfile<EncounterMappingProfile>();
            cfg.AddProfile<LodestoneLookupMappingProfile>();
            cfg.AddProfile<PlayerCustomizeHistoryMappingProfile>();
            cfg.AddProfile<PlayerEncounterMappingProfile>();
            cfg.AddProfile<PlayerMappingProfile>();
            cfg.AddProfile<PlayerNameWorldHistoryMappingProfile>();
            cfg.AddProfile<PlayerConfigMappingProfile>();
            cfg.AddProfile<PlayerTagMappingProfile>();
            cfg.AddProfile<PlayerCategoryMappingProfile>();
            cfg.AddProfile<TagMappingProfile>();
            cfg.AddProfile<ArchiveRecordMappingProfile>();
        });

        return mapperConfig.CreateMapper();
    }

    private static void RunMaintenanceChecks()
    {
        PluginLog.LogVerbose("Entering RepositoryContext.RunMaintenance()");
        var currentTime = UnixTimestampHelper.CurrentTime();
        var config = ConfigRepository.GetPluginConfig();

        if (config == null)
        {
            PluginLog.LogWarning("Plugin config not found, skipping maintenance");
            return;
        }

        const long weekInMillis = 604800000; // 7 days in milliseconds

        if (currentTime - config.MaintenanceLastRunOn >= weekInMillis)
        {
            PluginLog.LogVerbose($"It's been a week since the last maintenance. Current time: {currentTime}, Last run: {config.MaintenanceLastRunOn}");
            SQLiteDbMaintenance.Reindex(Database);
            SQLiteDbMaintenance.Vacuum(Database);
            SQLiteDbMaintenance.Analyze(Database);
            SQLiteDbMaintenance.Optimize(Database);
            config.MaintenanceLastRunOn = currentTime;
            ConfigRepository.UpdatePluginConfig(config);
        }
        else
        {
            PluginLog.LogVerbose("It hasn't been a week since the last maintenance, skipping");
        }

        PluginLog.LogVerbose("Exiting RepositoryContext.RunMaintenance()");
    }
}
