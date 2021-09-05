using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <inheritdoc />
    public class BaseRepository : Repository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository"/> class.
        /// </summary>
        /// <param name="pluginFolder">plugin folder.</param>
        public BaseRepository(string pluginFolder)
            : base(pluginFolder)
        {
        }

        /// <summary>
        /// Get schema version.
        /// </summary>
        /// <returns>version.</returns>
        public int GetSchemaVersion()
        {
            return this.GetVersion();
        }

        /// <summary>
        /// Set schema version.
        /// </summary>
        /// <param name="version">version.</param>
        public void SetSchemaVersion(int version)
        {
            this.SetVersion(version);
        }
    }
}
