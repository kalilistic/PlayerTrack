using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

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
    /// GetPlayerLodestoneId.
    /// </summary>
    public const string LabelProviderGetPlayerLodestoneId = "PlayerTrack.GetPlayerLodestoneId";

    /// <summary>
    /// GetPlayerNotes.
    /// </summary>
    public const string LabelProviderGetPlayerNotes = "PlayerTrack.GetPlayerNotes";

    /// <summary>
    /// GetPlayerPreviousNames.
    /// </summary>
    public const string LabelProviderGetPlayerPreviousNames = "PlayerTrack.GetPlayerPreviousNames";

    /// <summary>
    /// GetPlayerPreviousWorlds.
    /// </summary>
    public const string LabelProviderGetPlayerPreviousWorlds = "PlayerTrack.GetPlayerPreviousWorlds";
    /// <summary>
    /// GetPlayerPreviousWorlds.
    /// </summary>
    public const string LabelProviderGetPlayersPreviousNamesWorlds = "PlayerTrack.GetPlayersPreviousNamesWorlds";

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
    /// GetPlayerLodestoneId.
    /// </summary>
    public readonly ICallGateProvider<string, uint, uint>? ProviderGetPlayerLodestoneId;

    /// <summary>
    /// GetPlayerNotes.
    /// </summary>
    public readonly ICallGateProvider<string, uint, string>? ProviderGetPlayerNotes;

    /// <summary>
    /// GetPlayerPreviousNames.
    /// </summary>
    public readonly ICallGateProvider<string, uint, string[]>? ProviderGetPlayerPreviousNames;

    /// <summary>
    /// GetPlayerPreviousWorlds.
    /// </summary>
    public readonly ICallGateProvider<string, uint, string[]>? ProviderGetPlayerPreviousWorlds;

    /// <summary>
    /// GetPlayersPreviousNamesWorlds.
    /// </summary>
    public readonly ICallGateProvider<(string, uint)[], ((string, uint), string[], uint[])[]>? ProviderGetPlayersPreviousNamesWorlds;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerTrackProvider"/> class.
    /// </summary>
    /// <param name="pluginInterface">plugin interface.</param>
    /// <param name="api">plugin api.</param>
    public PlayerTrackProvider(DalamudPluginInterface pluginInterface, IPlayerTrackAPI api)
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
            this.ProviderGetPlayerLodestoneId =
                pluginInterface.GetIpcProvider<string, uint, uint>(LabelProviderGetPlayerLodestoneId);
            this.ProviderGetPlayerLodestoneId.RegisterFunc(api.GetPlayerLodestoneId);
        }
        catch (Exception e) 
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerLodestoneId}:\n{e}");
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
            this.ProviderGetPlayerPreviousNames =
                pluginInterface.GetIpcProvider<string, uint, string[]>(LabelProviderGetPlayerPreviousNames);
            this.ProviderGetPlayerPreviousNames.RegisterFunc(api.GetPlayerPreviousNames);
        }
        catch (Exception e) 
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerPreviousNames}:\n{e}");
        }

        try 
        {
            this.ProviderGetPlayerPreviousWorlds =
                pluginInterface.GetIpcProvider<string, uint, string[]>(LabelProviderGetPlayerPreviousWorlds);
            this.ProviderGetPlayerPreviousWorlds.RegisterFunc(api.GetPlayerPreviousWorlds);
        }
        catch (Exception e) 
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayerPreviousWorlds}:\n{e}");
        }

        try
        {
            this.ProviderGetPlayersPreviousNamesWorlds =
                pluginInterface.GetIpcProvider<(string, uint)[], ((string, uint), string[], uint[])[]>(LabelProviderGetPlayersPreviousNamesWorlds);
            this.ProviderGetPlayersPreviousNamesWorlds.RegisterFunc(api.GetPlayersPreviousNamesWorlds);
        }
        catch (Exception e)
        {
            DalamudContext.PluginLog.Error($"Error registering IPC provider for {LabelProviderGetPlayersPreviousNamesWorlds}:\n{e}");
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
        this.ProviderGetPlayerPreviousNames?.UnregisterFunc();
        this.ProviderGetPlayerPreviousWorlds?.UnregisterFunc();
        this.ProviderGetPlayersPreviousNamesWorlds?.UnregisterFunc();
    }
}
