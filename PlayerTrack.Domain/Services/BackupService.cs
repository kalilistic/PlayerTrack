using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.DrunkenToad.Core;

using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Text.RegularExpressions;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.DrunkenToad.Helpers;

public class BackupService
{
    private string pluginDir = null!;
    private string backupDir = null!;
    private int pluginVersion;
    private int lastVersionBackup;

    public static List<Backup> GetBackups() =>
        RepositoryContext.BackupRepository.GetAllBackups()?.OrderByDescending(bk => bk.Created).ToList() ?? new List<Backup>();

    public static List<Backup> GetUnprotectedBackups() => RepositoryContext.BackupRepository.GetAllUnprotectedBackups() ?? new List<Backup>();

    public void Startup()
    {
        DalamudContext.PluginLog.Verbose("Entering BackupService.Startup()");
        this.pluginDir = DalamudContext.PluginInterface.GetPluginConfigDirectory();
        this.backupDir = DalamudContext.PluginInterface.PluginBackupDirectory();
        this.pluginVersion = ServiceContext.ConfigService.GetConfig().PluginVersion;
        this.lastVersionBackup = ServiceContext.ConfigService.GetConfig().LastVersionBackup;
        this.RunStartupChecks();
    }

    public void AutoDeleteBackups()
    {
        DalamudContext.PluginLog.Verbose("Entering BackupService.AutoDeleteBackups()");
        const int MaxBackups = 5;
        var unprotectedBackups = GetUnprotectedBackups();
        if (unprotectedBackups is not { Count: > MaxBackups })
        {
            return;
        }

        while (unprotectedBackups.Count > MaxBackups)
        {
            var backupToDelete = unprotectedBackups[0];
            this.DeleteBackup(backupToDelete);
            unprotectedBackups.RemoveAt(0);
        }
    }

    public bool DeleteBackup(Backup backup)
    {
        DalamudContext.PluginLog.Verbose($"Entering BackupService.DeleteBackup(): {backup.Name}");
        try
        {
            File.Delete(Path.Combine(this.backupDir, backup.Name));
            RepositoryContext.BackupRepository.DeleteBackup(backup.Id);
            return true;
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error($"Failed to delete {backup.Name}.", ex);
            return false;
        }
    }

    public void RunBackup(BackupType backupType)
    {
        DalamudContext.PluginLog.Verbose($"Entering BackupService.RunBackup(): {backupType}");
        var backup = this.CreateBackupEntry(backupType);
        if (!File.Exists(Path.Combine(this.backupDir, "data.db")))
        {
            File.Copy(Path.Combine(this.pluginDir, "data.db"), Path.Combine(this.backupDir, "data.db"));
        }

        FileHelper.CompressFile(Path.Combine(this.backupDir, "data.db"), backup.Name);
        var fileInfo = new FileInfo(Path.Combine(this.backupDir, backup.Name));
        backup.Size = fileInfo.Length;

        RepositoryContext.BackupRepository.CreateBackup(backup);
    }

    private Backup CreateBackupEntry(BackupType type)
    {
        var currentTime = UnixTimestampHelper.CurrentTime();
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
            Name = $"v{this.pluginVersion}_{currentTime}.zip",
        };

        return backup;
    }

    private void RunStartupChecks()
    {
        // setup directories
        Directory.CreateDirectory(this.pluginDir);
        Directory.CreateDirectory(this.backupDir);

        // move files in old backup dir to new one
        var oldBackupDir = Path.Combine(this.pluginDir, "backups");
        var oldFiles = Directory.GetFiles(oldBackupDir);
        foreach (var file in oldFiles)
        {
            File.Move(file, Path.Combine(this.backupDir, Path.GetFileName(file)));
        }

        if (Directory.Exists(oldBackupDir) && !Directory.EnumerateFileSystemEntries(oldBackupDir).Any())
        {
            Directory.Delete(oldBackupDir);
        }

        // create backup records for discovered files
        var files = Directory.GetFiles(this.backupDir);
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Extension.Equals(".zip", StringComparison.Ordinal))
            {
                DalamudContext.PluginLog.Warning($"Found unknown file in backup directory: {fileInfo.Name}");
                continue;
            }

            var backup = GetBackups().FirstOrDefault(bk => bk.Name.Equals(fileInfo.Name, StringComparison.Ordinal));
            if (backup == null)
            {
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

                var regex = new Regex(@"\d{13}");
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
        }

        // Run automatic scheduled backup if needed
        const long backupInterval = 43200000;
        var latestBackup = RepositoryContext.BackupRepository.GetLatestBackup();
        if (latestBackup == null || latestBackup.Created + backupInterval < UnixTimestampHelper.CurrentTime())
        {
            DalamudContext.PluginLog.Verbose($"Running automatic backup.");
            this.RunBackup(BackupType.Automatic);
        }

        // Run upgrade backup if needed
        var config = ServiceContext.ConfigService.GetConfig();
        DalamudContext.PluginLog.Verbose($"Checking for upgrade backup. Last backup version: {this.lastVersionBackup}. Current plugin version: {this.pluginVersion}.");
        if (this.lastVersionBackup < this.pluginVersion)
        {
            DalamudContext.PluginLog.Verbose($"Running upgrade backup from v{this.lastVersionBackup} to v{this.pluginVersion}.");
            this.RunBackup(BackupType.Upgrade);
            this.lastVersionBackup = this.pluginVersion;
            config.LastVersionBackup = this.pluginVersion;
            ServiceContext.ConfigService.SaveConfig(config);
        }
        else
        {
            DalamudContext.PluginLog.Verbose($"No upgrade backup needed.");
        }

        // Clean up deleted backup records
        foreach (var backup in GetBackups().Where(backup => !File.Exists(Path.Combine(this.backupDir, backup.Name))))
        {
            DalamudContext.PluginLog.Verbose($"Backup {backup.Name} is missing. Marking as deleted.");
            RepositoryContext.BackupRepository.DeleteBackup(backup.Id);
        }

        // delete old backups
        this.AutoDeleteBackups();
    }
}
