using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Utility;
using NetStone;
using NetStone.Search.Character;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class LodestoneService
{
    private const string LodestoneBaseUrl = "https://{0}.finalfantasyxiv.com/lodestone/character/";

    private LodestoneClient LodestoneClient = null!;
    private bool IsStarted;

    public void Start()
    {
        SetupClient().Wait();
    }

    // Replaced with void to ignore compiler warning CS4014 in
    // PlayerTracker.UserInterface.Main.Components.PlayerListComponent.DrawPlayer
    // Line-in-method: 75
    public void OpenLodestoneProfile(string playerName, uint worldId)
    {
        if (!IsStarted)
            return;

        try
        {
            var worldName = Sheets.GetWorldNameById(worldId);
            var player = ServiceContext.PlayerDataService.GetPlayer(playerName, worldId);
            var lodestoneId = player?.LodestoneId ?? 0;
            var shouldUpdate = false;

            // Since the only part for asynchronous methods is at the call to method:
            // GetLodestoneIdAsync
            //
            // We can just have the rest of this method as a separate task. Because the call to:
            //
            // Plugin.DataManager.GetWorldNameById(uint)
            // and
            // ServiceContext.PlayerDataService.GetPlayer(string, uint)
            //
            // require to be ran on the same thread as the plugin's base thread.
            Task.Run(async () => {
                try
                {
                    if (lodestoneId == 0)
                    {
                        lodestoneId = await GetLodestoneIdAsync(playerName, worldName).ConfigureAwait(false);
                        shouldUpdate = true;
                    }

                    var lodestoneUrl = BuildLodestoneUrl(playerName, worldName, lodestoneId);

                    Dalamud.Utility.Util.OpenLink(lodestoneUrl);
                    if (player != null && shouldUpdate && lodestoneId > 0)
                    {
                        player.LodestoneId = lodestoneId;
                        player.LodestoneStatus = LodestoneStatus.Verified;
                        player.LodestoneVerifiedOn = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                        ServiceContext.PlayerDataService.UpdatePlayer(player);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.PluginLog.Error(ex, "Failed to open lodestone profile");
                }
            });
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to open lodestone profile");
        }
    }

    private async Task<uint> GetLodestoneIdAsync(string playerName, string worldName)
    {
        try
        {
            var searchResponse = await LodestoneClient.SearchCharacter(new CharacterSearchQuery
            {
                CharacterName = playerName,
                World = worldName
            }).ConfigureAwait(false);

            if (searchResponse == null)
            {
                Plugin.PluginLog.Warning($"Failed to search lodestone for {playerName}@{worldName}");
                return 0;
            }

            var result = searchResponse.Results.FirstOrDefault(entry => entry.Name == playerName);
            return result != null ? Convert.ToUInt32(result.Id) : 0;
        }
        catch (HttpRequestException ex)
        {
            Plugin.PluginLog.Error(ex, $"Failed to search lodestone for {playerName}@{worldName}");
            return 0;
        }
    }

    private static string BuildLodestoneUrl(string playerName, string worldName, uint lodestoneId)
    {
        var baseUrl = LodestoneBaseUrl.Format(ServiceContext.ConfigService.GetConfig().LodestoneLocale);
        return lodestoneId > 0 ? $"{baseUrl}{lodestoneId}" : $"{baseUrl}?q={playerName}&worldname={worldName}";
    }

    private async Task SetupClient()
    {
        try
        {
            LodestoneClient = await LodestoneClient.GetClientAsync().ConfigureAwait(false);
            IsStarted = true;
        }
        catch (HttpRequestException ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to setup lodestone client");
        }
    }
}
