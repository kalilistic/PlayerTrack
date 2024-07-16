using System;
using System.IO;
using System.Reflection;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.DrunkenToad.Helpers;
using Dalamud.Plugin;
using FluentDapperLite.Runner;
using PlayerTrack.API;
using PlayerTrack.Domain;
using PlayerTrack.Infrastructure;
using PlayerTrack.Migration;

namespace PlayerTrack.Plugin;

using System.Threading.Tasks;

public class Plugin : IDalamudPlugin
{
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        if (!DalamudContext.Initialize(pluginInterface)) return;

        if (pluginInterface.IsDifferentVersionLoaded())
        {
            DalamudContext.PluginLog.Error("Terminating plugin since another version of PlayerTrack is loaded.");
            return;
        }

        var isDatabaseLoadedSuccessfully = LoadDatabase();
        if (!isDatabaseLoadedSuccessfully) return;

        DalamudContext.LocManager.LoadLanguagesFromAssembly("PlayerTrack.Plugin.Resource.Loc");
        RepositoryContext.Initialize(DalamudContext.PluginInterface.GetPluginConfigDirectory());
        ServiceContext.Initialize();
        this.RunPostStartup();
    }

    private PlayerTrackProvider? PlayerTrackProvider { get; set; }

    public void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering Plugin.Dispose()");
        GC.SuppressFinalize(this);
        try
        {
            PluginInstanceLock.ReleaseLock();
            this.PlayerTrackProvider?.Dispose();
            LiteDBMigrator.Dispose();
            CommandHandler.Dispose();
            NameplateHandler.Dispose();
            EventDispatcher.Dispose();
            ContextMenuHandler.Dispose();
            GuiController.Dispose();
            ServiceContext.Dispose();
            RepositoryContext.Dispose();
            DalamudContext.Dispose();
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error(ex, "Failed to dispose plugin.");
        }
    }

    private static void SetPluginVersion()
    {
        DalamudContext.PluginLog.Verbose("Entering Plugin.SetPluginVersion()");
        var pluginVersion = Assembly.GetExecutingAssembly().VersionNumber();
        var config = ServiceContext.ConfigService.GetConfig();
        config.PluginVersion = pluginVersion;
        ServiceContext.ConfigService.SaveConfig(config);
    }

    private static bool LoadDatabase()
    {
        DalamudContext.PluginLog.Verbose("Entering Plugin.LoadDatabase()");
        var dataSource = Path.Combine(DalamudContext.PluginInterface.GetPluginConfigDirectory(), "data.db");
        var assemblyWithMigrations = Assembly.Load("PlayerTrack.Infrastructure");
        SQLiteFluentMigratorRunner.Run(dataSource, assemblyWithMigrations);
        return true;
    }

    private void RunPostStartup() => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose("Entering Plugin.RunPostStartup()");
        if (!LiteDBMigrator.Run())
        {
            DalamudContext.PluginLog.Error("Terminating plugin early since migration failed.");
            return;
        }

        EncounterService.EnsureNoOpenEncounters();
        ServiceContext.LodestoneService.Start();
        ServiceContext.ConfigService.SyncIcons();
        ServiceContext.PlayerCacheService.LoadPlayers();
        ServiceContext.VisibilityService.Initialize();
        SetPluginVersion();
        ServiceContext.BackupService.Startup();
        GuiController.Start();
        ContextMenuHandler.Start();
        EventDispatcher.Start();
        NameplateHandler.Start();
        CommandHandler.Start();
        DalamudContext.PlayerLocationManager.Start();
        DalamudContext.SocialListHandler.Start();
        ServiceContext.PlayerProcessService.Start();
        this.PlayerTrackProvider = new PlayerTrackProvider(DalamudContext.PluginInterface, new PlayerTrackAPI());
    });
}
