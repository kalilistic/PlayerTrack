using System.Collections.Generic;

namespace PlayerTrack
{
    /// <summary>
    /// Free Company from FCNameColor.
    /// </summary>
    public class FreeCompany
    {
        /// <summary>
        /// Gets or sets local player lodestone id.
        /// </summary>
        public string LocalPlayerLodestoneId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets free company lodestone id.
        /// </summary>
        public string FreeCompanyLodestoneId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets free company name.
        /// </summary>
        public string FreeCompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets free company homeworld Id.
        /// </summary>
        public ushort HomeWorldId { get; init; }

        /// <summary>
        /// Gets free company members.
        /// </summary>
        public List<FreeCompanyMember> FreeCompanyMembers { get; init; } = new();
    }
}
