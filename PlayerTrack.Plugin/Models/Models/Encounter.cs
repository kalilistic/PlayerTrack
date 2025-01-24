using Dapper.Contrib.Extensions;

namespace PlayerTrack.Models;

public class Encounter
{
    public int Id { get; set; }

    public ushort TerritoryTypeId { get; set; }

    public long Ended { get; set; }

    public long Created { get; set; }

    public long Updated { get; set; }

    [Write(false)] public bool SaveEncounter { get; set; }

    [Write(false)] public bool SavePlayers { get; set; }

    [Write(false)] public int CategoryId { get; set; }
}
