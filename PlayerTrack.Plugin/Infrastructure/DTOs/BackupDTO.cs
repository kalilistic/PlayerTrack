using FluentDapperLite.Repository;

namespace PlayerTrack.Infrastructure;

public class BackupDTO : DTO
{
    public int backup_type { get; set; }

    public string name { get; set; } = string.Empty;

    public long size { get; set; }

    public bool is_restorable { get; set; }

    public bool is_protected { get; set; }

    public string notes { get; set; } = string.Empty;
}
