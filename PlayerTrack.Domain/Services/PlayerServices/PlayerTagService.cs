using System.Collections.Generic;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using Dalamud.Logging;

public class PlayerTagService
{
    public static void UpdateTags(int playerId, List<Tag> tags)
    {
        PluginLog.LogVerbose($"Entering PlayerTagService.UpdateTags(), playerId: {playerId}, tags: {tags.Count}");
        var player = ServiceContext.PlayerDataService.GetPlayer(playerId);
        if (player == null)
        {
            PluginLog.LogWarning("Player not found, cannot update tags.");
            return;
        }

        player.AssignedTags = tags;
        ServiceContext.PlayerDataService.UpdatePlayer(player);
    }

    public static void RemoveTag(int playerId, int tagId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagService.RemoveTag(), playerId: {playerId}, tagId: {tagId}");
        var tags = ServiceContext.PlayerDataService.GetPlayer(playerId)?.AssignedTags;
        if (tags == null || tags.Count == 0)
        {
            PluginLog.LogWarning("Player not found, cannot remove tag.");
            return;
        }

        tags.RemoveAll(t => t.Id == tagId);
        UpdateTags(playerId, tags);
        RepositoryContext.PlayerTagRepository.DeletePlayerTag(playerId, tagId);
    }

    public static void AssignTag(int playerId, int tagId)
    {
        PluginLog.LogVerbose($"Entering PlayerTagService.AssignTag(), playerId: {playerId}, tagId: {tagId}");
        var tag = ServiceContext.TagService.GetTagById(tagId);
        if (tag == null)
        {
            PluginLog.LogWarning("Tag not found, cannot assign tag.");
            return;
        }

        var tags = ServiceContext.PlayerDataService.GetPlayer(playerId)?.AssignedTags ?? new List<Tag>();
        tags.Add(tag);
        UpdateTags(playerId, tags);
        RepositoryContext.PlayerTagRepository.CreatePlayerTag(playerId, tagId);
    }

    public static void DeletePlayerTagsByTagId(int tagId) => RepositoryContext.PlayerTagRepository.DeletePlayerTag(tagId);

    public static void DeletePlayerTagsByPlayerId(int playerId) => RepositoryContext.PlayerTagRepository.DeletePlayerTagByPlayerId(playerId);
}
