using Dalamud.DrunkenToad.Gui.Interfaces;
using Dalamud.Logging;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class ConfigService
{
    private PluginConfig pluginConfig = null!;

    public ConfigService() => this.ReloadCache();

    public void SaveConfig(IPluginConfig config)
    {
        PluginLog.LogVerbose("Entering ConfigService.SaveConfig()");
        var updatedPluginConfig = (PluginConfig)config;
        PluginLog.LogVerbose($"Saving config with playerConfig of type: {updatedPluginConfig.PlayerConfig.PlayerConfigType}");
        RepositoryContext.PlayerConfigRepository.UpdatePlayerConfig(updatedPluginConfig.PlayerConfig);
        if (RepositoryContext.ConfigRepository.UpdatePluginConfig(updatedPluginConfig))
        {
            this.pluginConfig = updatedPluginConfig;
        }
    }

    public PluginConfig GetConfig() => this.pluginConfig;

    public void ReloadCache()
    {
        PluginLog.LogVerbose("Entering ConfigService.ReloadCache()");
        var config = RepositoryContext.ConfigRepository.GetPluginConfig();
        if (config == null)
        {
            PluginLog.LogVerbose($"Creating default config.");
            this.pluginConfig = new PluginConfig();
            this.SaveConfig(this.pluginConfig);
            this.pluginConfig.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(this.pluginConfig.PlayerConfig);
        }
        else
        {
            var playerConfig = RepositoryContext.PlayerConfigRepository.GetDefaultPlayerConfig();
            if (playerConfig == null)
            {
                PluginLog.LogVerbose("Player config not found, creating default.");
                config.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(config.PlayerConfig);
            }
            else
            {
                PluginLog.LogVerbose($"Player config found with id {playerConfig.Id}.");
                config.PlayerConfig = playerConfig;
            }

            this.pluginConfig = config;
        }
    }
}
