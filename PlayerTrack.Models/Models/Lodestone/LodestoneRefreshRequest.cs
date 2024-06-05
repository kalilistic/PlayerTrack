namespace PlayerTrack.Models;

public class LodestoneRefreshRequest
{
    public LodestoneRefreshRequest(int playerId, uint lodestoneId)
    {
        this.PlayerId = playerId;
        this.LodestoneId = lodestoneId;
    }

    public int PlayerId { get; }
    public uint LodestoneId { get; }
}
