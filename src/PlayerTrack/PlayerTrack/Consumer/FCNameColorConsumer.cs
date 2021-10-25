using System;
using System.Collections.Generic;

using Dalamud.DrunkenToad;
using Dalamud.Plugin.Ipc;

namespace PlayerTrack
{
    /// <summary>
    /// IPC with FCNameColor Plugin.
    /// </summary>
    public class FCNameColorConsumer
    {
        private const string RequiredFCNameColorVersion = "1";
        private ICallGateSubscriber<string> consumerApiVersion = null!;
        private ICallGateSubscriber<IEnumerable<string>> consumerGetLocalPlayers = null!;
        private ICallGateSubscriber<IEnumerable<string>> consumerGetPlayerFCs = null!;
        private ICallGateSubscriber<string, IEnumerable<string>> consumerGetFCMembers = null!;
        private ICallGateSubscriber<IEnumerable<string>> consumerGetIgnoredPlayers = null!;
        private ICallGateSubscriber<string, string, object> consumerAddPlayerToIgnoredPlayers = null!;
        private ICallGateSubscriber<string, object> consumerRemovePlayerFromIgnoredPlayers = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="FCNameColorConsumer"/> class.
        /// </summary>
        public FCNameColorConsumer()
            => this.Subscribe();

        /// <summary>
        /// Subscribe to FCNameColor plugin methods.
        /// </summary>
        public void Subscribe()
        {
            try
            {
                this.consumerApiVersion =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string>("FCNameColor.APIVersion");
                this.consumerGetLocalPlayers =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>(
                        "FCNameColor.GetLocalPlayers");
                this.consumerGetPlayerFCs =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>(
                        "FCNameColor.GetPlayerFCs");
                this.consumerGetFCMembers =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, IEnumerable<string>>(
                        "FCNameColor.GetFCMembers");
                this.consumerGetIgnoredPlayers =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>(
                        "FCNameColor.GetIgnoredPlayers");
                this.consumerAddPlayerToIgnoredPlayers =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, string, object>(
                        "FCNameColor.AddPlayerToIgnoredPlayers");
                this.consumerRemovePlayerFromIgnoredPlayers =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, object>(
                        "FCNameColor.RemovePlayerFromIgnoredPlayers");
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Failed to subscribe to FCNameColor.:\n{ex}");
            }
        }

        /// <summary>
        /// Check if FCNameColor is available.
        /// </summary>
        /// <returns>Gets indicator whether FCNameColor is available.</returns>
        public bool IsAvailable()
        {
            try
            {
                var version = this.consumerApiVersion.InvokeFunc();
                return version.Equals(RequiredFCNameColorVersion);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get all local players.
        /// </summary>
        /// <returns>A collection of strings in the form of (Name@Server PlayerID).</returns>
        public IEnumerable<string> GetLocalPlayers()
        {
            return this.consumerGetLocalPlayers.InvokeFunc();
        }

        /// <summary>
        /// Get Player FCs.
        /// </summary>
        /// <returns>A collection of strings in the form of (PlayerID FCID FCName).</returns>
        public IEnumerable<string> GetPlayerFCs()
        {
            return this.consumerGetPlayerFCs.InvokeFunc();
        }

        /// <summary>
        /// Get Player FCs.
        /// </summary>
        /// <param name="id">FC ID.</param>
        /// <returns>A collection of strings in the form of (PlayerID PlayerName).</returns>
        public IEnumerable<string> GetFCMembers(string id)
        {
            return this.consumerGetFCMembers.InvokeFunc(id);
        }

        /// <summary>
        /// Get ignored players list.
        /// </summary>
        /// <returns>A collection of strings in the form of (PlayerID PlayerName).</returns>
        public IEnumerable<string> GetIgnoredPlayers()
        {
           return this.consumerGetIgnoredPlayers.InvokeFunc();
        }

        /// <summary>
        /// Adds player to ignored list.
        /// </summary>
        /// <param name="id">player lodestone id.</param>
        /// <param name="name">Player name.</param>
        public void AddPlayerToIgnoredPlayers(string id, string name)
        {
            this.consumerAddPlayerToIgnoredPlayers.InvokeAction(id, name);
        }

        /// <summary>
        /// Removes player from ignored list.
        /// </summary>
        /// <param name="id">player lodestone id.</param>
        public void RemovePlayerFromIgnoredPlayers(string id)
        {
            this.consumerRemovePlayerFromIgnoredPlayers.InvokeAction(id);
        }
    }
}
