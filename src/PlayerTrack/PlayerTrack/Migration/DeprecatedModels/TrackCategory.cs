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
using System.Numerics;
using Newtonsoft.Json;

namespace PlayerTrack
{
    [Obsolete]
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackCategory
    {
        [JsonProperty] [DefaultValue(0)] public int Id;
        [JsonProperty] [DefaultValue("")] public string Name = string.Empty;
        [JsonProperty] [DefaultValue(0)] public int Icon { get; set; }
        [JsonProperty] public Vector4 Color { get; set; }
        [JsonProperty] [DefaultValue(false)] public bool EnableAlerts { get; set; }
        [JsonProperty] [DefaultValue(false)] public bool IsDefault { get; set; }

        public TrackCategory Copy()
        {
            return new TrackCategory
            {
                Id = Id,
                Name = Name,
                Icon = Icon,
                Color = Color,
                EnableAlerts = EnableAlerts,
                IsDefault = IsDefault
            };
        }
    }
}
