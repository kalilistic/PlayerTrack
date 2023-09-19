namespace PlayerTrack.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public long Created { get; set; }

    public long Updated { get; set; }

    public int Rank { get; set; }

    public PlayerConfig PlayerConfig { get; set; } = new(PlayerConfigType.Category);
}
