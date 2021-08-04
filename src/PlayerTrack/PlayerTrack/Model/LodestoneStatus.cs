using System;

namespace PlayerTrack
{
    /// <summary>
    /// Lodestone status.
    /// </summary>
    public enum LodestoneStatus
    {
        /// <summary>
        /// Unverified: not looked up on lodestone.
        /// </summary>
        Unverified = 0,

        /// <summary>
        /// Verifying: waiting on response from lodestone.
        /// </summary>
        [ObsoleteAttribute]
        Verifying = 1,

        /// <summary>
        /// Verified: confirmed lodestone id.
        /// </summary>
        Verified = 2,

        /// <summary>
        /// Updating: waiting on update response from lodestone.
        /// </summary>
        [ObsoleteAttribute]
        Updating = 3,

        /// <summary>
        /// Updated: updated successfully from lodestone.
        /// </summary>
        [ObsoleteAttribute]
        Updated = 4,

        /// <summary>
        /// Failed: unable to confirm lodestone id.
        /// </summary>
        Failed = 5,
    }
}
