using Dalamud.Interface;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class ConfigService
{
    private PluginConfig PluginConfig = null!;

    public ConfigService()
    {
        ReloadCache();
    }

    public void SaveConfig(IPluginConfig config)
    {
        Plugin.PluginLog.Verbose("Entering ConfigService.SaveConfig()");
        var updatedPluginConfig = (PluginConfig)config;

        Plugin.PluginLog.Verbose($"Saving config with playerConfig of type: {updatedPluginConfig.PlayerConfig.PlayerConfigType}");
        RepositoryContext.PlayerConfigRepository.UpdatePlayerConfig(updatedPluginConfig.PlayerConfig);
        if (RepositoryContext.ConfigRepository.UpdatePluginConfig(updatedPluginConfig))
            PluginConfig = updatedPluginConfig;
    }

    public void SyncIcons()
    {
        var icons = RepositoryContext.PlayerConfigRepository.GetDistinctIcons();
        if (icons.Count == 0)
            return;

        var existingIcons = PluginConfig.Icons;
        foreach (var icon in icons)
            if (!existingIcons.Contains((FontAwesomeIcon)icon.Value))
                existingIcons.Add((FontAwesomeIcon)icon.Value);

        PluginConfig.Icons = existingIcons;
        SaveConfig(PluginConfig);
    }

    public PluginConfig GetConfig() =>
        PluginConfig;

    private void ReloadCache()
    {
        Plugin.PluginLog.Verbose("Entering ConfigService.ReloadCache()");
        var config = RepositoryContext.ConfigRepository.GetPluginConfig();
        if (config == null)
        {
            Plugin.PluginLog.Verbose("Creating default config.");
            PluginConfig = new PluginConfig();

            SaveConfig(PluginConfig);
            PluginConfig.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(PluginConfig.PlayerConfig);
        }
        else
        {
            var playerConfig = RepositoryContext.PlayerConfigRepository.GetDefaultPlayerConfig();
            if (playerConfig == null)
            {
                Plugin.PluginLog.Verbose("Player config not found, creating default.");
                config.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(config.PlayerConfig);
            }
            else
            {
                Plugin.PluginLog.Verbose($"Player config found with id {playerConfig.Id}.");
                config.PlayerConfig = playerConfig;
            }

            PluginConfig = config;
        }
    }
}
