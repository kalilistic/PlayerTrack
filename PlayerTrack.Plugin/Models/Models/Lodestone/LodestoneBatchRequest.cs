using Newtonsoft.Json;

namespace PlayerTrack.Models;

public class LodestoneBatchRequest
{
    public LodestoneBatchRequest(int playerId, string playerName, uint worldId)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        WorldId = worldId;
    }

    [JsonIgnore]
    public int PlayerId { get; }

    [JsonProperty("playerName", Required = Required.Always)]
    public string PlayerName { get; set; }

    [JsonProperty("worldId", Required = Required.Always)]
    public uint WorldId { get; set; }
}
