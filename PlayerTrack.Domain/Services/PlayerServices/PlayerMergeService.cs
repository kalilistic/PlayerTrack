namespace PlayerTrack.Domain;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Logging;
using Infrastructure;
using Models;

public class PlayerMergeService
{
    public static void HandleDuplicatePlayers(List<Player> players)
    {
        PluginLog.LogVerbose($"Entering PlayerMergeService.HandleDuplicatePlayers(): {players.Count}");
        if (players.Count < 2)
        {
            return;
        }

        var sortedPlayers = new List<Player>(
            players.OrderBy(p => p.LodestoneVerifiedOn)
                .ThenBy(p => p.Created)
                .ThenBy(p => p.Id));
        var oldestPlayer = sortedPlayers[0];
        var newestPlayers = sortedPlayers.Skip(1).ToList();

        foreach (var newPlayer in newestPlayers)
        {
            // create records
            PlayerChangeService.HandleNameWorldChange(oldestPlayer, newPlayer);
            PlayerChangeService.HandleCustomizeChange(oldestPlayer, newPlayer);

            // re-parent records
            PlayerChangeService.UpdatePlayerId(newPlayer.Id, oldestPlayer.Id);
            PlayerEncounterService.UpdatePlayerId(newPlayer.Id, oldestPlayer.Id);

            // delete records
            PlayerConfigService.DeletePlayerConfig(newPlayer.Id);
            PlayerCategoryService.DeletePlayerCategoryByPlayerId(newPlayer.Id);
            PlayerTagService.DeletePlayerTagsByPlayerId(newPlayer.Id);

            // update player records
            oldestPlayer.Merge(newPlayer);
            ServiceContext.PlayerDataService.DeletePlayer(newPlayer.Id);
            ServiceContext.PlayerDataService.UpdatePlayer(oldestPlayer);
        }
    }

    public static void CheckForDuplicates() => Task.Run(() =>
    {
        PluginLog.LogVerbose("Entering PlayerMergeService.CheckForDuplicates()");
        var allPlayers = ServiceContext.PlayerDataService.GetAllPlayers();
        var groupedPlayers = allPlayers.Where(p => p.LodestoneId > 0)
            .GroupBy(p => p.LodestoneId);

        foreach (var group in groupedPlayers)
        {
            HandleDuplicatePlayers(group.ToList());
        }
    });

    public static void CheckForDuplicates(Player player) => Task.Run(() =>
    {
        PluginLog.LogVerbose($"Entering PlayerMergeService.CheckForDuplicates(): {player.Id}");
        if (player.LodestoneId > 0)
        {
            var players = RepositoryContext.PlayerRepository.GetPlayersByLodestoneId(player.LodestoneId) ?? new List<Player>();
            HandleDuplicatePlayers(players);
        }
    });
}
