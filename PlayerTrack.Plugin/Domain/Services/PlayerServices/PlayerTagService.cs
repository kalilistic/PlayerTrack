using System.Collections.Generic;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class PlayerTagService
{
    public static void UpdateTags(int playerId, List<Tag> tags)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagService.UpdateTags(), playerId: {playerId}, tags: {tags.Count}");
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            Plugin.PluginLog.Warning("Player not found, cannot update tags.");
            return;
        }

        player.AssignedTags = tags;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
    }

    public static void UnassignTagsFromPlayer(int playerId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagService.UnassignTagsFromPlayer(), playerId: {playerId}");
        var tags = ServiceContext.PlayerDataService.GetPlayer(playerId)?.AssignedTags;
        if (tags == null || tags.Count == 0)
        {
            Plugin.PluginLog.Warning("Player not found, cannot remove tag.");
            return;
        }

        tags = [];
        UpdateTags(playerId, tags);
        RepositoryContext.PlayerTagRepository.DeletePlayerTagByPlayerId(playerId);
    }

    public static void UnassignTagFromPlayer(int playerId, int tagId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagService.UnassignTagFromPlayer(), playerId: {playerId}, tagId: {tagId}");
        var tags = ServiceContext.PlayerDataService.GetPlayer(playerId)?.AssignedTags;
        if (tags == null || tags.Count == 0)
        {
            Plugin.PluginLog.Warning("Player not found, cannot remove tag.");
            return;
        }

        tags.RemoveAll(t => t.Id == tagId);
        UpdateTags(playerId, tags);
        RepositoryContext.PlayerTagRepository.DeletePlayerTag(playerId, tagId);
    }

    public static void AssignTag(int playerId, int tagId)
    {
        Plugin.PluginLog.Verbose($"Entering PlayerTagService.AssignTag(), playerId: {playerId}, tagId: {tagId}");
        var tag = ServiceContext.TagService.GetTagById(tagId);
        if (tag == null)
        {
            Plugin.PluginLog.Warning("Tag not found, cannot assign tag.");
            return;
        }

        var tags = ServiceContext.PlayerDataService.GetPlayer(playerId)?.AssignedTags ?? [];
        tags.Add(tag);
        UpdateTags(playerId, tags);
        RepositoryContext.PlayerTagRepository.CreatePlayerTag(playerId, tagId);
    }

    public static void DeletePlayerTagsByTagId(int tagId) =>
        RepositoryContext.PlayerTagRepository.DeletePlayerTag(tagId);

    public static void DeletePlayerTagsByPlayerId(int playerId) =>
        RepositoryContext.PlayerTagRepository.DeletePlayerTagByPlayerId(playerId);
}
