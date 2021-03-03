using Newtonsoft.Json;

namespace PlayerTrack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackEncounter
    {
        private string _duration;
        private string _time;

        public string Time => _time ?? (_time = Created.ToTimeSpan());
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