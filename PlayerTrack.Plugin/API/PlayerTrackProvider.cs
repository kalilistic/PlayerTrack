using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

// ReSharper disable InconsistentNaming
namespace PlayerTrack.API;

using Dalamud.DrunkenToad.Core;

/// <summary>
/// IPC for PlayerTrack plugin.
/// </summary>
public class PlayerTrackProvider
{
    /// <summary>
    /// API Version.
    /// </summary>
    public const string LabelProviderApiVersion = "PlayerTrack.APIVersion";

    /// <summary>
    /// GetPlayerCurrentNameWorld.
    /// </summary>
    public const string LabelProviderGetPlayerCurrentNameWorld = "PlayerTrack.GetPlayerCurrentNameWorld";

    /// <summary>
    /// GetPlayerNotes.
    /// </summary>
    public const string LabelProviderGetPlayerNotes = "PlayerTrack.GetPlayerNotes";

    /// <summary>
    /// GetPlayerNameWorldHistories.
    /// </summary>
    public const string LabelProviderGetPlayerNameWorldHistories = "PlayerTrack.GetPlayerNameWorldHistories";

    /// <summary>
    /// API.
    /// </summary>
    public readonly IPlayerTrackAPI API;

    /// <summary>
    /// ProviderAPIVersion.
    /// </summary>
    public readonly ICallGateProvider<int>? ProviderAPIVersion;

    /// <summary>
    /// GetPlayerCurrentNameWorld.
    /// </summary>
    public readonly ICallGateProvider<string, uint, string>? ProviderGetPlayerCurrentNameWorld;

    /// <summary>
    /// GetPlayerNotes.
    /// </summary>
    public readonly ICallGateProvider<string, uint, string>? ProviderGetPlayerNotes;

    /// <summary>
    /// GetUniquePlayerNameWorldHistories.
    /// </summary>
    public readonly ICallGateProvider<(string, uint)[], ((string, uint), (string, uint)[])[]>? GetPlayerNameWorldHistories;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTrackProvider"/> class.
    /// </summary>
    /// <param name="pluginInterface">plugin interface.</param>
    /// <param name="api">plugin api.</param>
    public PlayerTrackProvider(IDalamudPluginInterface pluginInterface, IPlayerTrackAPI api)
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerTrackProvider");
        this.API = api;

        try
        {
            this.ProviderAPIVersion = pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
            this.ProviderAPIVersion.RegisterFunc(() => api.APIVersion);
        }
        catch (Exception ex)
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderApiVersion}:\n{ex}");
        }

        try
        {
            this.ProviderGetPlayerCurrentNameWorld =
                pluginInterface.GetIpcProvider<string, uint, string>(LabelProviderGetPlayerCurrentNameWorld);
            this.ProviderGetPlayerCurrentNameWorld.RegisterFunc(api.GetPlayerCurrentNameWorld);
        }
        catch (Exception e)
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerCurrentNameWorld}:\n{e}");
        }

        try
        {
            this.ProviderGetPlayerNotes =
                pluginInterface.GetIpcProvider<string, uint, string>(LabelProviderGetPlayerNotes);
            this.ProviderGetPlayerNotes.RegisterFunc(api.GetPlayerNotes);
        }
        catch (Exception e)
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerNotes}:\n{e}");
        }

        try
        {
            this.GetPlayerNameWorldHistories =
                pluginInterface.GetIpcProvider<(string, uint)[], ((string, uint), (string, uint)[])[]>(LabelProviderGetPlayerNameWorldHistories);
            this.GetPlayerNameWorldHistories.RegisterFunc(api.GetPlayerNameWorldHistories);
        }
        catch (Exception e)
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerNameWorldHistories}:\n{e}");
        }
    }

    /// <summary>
    /// Dispose IPC.
    /// </summary>
    public void Dispose()
    {
        DalamudContext.PluginLog.Verbose("Entering PlayerTrackProvider.Dispose");
        this.ProviderAPIVersion?.UnregisterFunc();
        this.ProviderGetPlayerCurrentNameWorld?.UnregisterFunc();
        this.ProviderGetPlayerNotes?.UnregisterFunc();
        this.GetPlayerNameWorldHistories?.UnregisterFunc();
    }
}
