using System;
using System.IO;
using System.Linq;
using Dalamud.Plugin;

namespace PlayerTrack.Extensions;

/// <summary>
/// Dalamud PluginInterface extensions.
/// </summary>
public static class PluginInterfaceExtension
{
    /// <summary>
    /// Get the plugin backup directory for windows (don't use for Wine).
    /// </summary>
    /// <param name="value">dalamud plugin interface.</param>
    /// <returns>Plugin backup directory.</returns>
    public static string WindowsPluginBackupDirectory(this IDalamudPluginInterface value)
    {
        var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var backupsDir = Path.Combine(appDataDir, "XIVLauncher", $"{value.InternalName.FirstCharToLower()}Backups");
        Directory.CreateDirectory(backupsDir);
        return backupsDir;
    }

    /// <summary>
    /// Get the plugin backup directory.
    /// </summary>
    /// <param name="value">dalamud plugin interface.</param>
    /// <returns>Plugin backup directory.</returns>
    public static string PluginBackupDirectory(this IDalamudPluginInterface value)
    {
        var configDir = value.ConfigDirectory.Parent;
        var appDir = configDir?.Parent;
        if (appDir == null)
            return WindowsPluginBackupDirectory(value); // use as a fallback

        var backupsDir = Path.Combine(appDir.FullName, $"{value.InternalName.FirstCharToLower()}Backups");
        Directory.CreateDirectory(backupsDir);
        return backupsDir;
    }

    /// <summary>
    /// Check if different version of plugin is loaded.
    /// </summary>
    /// <param name="value">dalamud plugin interface.</param>
    /// <param name="version">version to check.</param>
    /// <returns>Indicator if another version of the plugin is loaded.</returns>
    public static bool IsDifferentVersionLoaded(this IDalamudPluginInterface value, string version = "Canary")
    {
        var internalName = value.InternalName;
        if (!internalName.EndsWith(version, StringComparison.CurrentCulture))
            return IsPluginLoaded(value, $"{internalName}{version}");

        var stableName = internalName.Replace(version, string.Empty);
        return IsPluginLoaded(value, stableName);
    }

    private static bool IsPluginLoaded(IDalamudPluginInterface pluginInterface, string pluginName)
    {
        var plugin = pluginInterface.InstalledPlugins.FirstOrDefault(p => p.Name == pluginName);
        return plugin is { IsLoaded: true };
    }
}
