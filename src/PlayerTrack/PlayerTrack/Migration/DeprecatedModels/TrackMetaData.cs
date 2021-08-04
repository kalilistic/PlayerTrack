#pragma warning disable CS1591
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8618
#pragma warning disable CS8625
#pragma warning disable SA1003
#pragma warning disable SA1009
#pragma warning disable SA1101
#pragma warning disable SA1134
#pragma warning disable SA1204
#pragma warning disable SA1309
#pragma warning disable SA1413
#pragma warning disable SA1516
#pragma warning disable SA1600

using System;

// ReSharper disable All
namespace PlayerTrack
{
    /// <summary>
    /// Meta data.
    /// </summary>
    [Obsolete]
    public class TrackMetaData
    {
        /// <summary>
        /// Compressed state.
        /// </summary>
        public bool Compressed;

        /// <summary>
        /// Schema version.
        /// </summary>
        public int SchemaVersion;
    }
}
