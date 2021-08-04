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
using System.ComponentModel;
using Dalamud.DrunkenToad;
using Newtonsoft.Json;

namespace PlayerTrack
{
    [Obsolete]
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackLodestone
    {
        [JsonProperty] [DefaultValue(0)] public uint Id { get; set; }
        [JsonProperty] [DefaultValue(0)] public long LastUpdated { get; set; }
        [JsonProperty] [DefaultValue(0)] public long LastFailed { get; set; }
        [JsonProperty] [DefaultValue(0)] public int FailureCount { get; set; }
        [JsonProperty] [DefaultValue(0)] public TrackLodestoneStatus Status { get; set; }

        public string LastUpdatedDisplay => LastUpdated == 0 ? "Never" : LastUpdated.ToTimeSpan();

        public string GetProfileUrl(TrackLodestoneLocale locale = TrackLodestoneLocale.na)
        {
            if (Id == 0) return null;
            return "https://" + locale + ".finalfantasyxiv.com/lodestone/character/" + Id;
        }
    }
}
