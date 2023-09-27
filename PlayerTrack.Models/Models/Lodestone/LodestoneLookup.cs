namespace PlayerTrack.Models;

using Dalamud.DrunkenToad.Core;

public class LodestoneLookup
{
    public int Id { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    public int PlayerId { get; init; }

    public string PlayerName { get; set; } = string.Empty;

    public string WorldName { get; set; } = string.Empty;

    public uint LodestoneId { get; set; }

    public int FailureCount { get; set; }

    public LodestoneStatus LodestoneStatus { get; set; } = LodestoneStatus.Unverified;

    public void FlagAsFailed()
    {
        this.FailureCount++;

        if (this.FailureCount < 3)
        {
            this.LodestoneStatus = LodestoneStatus.Failed;
            DalamudContext.PluginLog.Verbose($"Failed to find lodestone response for {this.PlayerName} on {this.WorldName}.");
        }
        else
        {
            this.LodestoneStatus = LodestoneStatus.Banned;
            DalamudContext.PluginLog.Verbose($"Banned lodestone response for {this.PlayerName} on {this.WorldName}.");
        }
    }

    public void FlagAsSuccessful(uint lodestoneId)
    {
        this.LodestoneStatus = LodestoneStatus.Verified;
        this.LodestoneId = lodestoneId;
        DalamudContext.PluginLog.Verbose($"Found lodestone response for {this.PlayerName} on {this.WorldName}.");
    }

    public void Reset()
    {
        this.FailureCount = 0;
        this.LodestoneStatus = LodestoneStatus.Unverified;
    }
}
