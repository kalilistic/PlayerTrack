using Dalamud.DrunkenToad.Extensions;

namespace PlayerTrack.Models;

using System;

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
            if (string.IsNullOrEmpty(this.Name))
            {
                return this.Name;
            }

            var nameWithoutExtension = this.Name.EndsWith(".zip", StringComparison.Ordinal) ? this.Name[..^4] : this.Name;
            return nameWithoutExtension.TruncateWithEllipsis(25);
        }
    }
}
