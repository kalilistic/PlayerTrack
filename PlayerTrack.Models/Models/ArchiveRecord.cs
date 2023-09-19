namespace PlayerTrack.Models;

public class ArchiveRecord
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public ArchiveType ArchiveType { get; set; }

    public string Data { get; set; } = string.Empty;
}
