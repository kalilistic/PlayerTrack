using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.DrunkenToad;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Colors;
using LiteDB;

namespace PlayerTrack
{
    /// <summary>
    /// Player.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Gets or sets id for litedb record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets composite player key of world id and name.
        /// </summary>
        public string Key { get; set; } = null!;

        /// <summary>
        /// Gets or sets composite for sorting.
        /// </summary>
        [BsonIgnore]
        public string SortKey { get; set; } = null!;

        /// <summary>
        /// Gets or sets current actor id (may change).
        /// </summary>
        [BsonIgnore]
        public uint ActorId { get; set; }

        /// <summary>
        /// Gets or sets lodestone id.
        /// </summary>
        public uint LodestoneId { get; set; }

        /// <summary>
        /// Gets or sets sync status for lodestone id lookup.
        /// </summary>
        public LodestoneStatus LodestoneStatus { get; set; } = LodestoneStatus.Unverified;

        /// <summary>
        /// Gets or sets last date lodestone data update was attempted.
        /// </summary>
        public long LodestoneLastUpdated { get; set; }

        /// <summary>
        /// Gets or sets lodestone failure count to avoid repeatedly trying deleted or invalid characters.
        /// </summary>
        public int LodestoneFailureCount { get; set; }

        /// <summary>
        /// Gets or sets list of names (current and previous).
        /// </summary>
        public List<string> Names { get; set; } = null!;

        /// <summary>
        /// Gets or sets list of homeworlds (current and previous).
        /// </summary>
        public List<KeyValuePair<uint, string>> HomeWorlds { get; set; } = null!;

        /// <summary>
        /// Gets or sets title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets title as SeString.
        /// </summary>
        [BsonIgnore]
        public SeString? SeTitle { get; set; }

