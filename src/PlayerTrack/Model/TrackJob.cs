using Newtonsoft.Json;

namespace PlayerTrack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackJob
    {
        public string Code;

        [JsonProperty] public uint Id;

        [JsonProperty] public byte Lvl { get; set; }
    }
}