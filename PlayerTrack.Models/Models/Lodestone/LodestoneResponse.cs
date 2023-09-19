using Newtonsoft.Json;

namespace PlayerTrack.Models;

public class LodestoneResponse
{
    [JsonProperty("lodestoneId")] public uint LodestoneId { get; set; }

    [JsonProperty("code")] public int StatusCode { get; set; }

    [JsonProperty("playerName")] public string PlayerName { get; set; } = string.Empty;

    [JsonProperty("worldName")] public string WorldName { get; set; } = string.Empty;
}
