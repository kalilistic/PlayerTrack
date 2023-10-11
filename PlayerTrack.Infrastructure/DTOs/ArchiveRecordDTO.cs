using FluentDapperLite.Repository;
using PlayerTrack.Models;

namespace PlayerTrack.Infrastructure;

public class ArchiveRecordDTO : DTO
{
    public int Id { get; set; }

    public ArchiveType archive_type { get; set; }

    public string data { get; set; } = string.Empty;
}
