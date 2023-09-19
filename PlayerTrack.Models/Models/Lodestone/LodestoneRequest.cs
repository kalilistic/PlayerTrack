using Newtonsoft.Json;

namespace PlayerTrack.Models;

public class LodestoneRequest
{
    public LodestoneRequest(int playerId, string playerName, string worldName)
    {
        this.PlayerId = playerId;
        this.PlayerName = playerName;
        this.WorldName = worldName;
    }

    [JsonIgnore] public int PlayerId { get; }

    [JsonProperty("playerName", Required = Required.Always)] public string PlayerName { get; set; }

    [JsonProperty("worldName", Required = Required.Always)] public string WorldName { get; set; }
}
