using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using System.Text.RegularExpressions;
using PlayerTrack.Extensions;
using Util = Dalamud.Utility.Util;

namespace PlayerTrack.Domain;

public class BackupService
{
    private string PluginDir = null!;
    private string BackupDir = null!;
    private int PluginVersion;
    private int LastVersionBackup;

    public static List<Backup> GetBackups() =>
        RepositoryContext.BackupRepository.GetAllBackups()?.OrderByDescending(bk => bk.Created).ToList() ?? [];

    public static List<Backup> GetUnprotectedBackups() =>
        RepositoryContext.BackupRepository.GetAllUnprotectedBackups() ?? new List<Backup>();

    public void Startup()
    {
        Plugin.PluginLog.Verbose("Entering BackupService.Startup()");
        PluginDir = Plugin.PluginInterface.GetPluginConfigDirectory();
        BackupDir = Plugin.PluginInterface.PluginBackupDirectory();
        PluginVersion = ServiceContext.ConfigService.GetConfig().PluginVersion;
        LastVersionBackup = ServiceContext.ConfigService.GetConfig().LastVersionBackup;
        RunStartupChecks();
    }

    public void AutoDeleteBackups()
    {
        const int MaxBackups = 5;

        Plugin.PluginLog.Verbose("Entering BackupService.AutoDeleteBackups()");
        var unprotectedBackups = GetUnprotectedBackups();
        if (unprotectedBackups is not { Count: > MaxBackups })
            return;

        while (unprotectedBackups.Count > MaxBackups)
        {
            DeleteBackup(unprotectedBackups[0]);
            unprotectedBackups.RemoveAt(0);
        }
    }

    public bool DeleteBackup(Backup backup)
    {
        Plugin.PluginLog.Verbose($"Entering BackupService.DeleteBackup(): {backup.Name}");
        try
        {
            File.Delete(Path.Combine(BackupDir, backup.Name));
            RepositoryContext.BackupRepository.DeleteBackup(backup.Id);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error($"Failed to delete {backup.Name}.", ex);
            return false;
        }
    }

    public void RunBackup(BackupType backupType)
    {
        Plugin.PluginLog.Verbose($"Entering BackupService.RunBackup(): {backupType}");
        var backup = CreateBackupEntry(backupType);
        if (!File.Exists(Path.Combine(BackupDir, "data.db")))
            File.Copy(Path.Combine(PluginDir, "data.db"), Path.Combine(BackupDir, "data.db"));

        FileHelper.CompressFile(Path.Combine(BackupDir, "data.db"), backup.Name);
        var fileInfo = new FileInfo(Path.Combine(BackupDir, backup.Name));
        backup.Size = fileInfo.Length;

        RepositoryContext.BackupRepository.CreateBackup(backup);
    }

    private Backup CreateBackupEntry(BackupType type)
    {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bool isProtected;
        switch (type)
        {
            case BackupType.Automatic:
            case BackupType.Manual:
                isProtected = false;
                break;
            case BackupType.Upgrade:
            case BackupType.Unknown:
            default:
                isProtected = true;
                break;
        }

        var backup = new Backup
        {
            BackupType = type,
            Created = currentTime,
            IsRestorable = true,
            IsProtected = isProtected,
            Notes = string.Empty,
            Name = $"v{PluginVersion}_{currentTime}.zip",
        };

        return backup;
    }

    private void MigrateBackups(string oldBackupDir)
    {
        try
        {
            if (Directory.Exists(oldBackupDir))
            {
                foreach (var file in Directory.GetFiles(oldBackupDir))
                    File.Move(file, Path.Combine(BackupDir, Path.GetFileName(file)));

                if (!Directory.EnumerateFileSystemEntries(oldBackupDir).Any())
                    Directory.Delete(oldBackupDir);
            }
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to move old backups to new directory.");
        }

    }

    private void RunStartupChecks()
    {
        // setup directories
        Directory.CreateDirectory(PluginDir);
        Directory.CreateDirectory(BackupDir);

        // move files in old backup dir to new one
        MigrateBackups(Path.Combine(PluginDir, "backups"));
        if (Util.IsWine())
            MigrateBackups(Plugin.PluginInterface.WindowsPluginBackupDirectory());

        // create backup records for discovered files
        var files = Directory.GetFiles(BackupDir);
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Extension.Equals(".zip", StringComparison.Ordinal))
            {
                Plugin.PluginLog.Warning($"Found unknown file in backup directory: {fileInfo.Name}");
                continue;
            }

            var backup = GetBackups().FirstOrDefault(bk => bk.Name.Equals(fileInfo.Name, StringComparison.Ordinal));
            if (backup != null)
                continue;

            DateTimeOffset creationTime = fileInfo.CreationTimeUtc;
            DateTimeOffset modificationTime = fileInfo.LastWriteTimeUtc;
            var creationTimestamp = creationTime.ToUnixTimeMilliseconds();
            var modificationTimestamp = modificationTime.ToUnixTimeMilliseconds();
            var backupType = fileInfo.Name switch
            {
                { } name when name.Contains("UPGRADE") => BackupType.Upgrade,
                { } name when name.Contains("AUTOMATIC") => BackupType.Automatic,
                _ => BackupType.Unknown,
            };

            var regex = new Regex(@"\d{13}", RegexOptions.Compiled);
            var match = regex.Match(fileInfo.Name);
            if (match.Success)
            {
                if (long.TryParse(match.Value, out var filenameTimestamp))
                {
                    creationTimestamp = filenameTimestamp;
                    modificationTimestamp = filenameTimestamp;
                }
            }

            backup = new Backup
            {
                Name = fileInfo.Name,
                Size = fileInfo.Length,
                BackupType = backupType,
                IsProtected = true,
                Created = creationTimestamp,
                Updated = modificationTimestamp,
            };
            RepositoryContext.BackupRepository.CreateBackup(backup, false);
        }

        // Run automatic scheduled backup if needed
        const long backupInterval = 43200000;
        var latestBackup = RepositoryContext.BackupRepository.GetLatestBackup();
        if (latestBackup == null || latestBackup.Created + backupInterval < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
        {
            Plugin.PluginLog.Verbose($"Running automatic backup.");
            RunBackup(BackupType.Automatic);
        }

        // Run upgrade backup if needed
        var config = ServiceContext.ConfigService.GetConfig();
        Plugin.PluginLog.Verbose($"Checking for upgrade backup. Last backup version: {LastVersionBackup}. Current plugin version: {PluginVersion}.");
        if (LastVersionBackup < PluginVersion)
        {
            Plugin.PluginLog.Verbose($"Running upgrade backup from v{LastVersionBackup} to v{PluginVersion}.");
            RunBackup(BackupType.Upgrade);
            LastVersionBackup = PluginVersion;
            config.LastVersionBackup = PluginVersion;
            ServiceContext.ConfigService.SaveConfig(config);
        }
        else
        {
            Plugin.PluginLog.Verbose($"No upgrade backup needed.");
        }

        // Clean up deleted backup records
        foreach (var backup in GetBackups().Where(backup => !File.Exists(Path.Combine(BackupDir, backup.Name))))
        {
            Plugin.PluginLog.Verbose($"Backup {backup.Name} is missing. Marking as deleted.");
            RepositoryContext.BackupRepository.DeleteBackup(backup.Id);
        }

        // delete old backups
        AutoDeleteBackups();
    }
}
