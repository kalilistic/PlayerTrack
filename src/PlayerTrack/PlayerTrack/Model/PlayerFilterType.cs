using System.Collections.Generic;
using System.Linq;

// ReSharper disable CollectionNeverQueried.Global
namespace PlayerTrack
{
    /// <summary>
    /// Filter type for player list.
    /// </summary>
    public class PlayerFilterType
    {
        /// <summary>
        /// List of available filter types.
        /// </summary>
        public static readonly List<PlayerFilterType> FilterTypes = new ();

        /// <summary>
        /// List of available filter type names.
        /// </summary>
        public static readonly List<string> FilterTypeNames = new ();

        /// <summary>
        /// Filter Type: Current Players.
        /// </summary>
        public static readonly PlayerFilterType CurrentPlayers = new (0, 0, "Current Players");

        /// <summary>
        /// Filter Type: Recent Players.
        /// </summary>
        public static readonly PlayerFilterType RecentPlayers = new (1, 1, "Recent Players");

        /// <summary>
        /// Filter Type: All Players.
        /// </summary>
        public static readonly PlayerFilterType AllPlayers = new (2, 2, "All Players");

        /// <summary>
        /// Filter Type: By Category.
        /// </summary>
        public static readonly PlayerFilterType PlayersByCategory = new (3, 3, "Players By Category");

        private PlayerFilterType(int index, int code, string name)
        {
            this.Index = index;
            this.Name = name;
            this.Code = code;
            FilterTypes.Add(this);
            FilterTypeNames.Add(name);
        }

        /// <summary>
        /// Gets or sets filter type index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets filter type code.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets filter type name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get player filter type by index.
        /// </summary>
        /// <param name="index">filter index.</param>
        /// <returns>filter type.</returns>
        public static PlayerFilterType GetPlayerFilterTypeByIndex(int index)
        {
            return FilterTypes.FirstOrDefault(view => view.Index == index) !;
        }

        /// <summary>
        /// Return filter name.
        /// </summary>
        /// <returns>filter name.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
