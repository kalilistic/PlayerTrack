using Newtonsoft.Json;

namespace PlayerTrack
{
    /// <summary>
    /// Lodestone response.
    /// </summary>
    public class LodestoneResponse
    {
        /// <summary>
        /// Player key.
        /// </summary>
        public string PlayerKey = string.Empty;

        /// <summary>
        /// Gets or sets lodestone status.
        /// </summary>
        public LodestoneStatus Status { get; set; } = LodestoneStatus.Unverified;

        /// <summary>
        /// Gets or sets lodestone Id.
        /// </summary>
        [JsonProperty("lodestoneId")]
        public uint LodestoneId { get; set; }

        /// <summary>
        /// Gets or sets individual status code.
        /// </summary>
        [JsonProperty("code")]
        public uint StatusCode { get; set; }

        /// <summary>
        /// Gets or sets player name.
        /// </summary>
        [JsonProperty("playerName")]
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets world name.
        /// </summary>
        [JsonProperty("worldName")]
        public string WorldName { get; set; } = string.Empty;
    }
}
