using Newtonsoft.Json;

namespace PlayerTrack.Models;

public class LodestoneResponse
{
    [JsonProperty("playerName")] public string PlayerName { get; set; } = string.Empty;
    
    [JsonProperty("worldId")] public uint WorldId { get; set; }
    
    [JsonProperty("lodestoneId")] public uint LodestoneId { get; set; }

    [JsonProperty("code")] public int StatusCode { get; set; }

    [JsonProperty("message")] public string Message { get; set; } = string.Empty;
    
}
