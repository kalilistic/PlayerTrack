using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class BackupRepository : BaseRepository
{
    public BackupRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public List<Backup>? GetAllBackups()
    {
        Plugin.PluginLog.Verbose("Entering BackupRepository.GetAllBackups()");
        try
        {
            const string sql = "SELECT * FROM backups";
            var backupDTOs = Connection.Query<BackupDTO>(sql).ToList();
            return backupDTOs.Select(dto => Mapper.Map<Backup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to get all backups.");
            return null;
        }
    }

    public List<Backup>? GetAllUnprotectedBackups()
    {
        Plugin.PluginLog.Verbose("Entering BackupRepository.GetAllUnprotectedBackups()");
        try
        {
            const string sql = "SELECT * FROM backups WHERE is_protected = @is_protected ORDER BY created";
            var backupDTOs = Connection.Query<BackupDTO>(sql, new { is_protected = 0 }).ToList();
            return backupDTOs.Select(dto => Mapper.Map<Backup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to get unprotected backups.");
            return null;
        }
    }

    public int CreateBackup(Backup backup, bool setTimestamps = true)
    {
        Plugin.PluginLog.Verbose($"Entering BackupRepository.CreateBackup(), backup: {backup.Name}");
        try
        {
            var backupDTO = Mapper.Map<BackupDTO>(backup);
            if (setTimestamps)
                SetCreateTimestamp(backupDTO);

            const string insertSql = @"INSERT INTO backups
                                (created, updated, backup_type, name, size, is_restorable, is_protected, notes)
                                VALUES (@created, @updated, @backup_type, @name, @size, @is_restorable, @is_protected, @notes)
                                RETURNING id";

            return Connection.ExecuteScalar<int>(insertSql, backupDTO);
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to create backup.", backup);
            return 0;
        }
    }

    public bool DeleteBackup(int backupId)
    {
        Plugin.PluginLog.Verbose($"Entering BackupRepository.DeleteBackup(): {backupId}");
        try
        {
            const string sql = @"DELETE FROM backups WHERE id = @id";
            Connection.Execute(sql, new { id = backupId });
            return true;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to delete backup with ID {backupId}.");
            return false;
        }
    }

    public Backup? GetLatestBackup()
    {
        Plugin.PluginLog.Verbose("Entering BackupRepository.GetLatestBackup()");
        try
        {
            const string sql = @"SELECT * FROM backups ORDER BY created DESC LIMIT 1";
            var backupDTO = Connection.QueryFirstOrDefault<BackupDTO>(sql);
            return backupDTO != null ? Mapper.Map<Backup>(backupDTO) : null;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to get the latest backup.");
            return null;
        }
    }
}
