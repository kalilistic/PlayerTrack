using Dalamud.DrunkenToad.Gui.Interfaces;

using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using Dalamud.DrunkenToad.Core;
using Dalamud.Interface;

public class ConfigService
{
    private PluginConfig pluginConfig = null!;

    public ConfigService() => this.ReloadCache();

    public void SaveConfig(IPluginConfig config)
    {
        DalamudContext.PluginLog.Verbose("Entering ConfigService.SaveConfig()");
        var updatedPluginConfig = (PluginConfig)config;
        DalamudContext.PluginLog.Verbose($"Saving config with playerConfig of type: {updatedPluginConfig.PlayerConfig.PlayerConfigType}");
        RepositoryContext.PlayerConfigRepository.UpdatePlayerConfig(updatedPluginConfig.PlayerConfig);
        if (RepositoryContext.ConfigRepository.UpdatePluginConfig(updatedPluginConfig))
        {
            this.pluginConfig = updatedPluginConfig;
        }
    }

    public void SyncIcons()
    {
        var icons = RepositoryContext.PlayerConfigRepository.GetDistinctIcons();
        if (icons.Count == 0)
        {
            return;
        }

        var existingIcons = this.pluginConfig.Icons;
        foreach (var icon in icons)
        {
            if (!existingIcons.Contains((FontAwesomeIcon)icon.Value))
            {
                existingIcons.Add((FontAwesomeIcon)icon.Value);
            }
        }

        this.pluginConfig.Icons = existingIcons;
        this.SaveConfig(this.pluginConfig);
    }

    public PluginConfig GetConfig() => this.pluginConfig;

    private void ReloadCache()
    {
        DalamudContext.PluginLog.Verbose("Entering ConfigService.ReloadCache()");
        var config = RepositoryContext.ConfigRepository.GetPluginConfig();
        if (config == null)
        {
            DalamudContext.PluginLog.Verbose($"Creating default config.");
            this.pluginConfig = new PluginConfig();
            this.SaveConfig(this.pluginConfig);
            this.pluginConfig.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(this.pluginConfig.PlayerConfig);
        }
        else
        {
            var playerConfig = RepositoryContext.PlayerConfigRepository.GetDefaultPlayerConfig();
            if (playerConfig == null)
            {
                DalamudContext.PluginLog.Verbose("Player config not found, creating default.");
                config.PlayerConfig.Id = RepositoryContext.PlayerConfigRepository.CreatePlayerConfig(config.PlayerConfig);
            }
            else
            {
                DalamudContext.PluginLog.Verbose($"Player config found with id {playerConfig.Id}.");
                config.PlayerConfig = playerConfig;
            }

            this.pluginConfig = config;
        }
    }
}
