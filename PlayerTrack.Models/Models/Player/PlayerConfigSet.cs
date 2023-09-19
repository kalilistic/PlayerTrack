namespace PlayerTrack.Models;

using System.Collections.Generic;

public class PlayerConfigSet
{
    public PlayerConfigType PlayerConfigType { get; set; }

    public PlayerConfig CurrentPlayerConfig { get; set; } = new();

    public List<PlayerConfig> CategoryPlayerConfigs { get; set; } = new();
}
