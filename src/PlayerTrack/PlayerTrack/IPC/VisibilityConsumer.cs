using System;
using System.Collections.Generic;

using Dalamud.DrunkenToad;
using Dalamud.Plugin.Ipc;

namespace PlayerTrack
{
    /// <summary>
    /// IPC with Visibility Plugin.
    /// </summary>
    public class VisibilityConsumer
    {
        private const string RequiredVisibilityVersion = "1";
        private ICallGateSubscriber<string> consumerApiVersion = null!;
        private ICallGateSubscriber<IEnumerable<string>> consumerGetVoidListEntries = null!;
        private ICallGateSubscriber<string, uint, string, object> consumerAddToVoidList = null!;
        private ICallGateSubscriber<string, uint, object> consumerRemoveFromVoidList = null!;
        private ICallGateSubscriber<IEnumerable<string>> consumerGetWhiteListEntries = null!;
        private ICallGateSubscriber<string, uint, string, object> consumerAddToWhiteList = null!;
        private ICallGateSubscriber<string, uint, object> consumerRemoveFromWhiteList = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisibilityConsumer"/> class.
        /// </summary>
        public VisibilityConsumer()
            => this.Subscribe();

        /// <summary>
        /// Subscribe to visibility plugin methods.
        /// </summary>
        public void Subscribe()
        {
            try
            {
                this.consumerApiVersion =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string>("Visibility.ApiVersion");

                this.consumerGetVoidListEntries =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>(
                        "Visibility.GetVoidListEntries");

                this.consumerAddToVoidList =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, uint, string, object>(
                        "Visibility.AddToVoidList");

                this.consumerRemoveFromVoidList =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, uint, object>(
                        "Visibility.RemoveFromVoidList");

                this.consumerGetWhiteListEntries =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>(
                        "Visibility.GetWhitelistEntries");

                this.consumerAddToWhiteList =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, uint, string, object>(
                        "Visibility.AddToWhitelist");

                this.consumerRemoveFromWhiteList =
                    PlayerTrackPlugin.PluginInterface.GetIpcSubscriber<string, uint, object>(
                        "Visibility.RemoveFromWhitelist");
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"Failed to subscribe to Visibility.:\n{ex}");
            }
        }

        /// <summary>
        /// Get void list entries.
        /// </summary>
        /// <returns>list of void entries.</returns>
        public IEnumerable<string> GetVoidListEntries()
        {
            return this.consumerGetVoidListEntries.InvokeFunc();
        }

        /// <summary>
        /// Adds entry to VoidList.
        /// </summary>
        /// <param name="name">Full player name.</param>
        /// <param name="worldId">World ID.</param>
        /// <param name="reason">Reason for adding.</param>
        public void AddToVoidList(string name, uint worldId, string reason)
        {
            Logger.LogDebug("Adding " + name);
            this.consumerAddToVoidList.InvokeAction(name, worldId, reason);
        }

        /// <summary>
        /// Removes entry from VoidList.
        /// </summary>
        /// <param name="name">Full player name.</param>
        /// <param name="worldId">World ID.</param>
        public void RemoveFromVoidList(string name, uint worldId)
        {
            Logger.LogDebug("Removing " + name);
            this.consumerRemoveFromVoidList.InvokeAction(name, worldId);
        }

        /// <summary>
        /// Fetch all entries from WhiteList.
        /// </summary>
        /// <returns>A collection of strings in the form of (name worldId reason).</returns>
        public IEnumerable<string> GetWhiteListEntries()
        {
            return this.consumerGetWhiteListEntries.InvokeFunc();
        }

        /// <summary>
        /// Adds entry to WhiteList.
        /// </summary>
        /// <param name="name">Full player name.</param>
        /// <param name="worldId">World ID.</param>
        /// <param name="reason">Reason for adding.</param>
        public void AddToWhiteList(string name, uint worldId, string reason)
        {
            Logger.LogDebug("Adding " + name);
            this.consumerAddToWhiteList.InvokeAction(name, worldId, reason);
        }

        /// <summary>
        /// Removes entry from WhiteList.
        /// </summary>
        /// <param name="name">Full player name.</param>
        /// <param name="worldId">World ID.</param>
        public void RemoveFromWhiteList(string name, uint worldId)
        {
            Logger.LogDebug("Removing " + name);
            this.consumerRemoveFromWhiteList.InvokeAction(name, worldId);
        }

        /// <summary>
        /// Check if visibility is available.
        /// </summary>
        /// <returns>Gets indicator whether visibility is available.</returns>
        public bool IsAvailable()
        {
            try
            {
                var version = this.consumerApiVersion.InvokeFunc();
                return version.Equals(RequiredVisibilityVersion);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
