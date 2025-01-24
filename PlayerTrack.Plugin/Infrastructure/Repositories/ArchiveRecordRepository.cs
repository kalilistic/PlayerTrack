using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMapper;

using Dapper;
using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class ArchiveRecordRepository : BaseRepository
{
    public ArchiveRecordRepository(IDbConnection connection, IMapper mapper) : base(connection, mapper) { }

    public bool CreateArchiveRecord(ArchiveRecord archiveRecord)
    {
        Plugin.PluginLog.Verbose("Entering ArchiveRecordRepository.CreateArchiveRecord()");
        using var transaction = Connection.BeginTransaction();
        try
        {
            var migrationArchiveDTO = Mapper.Map<ArchiveRecordDTO>(archiveRecord);
            SetCreateTimestamp(migrationArchiveDTO);

            const string sql =
                "INSERT INTO archive_records (archive_type, data, created, updated) " +
                "VALUES (@archive_type, @data, @created, @updated)";
            Connection.Execute(sql, migrationArchiveDTO, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Plugin.PluginLog.Error(ex, "Failed to create new migration archive.");
            return false;
        }
    }

    public bool CreateArchiveRecords(IEnumerable<ArchiveRecord> migrationArchiveRecords)
    {
        Plugin.PluginLog.Verbose("Entering ArchiveRecordRepository.CreateArchiveRecords()");
        using var transaction = Connection.BeginTransaction();
        try
        {
            const string sql = @"
            INSERT INTO archive_records (
                archive_type,
                data,
                created,
                updated)
            VALUES (
                @archive_type,
                @data,
                @created,
                @updated)";

            var migrationArchiveDTOs = migrationArchiveRecords
                .Select(Mapper.Map<ArchiveRecordDTO>)
                .ToList();

            foreach (var dto in migrationArchiveDTOs)
                SetCreateTimestamp(dto);

            Connection.Execute(sql, migrationArchiveDTOs, transaction);

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Plugin.PluginLog.Error(ex, "Failed to create new migration archives.");
            return false;
        }
    }
}
