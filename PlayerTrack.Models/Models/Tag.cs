namespace PlayerTrack.Models;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long Created { get; set; }

    public long Updated { get; set; }

    public uint Color { get; set; } = 5;
}
