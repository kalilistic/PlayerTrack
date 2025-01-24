namespace PlayerTrack.Models;

public class LodestoneRefreshRequest
{
    public LodestoneRefreshRequest(int playerId, uint lodestoneId)
    {
        PlayerId = playerId;
        LodestoneId = lodestoneId;
    }

    public int PlayerId { get; }
    public uint LodestoneId { get; }
}
