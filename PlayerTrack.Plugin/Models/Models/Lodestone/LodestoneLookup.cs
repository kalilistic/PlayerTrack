namespace PlayerTrack.Models;

public class LodestoneLookup
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public int PlayerId { get; set; }

    public string PlayerName { get; init; } = string.Empty;

    public uint WorldId { get; init; }

    public string UpdatedPlayerName { get; set; } = string.Empty;

    public uint UpdatedWorldId { get; set; }

    public uint LodestoneId { get; private set; }

    public int FailureCount { get; set; }

    public bool IsDone { get; private set; }

    public int? PrerequisiteLookupId { get; set; }

    public LodestoneStatus LodestoneStatus { get; private set; } = LodestoneStatus.Unverified;

    public LodestoneLookupType LodestoneLookupType { get; init; } = LodestoneLookupType.Batch;

    public void SetLodestoneStatus(LodestoneStatus lodestoneStatus)
    {
        LodestoneStatus = lodestoneStatus;
        IsDone = LodestoneStatus is
            LodestoneStatus.Verified or
            LodestoneStatus.Banned or
            LodestoneStatus.NotApplicable or
            LodestoneStatus.Cancelled or
            LodestoneStatus.Unavailable;
    }

    public void SetLodestoneId(uint lodestoneId)
    {
        if (lodestoneId == 0 || LodestoneId != 0) return;
        LodestoneId = lodestoneId;
    }

    public void UpdateLookupAsFailed(bool allowRetry)
    {
        FailureCount++;

        if (FailureCount < 4 && allowRetry)
            SetLodestoneStatus(LodestoneStatus.Failed);
        else
            SetLodestoneStatus(LodestoneStatus.Banned);
    }

    public void UpdateLookupAsUnavailable()
    {
        SetLodestoneStatus(LodestoneStatus.Unavailable);
    }

    public void UpdateLookupAsInvalid()
    {
        SetLodestoneStatus(LodestoneStatus.Invalid);
    }

    public void UpdateLookupAsSuccess(LodestoneResponse response, LodestoneStatus status)
    {
        UpdatedPlayerName = response.PlayerName;
        UpdatedWorldId = response.WorldId;
        SetLodestoneId(response.LodestoneId);
        SetLodestoneStatus(status);
    }
}
