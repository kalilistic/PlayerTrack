using System.Collections.Generic;
using System.Linq;

// ReSharper disable CollectionNeverQueried.Global
namespace PlayerTrack
{
    /// <summary>
    /// Restriction type for several settings.
    /// </summary>
    public class ContentRestrictionType
    {
        /// <summary>
        /// List of available restriction types.
        /// </summary>
        public static readonly List<ContentRestrictionType> RestrictionTypes = new ();

        /// <summary>
        /// List of available restriction type names.
        /// </summary>
        public static readonly List<string> RestrictionTypeNames = new ();

        /// <summary>
        /// Restriction Type: Always (no restriction).
        /// </summary>
        public static readonly ContentRestrictionType Always = new (0, 0, "Always");

        /// <summary>
        /// Restriction Type: Content Only.
        /// </summary>
        public static readonly ContentRestrictionType ContentOnly = new (1, 1, "Content Only");

        /// <summary>
        /// Restriction Type: High-End Duty Only.
        /// </summary>
        public static readonly ContentRestrictionType HighEndDutyOnly = new (2, 2, "High-End Duty Only");

        /// <summary>
        /// Restriction Type: Never.
        /// </summary>
        public static readonly ContentRestrictionType Never = new (3, 3, "Never");


        private ContentRestrictionType(int index, int code, string name)
        {
            this.Index = index;
            this.Name = name;
            this.Code = code;
            RestrictionTypes.Add(this);
            RestrictionTypeNames.Add(name);
        }

        /// <summary>
        /// Gets or sets restriction type index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets restriction type code.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets restriction type name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get player restriction type by index.
        /// </summary>
        /// <param name="index">restriction index.</param>
        /// <returns>restriction type.</returns>
        public static ContentRestrictionType GetContentRestrictionTypeByIndex(int index)
        {
            return RestrictionTypes.FirstOrDefault(view => view.Index == index) !;
        }

        /// <summary>
        /// Return restriction name.
        /// </summary>
        /// <returns>restriction name.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
