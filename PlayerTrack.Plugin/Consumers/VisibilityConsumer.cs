using System;
using System.Collections.Generic;
using Dalamud.Plugin.Ipc;

namespace PlayerTrack.Consumers;

/// <summary>
/// IPC with Visibility Plugin.
/// </summary>
public class VisibilityConsumer
{
    private const string RequiredVisibilityVersion = "1";
    private ICallGateSubscriber<string, uint, string, object> ConsumerAddToVoidList = null!;
    private ICallGateSubscriber<string, uint, string, object> ConsumerAddToWhiteList = null!;
    private ICallGateSubscriber<string> ConsumerApiVersion = null!;
    private ICallGateSubscriber<IEnumerable<string>> ConsumerGetVoidListEntries = null!;
    private ICallGateSubscriber<IEnumerable<string>> ConsumerGetWhiteListEntries = null!;
    private ICallGateSubscriber<string, uint, object> ConsumerRemoveFromVoidList = null!;
    private ICallGateSubscriber<string, uint, object> ConsumerRemoveFromWhiteList = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisibilityConsumer" /> class.
    /// </summary>
    public VisibilityConsumer()
    {
        Subscribe();
    }

    /// <summary>
    /// Subscribe to visibility plugin methods.
    /// </summary>
    public void Subscribe()
    {
        try
        {
            ConsumerApiVersion = Plugin.PluginInterface.GetIpcSubscriber<string>("Visibility.ApiVersion");
            ConsumerGetVoidListEntries = Plugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>("Visibility.GetVoidListEntries");
            ConsumerAddToVoidList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, string, object>("Visibility.AddToVoidList");
            ConsumerRemoveFromVoidList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, object>("Visibility.RemoveFromVoidList");
            ConsumerGetWhiteListEntries = Plugin.PluginInterface.GetIpcSubscriber<IEnumerable<string>>("Visibility.GetWhitelistEntries");
            ConsumerAddToWhiteList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, string, object>("Visibility.AddToWhitelist");
            ConsumerRemoveFromWhiteList = Plugin.PluginInterface.GetIpcSubscriber<string, uint, object>("Visibility.RemoveFromWhitelist");
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Verbose($"Failed to subscribe to Visibility.:\n{ex}");
        }
    }

    /// <summary>
    /// Get void list entries.
    /// </summary>
    /// <returns>list of void entries.</returns>
    public IEnumerable<string> GetVoidListEntries() => ConsumerGetVoidListEntries.InvokeFunc();

    /// <summary>
    /// Adds entry to VoidList.
    /// </summary>
    /// <param name="name">Full player name.</param>
    /// <param name="worldId">World ID.</param>
    /// <param name="reason">Reason for adding.</param>
    public void AddToVoidList(string name, uint worldId, string reason)
    {
        Plugin.PluginLog.Verbose($"Adding {name}");
        ConsumerAddToVoidList.InvokeAction(name, worldId, reason);
    }

    /// <summary>
    /// Removes entry from VoidList.
    /// </summary>
    /// <param name="name">Full player name.</param>
    /// <param name="worldId">World ID.</param>
    public void RemoveFromVoidList(string name, uint worldId)
    {
        Plugin.PluginLog.Verbose($"Removing {name}");
        ConsumerRemoveFromVoidList.InvokeAction(name, worldId);
    }

    /// <summary>
    /// Fetch all entries from WhiteList.
    /// </summary>
    /// <returns>A collection of strings in the form of (name worldId reason).</returns>
    public IEnumerable<string> GetWhiteListEntries() => ConsumerGetWhiteListEntries.InvokeFunc();

    /// <summary>
    /// Adds entry to WhiteList.
    /// </summary>
    /// <param name="name">Full player name.</param>
    /// <param name="worldId">World ID.</param>
    /// <param name="reason">Reason for adding.</param>
    public void AddToWhiteList(string name, uint worldId, string reason)
    {
        Plugin.PluginLog.Verbose($"Adding {name}");
        ConsumerAddToWhiteList.InvokeAction(name, worldId, reason);
    }

    /// <summary>
    /// Removes entry from WhiteList.
    /// </summary>
    /// <param name="name">Full player name.</param>
    /// <param name="worldId">World ID.</param>
    public void RemoveFromWhiteList(string name, uint worldId)
    {
        Plugin.PluginLog.Verbose($"Removing {name}");
        ConsumerRemoveFromWhiteList.InvokeAction(name, worldId);
    }

    /// <summary>
    /// Check if visibility is available.
    /// </summary>
    /// <returns>Gets indicator whether visibility is available.</returns>
    public bool IsAvailable()
    {
        try
        {
            var version = ConsumerApiVersion.InvokeFunc();
            return version.Equals(RequiredVisibilityVersion, StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
