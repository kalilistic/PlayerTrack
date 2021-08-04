#pragma warning disable CS1591
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable SA1003
#pragma warning disable SA1009
#pragma warning disable SA1101
#pragma warning disable SA1134
#pragma warning disable SA1204
#pragma warning disable SA1309
#pragma warning disable SA1413
#pragma warning disable SA1516
#pragma warning disable SA1600

using System;

namespace PlayerTrack
{
    [Obsolete]
    public enum TrackLodestoneStatus
    {
        Unverified = 0,
        Verifying = 1,
        Verified = 2,
        Updating = 3, // retired
        Updated = 4,  // retired
        Failed = 5
    }
}
