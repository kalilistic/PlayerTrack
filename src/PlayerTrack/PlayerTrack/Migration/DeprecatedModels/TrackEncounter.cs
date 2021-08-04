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
using Dalamud.DrunkenToad;
using Newtonsoft.Json;

// ReSharper disable All
namespace PlayerTrack
{
    [Obsolete]
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackEncounter
    {
        private string _duration;
        private string _time;

        public string Time => _time ?? (_time = this.Created.ToTimeSpan());
        public string Duration => _duration ?? (_duration = (Updated - Created).ToDuration());

        [JsonProperty] public long Created { get; set; }
        [JsonProperty] public long Updated { get; set; }
        [JsonProperty] public TrackLocation Location { get; set; }
        [JsonProperty] public TrackJob Job { get; set; }

        public void ClearBackingFields()
        {
            _time = null;
            _duration = null;
        }
    }
}
