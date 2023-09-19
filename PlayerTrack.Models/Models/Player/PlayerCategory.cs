namespace PlayerTrack.Models;

public class PlayerCategory
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public int PlayerId { get; set; }

    public int CategoryId { get; set; }
}
