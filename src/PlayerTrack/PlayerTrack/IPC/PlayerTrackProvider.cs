using System;

using Dalamud.DrunkenToad;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace PlayerTrack
{
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
        /// API.
        /// </summary>
        public readonly IPlayerTrackAPI API;

        /// <summary>
        /// ProviderAPIVersion.
        /// </summary>
        public ICallGateProvider<int>? ProviderAPIVersion;

        /// <summary>
        /// GetPlayerCurrentNameWorld.
        /// </summary>
        public ICallGateProvider<string, uint, string>? ProviderGetPlayerCurrentNameWorld;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerTrackProvider"/> class.
        /// </summary>
        /// <param name="pluginInterface">plugin interface.</param>
        /// <param name="api">plugin api.</param>
        public PlayerTrackProvider(DalamudPluginInterface pluginInterface, IPlayerTrackAPI api)
        {
            this.API = api;

            try
            {
                this.ProviderAPIVersion = pluginInterface.GetIpcProvider<int>(LabelProviderApiVersion);
                this.ProviderAPIVersion.RegisterFunc(() => api.APIVersion);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error registering IPC provider for {LabelProviderApiVersion}:\n{ex}");
            }

            try
            {
                this.ProviderGetPlayerCurrentNameWorld =
                    pluginInterface.GetIpcProvider<string, uint, string>(LabelProviderGetPlayerCurrentNameWorld);
                this.ProviderGetPlayerCurrentNameWorld.RegisterFunc(api.GetPlayerCurrentNameWorld);
            }
            catch (Exception e)
            {
                Logger.LogError($"Error registering IPC provider for {LabelProviderGetPlayerCurrentNameWorld}:\n{e}");
            }
        }

        /// <summary>
        /// Dispose IPC.
        /// </summary>
        public void Dispose()
        {
            this.ProviderAPIVersion?.UnregisterFunc();
            this.ProviderGetPlayerCurrentNameWorld?.UnregisterFunc();
        }
    }
}