        /// <summary>
        /// Gets or sets free company abbreviation.
        /// </summary>
        public string FreeCompany { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last seen territory type id.
        /// </summary>
        public ushort LastTerritoryType { get; set; }

        /// <summary>
        /// Gets or sets the last seen content id.
        /// </summary>
        [BsonIgnore]
        public uint LastContentId { get; set; }

        /// <summary>
        /// Gets or sets the last seen location.
        /// </summary>
        [BsonIgnore]
        public string LastLocationName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets customize byte array.
        /// </summary>
        public byte[]? Customize { get; set; }

        /// <summary>
        /// Gets or sets get customize data as struct.
        /// </summary>
        [BsonIgnore]
        public CharaCustomizeData CharaCustomizeData { get; set; }

        /// <summary>
        /// Gets or sets player created date in unix timestamp (ms).
        /// </summary>
        public long Created { get; set; }

        /// <summary>
        /// Gets or sets player last updated date in unix timestamp (ms).
        /// </summary>
        public long Updated { get; set; }

        /// <summary>
        /// Gets or sets number of times player encountered.
        /// </summary>
        public int SeenCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets player icon (overrides category).
        /// </summary>
        public int Icon { get; set; }

        /// <summary>
        /// Gets or sets player list color (overrides category).
        /// </summary>
        public Vector4? ListColor { get; set; }

        /// <summary>
        /// Gets or sets player name plate color (overrides category).
        /// </summary>
        public Vector4? NamePlateColor { get; set; }

        /// <summary>
        /// Gets or sets player notes.
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether alert is enabled.
        /// </summary>
        public bool IsAlertEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the next time alert should be sent.
        /// </summary>
        public long SendNextAlert { get; set; }

        /// <summary>
        /// Gets or sets player category.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Gets or sets tags.
        /// </summary>
        public List<string> Tags { get; set; } = new ();

        /// <summary>
        /// Gets or sets player category rank for sorting.
        /// </summary>
        [BsonIgnore]
        public int CategoryRank { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the player is currently with you.
        /// </summary>
        [BsonIgnore]
        public bool IsCurrent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether you encountered the player in current session.
        /// </summary>
        [BsonIgnore]
        public bool IsRecent { get; set; }

        /// <summary>
        /// Set free company by company tag.
        /// </summary>
        /// <param name="contentId">last content id.</param>
        /// <param name="companyTag">company tag.</param>
        /// <returns>free company tag.</returns>
        public static string DetermineFreeCompany(uint contentId, string companyTag)
        {
            if (contentId == 0)
            {
                return string.IsNullOrEmpty(companyTag) ? "None" : companyTag;
            }

            return "N/A";
        }

        /// <summary>
        /// Get effective player list color based on default and overrides.
        /// </summary>
        /// <returns>player list color.</returns>
        public Vector4 EffectiveListColor()
        {
            if (this.ListColor != null) return (Vector4)this.ListColor;
            return ImGuiColors.DalamudWhite;
        }

        /// <summary>
        /// Get effective player nameplate color based on category and overrides.
        /// </summary>
        /// <returns>player nameplate color.</returns>
        public Vector4 EffectiveNamePlateColor()
        {
            if (this.NamePlateColor != null) return (Vector4)this.NamePlateColor;
            return ImGuiColors.DalamudWhite;
        }

        /// <summary>
        /// Reset player overrides to defaults.
        /// </summary>
        public void Reset()
        {
            this.Icon = 0;
            this.ListColor = null;
            this.NamePlateColor = null;
            this.IsAlertEnabled = false;
            this.Title = string.Empty;
            this.SeTitle = null;
        }

        /// <summary>
        /// Update existing player with new data.
        /// </summary>
        /// <param name="player">new player record.</param>
        public void UpdateFromNewCopy(Player player)
        {
            // overwrite
            this.ActorId = player.ActorId;
            this.LastTerritoryType = player.LastTerritoryType;
            this.LastContentId = player.LastContentId;
            this.LastLocationName = player.LastLocationName;
            this.Customize = player.Customize;
            this.CharaCustomizeData = player.CharaCustomizeData;
            this.Updated = player.Updated;

            if (player.LastContentId == 0)
            {
                this.FreeCompany = player.FreeCompany;
            }

            // increment
            this.SeenCount += 1;

            // reset
            this.IsCurrent = true;
            this.IsRecent = true;
        }

        /// <summary>
        /// Merge new copy into existing player from duplicate detection.
        /// </summary>
        /// <param name="player">newer player record.</param>
        public void Merge(Player player)
        {
            if (this.Updated < player.Updated)
            {
                // overwrite
                this.Key = player.Key;
                this.ActorId = player.ActorId;
                this.FreeCompany = player.FreeCompany;
                this.LastTerritoryType = player.LastTerritoryType;
                this.LastContentId = player.LastContentId;
                this.LastLocationName = player.LastLocationName;
                this.Customize = player.Customize;
                this.CharaCustomizeData = player.CharaCustomizeData;
                this.Updated = player.Updated;

                // insert newest
                this.Names.InsertRange(0, player.Names);
                this.HomeWorlds.InsertRange(0, player.HomeWorlds);
            }
            else
            {
                // append oldest
                this.Names.AddRange(player.Names);
                this.HomeWorlds.AddRange(player.HomeWorlds);
            }

            // remove duplicates
            this.Names = this.Names.Distinct().ToList();
            this.HomeWorlds = this.HomeWorlds.Distinct().ToList();

            // combine
            this.IsCurrent = this.IsCurrent || player.IsCurrent;
            this.IsRecent = this.IsRecent || player.IsRecent;
            this.SeenCount += player.SeenCount;
            this.Notes += " " + player.Notes;
        }

        /// <summary>
        /// Set SeString based on title.
        /// </summary>
        public void SetSeTitle()
        {
            if (string.IsNullOrEmpty(this.Title))
            {
                this.SeTitle = null;
            }
            else
            {
                this.SeTitle = new SeString(new Payload[]
                {
                    new TextPayload($"《{this.Title}》"),
                });
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            StringBuilder sb = new ();
            sb.Append(System.Environment.NewLine);
            sb.Append("Key: " + this.Key);
            sb.Append(System.Environment.NewLine);
            sb.Append("Name: " + this.Names.First());
            sb.Append(System.Environment.NewLine);
            sb.Append("World: " + this.HomeWorlds.First().Key);
            sb.Append(System.Environment.NewLine);
            sb.Append("CategoryId: " + this.CategoryId);
            sb.Append(System.Environment.NewLine);
            sb.Append("CategoryRank: " + this.CategoryRank);
            sb.Append(System.Environment.NewLine);
            return sb.ToString();
        }
    }
}
