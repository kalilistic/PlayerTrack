using System;
using System.Linq;

namespace PlayerTrack
{
    /// <inheritdoc cref="PlayerTrack.IPlayerTrackAPI" />
    public class PlayerTrackAPI : IPlayerTrackAPI
    {
        private readonly bool initialized;
        private readonly PlayerTrackPlugin plugin;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerTrackAPI"/> class.
        /// </summary>
        /// <param name="plugin">playertrack plugin.</param>
        public PlayerTrackAPI(PlayerTrackPlugin plugin)
        {
            this.plugin = plugin;
            this.initialized = true;
        }

        /// <inheritdoc />
        public int APIVersion => 1;

        /// <inheritdoc />
        public string GetPlayerCurrentNameWorld(string name, uint worldId)
        {
            this.CheckInitialized();
            var player =
                this.plugin.PlayerService.GetPlayers()?.Where(
                    pair => pair.Value.Names.Contains(name) &&
                    pair.Value.GetWorldIds().Contains(worldId)).ToList();
            if (player is not { Count: 1 })
            {
                return $"{name} {worldId}";
            }

            return $"{player.First().Value.Names.First()} {player.First().Value.HomeWorlds.First()}";
        }

        private void CheckInitialized()
        {
            if (!this.initialized)
            {
                throw new Exception("API is not initialized.");
            }
        }
    }
}
