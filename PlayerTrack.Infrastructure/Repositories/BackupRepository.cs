using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;
using Dalamud.Logging;
using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class BackupRepository : BaseRepository
{
    public BackupRepository(IDbConnection connection, IMapper mapper)
        : base(connection, mapper)
    {
    }

    public List<Backup>? GetAllBackups()
    {
        PluginLog.LogVerbose("Entering BackupRepository.GetAllBackups()");
        try
        {
            const string sql = "SELECT * FROM backups";
            var backupDTOs = this.Connection.Query<BackupDTO>(sql).ToList();
            return backupDTOs.Select(dto => this.Mapper.Map<Backup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to get all backups.");
            return null;
        }
    }

    public List<Backup>? GetAllUnprotectedBackups()
    {
        PluginLog.LogVerbose("Entering BackupRepository.GetAllUnprotectedBackups()");
        try
        {
            const string sql = "SELECT * FROM backups WHERE is_protected = @is_protected ORDER BY created";
            var backupDTOs = this.Connection.Query<BackupDTO>(sql, new { is_protected = 0 }).ToList();
            return backupDTOs.Select(dto => this.Mapper.Map<Backup>(dto)).ToList();
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to get unprotected backups.");
            return null;
        }
    }

    public int CreateBackup(Backup backup, bool setTimestamps = true)
    {
        PluginLog.LogVerbose($"Entering BackupRepository.CreateBackup(), backup: {backup.Name}");
        using var transaction = this.Connection.BeginTransaction();
        try
        {
            var backupDTO = this.Mapper.Map<BackupDTO>(backup);
            if (setTimestamps)
            {
                SetCreateTimestamp(backupDTO);
            }

            const string insertSql = @"INSERT INTO backups
                                (created, updated, backup_type, name, size, is_restorable, is_protected, notes)
                                VALUES (@created, @updated, @backup_type, @name, @size, @is_restorable, @is_protected, @notes)";
            this.Connection.Execute(insertSql, backupDTO, transaction);

            const string selectByCreatedDateSql = @"SELECT id FROM backups WHERE created = @created LIMIT 1";
            var newId = this.Connection.ExecuteScalar<int>(selectByCreatedDateSql, new { backupDTO.created }, transaction);

            transaction.Commit();

            return newId;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            PluginLog.LogError(ex, "Failed to create and retrieve backup based on created date.", backup);
            return 0;
        }
    }

    public bool DeleteBackup(int backupId)
    {
        PluginLog.LogVerbose($"Entering BackupRepository.DeleteBackup(): {backupId}");
        try
        {
            const string sql = @"DELETE FROM backups WHERE id = @id";
            this.Connection.Execute(sql, new { id = backupId });
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, $"Failed to delete backup with ID {backupId}.");
            return false;
        }
    }

    public Backup? GetLatestBackup()
    {
        PluginLog.LogVerbose("Entering BackupRepository.GetLatestBackup()");
        try
        {
            const string sql = @"SELECT * FROM backups ORDER BY created DESC LIMIT 1";
            var backupDTO = this.Connection.QueryFirstOrDefault<BackupDTO>(sql);
            return backupDTO != null ? this.Mapper.Map<Backup>(backupDTO) : null;
        }
        catch (Exception ex)
        {
            PluginLog.LogError(ex, "Failed to get the latest backup.");
            return null;
        }
    }
}
