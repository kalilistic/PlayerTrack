using System;
using PlayerTrack.Extensions;

namespace PlayerTrack.Models;

public class Backup
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public BackupType BackupType { get; init; } = BackupType.Automatic;

    public string Name { get; init; } = string.Empty;

    public long Size { get; set; }

    public bool IsRestorable { get; init; } = true;

    public bool IsProtected { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(Name))
                return Name;

            var nameWithoutExtension = Name.EndsWith(".zip", StringComparison.Ordinal) ? Name[..^4] : Name;
            return nameWithoutExtension.TruncateWithEllipsis(25);
        }
    }
}
