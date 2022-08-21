using LiteDB;

namespace PlayerTrack
{
    /// <summary>
    /// Occurence of encountering another player.
    /// </summary>
    public class Encounter
    {
        /// <summary>
        /// Gets or sets id for litedb record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets common id to unite encounter event across players.
        /// </summary>
        public long EventId { get; set; }

        /// <summary>
        /// Gets or sets foreign key reference to player.
        /// </summary>
        public string PlayerKey { get; set; } = null!;

        /// <summary>
        /// Gets or sets encounter start date.
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets encounter last updated date.
        /// </summary>
        public long Updated { get; set; }

        /// <summary>
        /// Gets or sets encounter territory type id.
        /// </summary>
        public ushort TerritoryType { get; set; }

        /// <summary>
        /// Gets or sets encounter player's class/job id.
        /// </summary>
        public uint JobId { get; set; }

        /// <summary>
        /// Gets or sets encounter player's three letter class/job code.
        /// </summary>
        [BsonIgnore]
        public string JobCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets encounter player's class/job level.
        /// </summary>
        public byte JobLvl { get; set; }

        /// <summary>
        /// Gets or sets encounter location depending if in content or world.
        /// </summary>
        [BsonIgnore]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// Create new copy of encounter without ID and passed player key (used for merges).
        /// </summary>
        /// <param name="playerKey">player id.</param>
        /// <returns>new copy of encounter.</returns>
        public Encounter Copy(string playerKey) =>
            new()
            {
                PlayerKey = playerKey,
                Created = this.Created,
                Updated = this.Updated,
                EventId = this.EventId,
                TerritoryType = this.TerritoryType,
                JobId = this.JobId,
                JobLvl = this.JobLvl,
            };
    }
}
