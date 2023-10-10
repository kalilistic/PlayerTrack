using System;
using System.IO;
using System.Reflection;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Plugin;
using FluentDapperLite.Runner;
using PlayerTrack.API;
using PlayerTrack.Domain;
using PlayerTrack.Infrastructure;
using PlayerTrack.Migration;

namespace PlayerTrack.Plugin;

using System.Linq;
using System.Threading.Tasks;

public class Plugin : IDalamudPlugin
{
    public Plugin(DalamudPluginInterface pluginInterface)
    {
        if (!DalamudContext.Initialize(pluginInterface))
        {
            return;
        }

        if (!this.ShouldRun())
        {
            DalamudContext.PluginLog.Error("Terminating plugin since another version of PlayerTrack is loaded.");
            return;
        }

        var isDatabaseLoadedSuccessfully = LoadDatabase();
        if (!isDatabaseLoadedSuccessfully)
        {
            return;
        }

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
            ServiceContext.LodestoneService.Stop();
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

    private static bool PluginNotLoaded(string pluginName)
    {
        var plugin = DalamudContext.PluginInterface.InstalledPlugins.FirstOrDefault(p => p.Name == pluginName);
        return plugin == null || !plugin.IsLoaded;
    }

    private bool ShouldRun()
    {
        var internalName = DalamudContext.PluginInterface.InternalName;
        if (!internalName.EndsWith("Canary", StringComparison.CurrentCulture))
        {
            return PluginNotLoaded($"{internalName}Canary");
        }

        var stableName = internalName.Replace("Canary", string.Empty);
        return PluginNotLoaded(stableName);
    }

    private void RunPostStartup() => Task.Run(() =>
    {
        DalamudContext.PluginLog.Verbose("Entering Plugin.RunPostStartup()");
        if (!LiteDBMigrator.Run())
        {
            DalamudContext.PluginLog.Error("Terminating plugin early since migration failed.");
            return;
        }

        ServiceContext.ConfigService.SyncIcons();
        ServiceContext.PlayerDataService.ReloadPlayerCache();
        SetPluginVersion();
        ServiceContext.BackupService.Startup();
        GuiController.Start();
        ContextMenuHandler.Start();
        EventDispatcher.Start();
        NameplateHandler.Start();
        CommandHandler.Start();
        DalamudContext.PlayerLocationManager.Start();
        DalamudContext.PlayerEventDispatcher.Start();
        this.PlayerTrackProvider = new PlayerTrackProvider(DalamudContext.PluginInterface, new PlayerTrackAPI());
        PlayerProcessService.CheckForDuplicates();
    });
}
