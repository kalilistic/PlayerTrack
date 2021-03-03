using Newtonsoft.Json;

namespace PlayerTrack
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TrackWorld
    {
        [JsonProperty] public uint Id;

        public string Name;


        public bool Equals(TrackWorld trackWorld)
        {
            return Id == trackWorld.Id;
        }
    }
}