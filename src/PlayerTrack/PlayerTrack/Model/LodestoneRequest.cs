using Newtonsoft.Json;

namespace PlayerTrack
{
    /// <summary>
    /// Request for lodestone.
    /// </summary>
    public class LodestoneRequest
    {
        /// <summary>
        /// Player key.
        /// </summary>
        [JsonIgnore]
        public string PlayerKey = string.Empty;

        /// <summary>
        /// Gets or sets player name.
        /// </summary>
        [JsonProperty("playerName", Required = Required.Always)]
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets world name.
        /// </summary>
        [JsonProperty("worldName", Required = Required.Always)]
        public string WorldName { get; set; } = string.Empty;
    }
}
