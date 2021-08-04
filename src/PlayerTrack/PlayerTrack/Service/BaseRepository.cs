using Dalamud.DrunkenToad;

namespace PlayerTrack
{
    /// <inheritdoc />
    public class BaseRepository : Repository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository"/> class.
        /// </summary>
        /// <param name="pluginService">plugin service.</param>
        public BaseRepository(PluginService pluginService)
            : base(pluginService)
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
