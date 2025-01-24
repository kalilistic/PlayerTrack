using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Domain.Common;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public class TagService : CacheService<Tag>
{
    private PlayerFilter PlayerTagFilter = new();
    private List<string> TagNames = [];
    private List<string> TagNamesWithBlank = [string.Empty];

    public TagService()
    {
        ReloadTagCache();
    }

    public void CreateTag(string name)
    {
        Plugin.PluginLog.Verbose($"Entering TagService.CreateTag(): {name}");
        AddTagToCacheAndRepository(new Tag
        {
            Name = name,
            Color = Sheets.GetRandomUiColor().Id,
        });
    }

    public PlayerFilter GetTagFilters() =>
        PlayerTagFilter;

    public void UpdateTag(Tag tag) =>
        UpdateTagInCacheAndRepository(tag);

    public List<Tag> GetAllTags() =>
        Cache.Values.ToList();

    public Tag? GetTagByName(string name) =>
        Cache.Values.FirstOrDefault(tag => tag.Name == name);

    public Tag? GetTagById(int id) =>
        Cache.Values.FirstOrDefault(tag => tag.Id == id);

    public void DeleteTag(Tag tag)
    {
        Plugin.PluginLog.Verbose($"Entering TagService.DeleteTag(): {tag.Name}");
        PlayerTagService.DeletePlayerTagsByTagId(tag.Id);
        DeleteTagFromCacheAndRepository(tag);
    }

    private void UpdateTagInCacheAndRepository(Tag tag)
    {
        if (Cache.TryGetValue(tag.Id, out var existingValue))
            Cache.TryUpdate(tag.Id, tag, existingValue);

        RepositoryContext.TagRepository.UpdateTag(tag);
        ServiceContext.PlayerDataService.RefreshAllPlayers();
    }

    private void AddTagToCacheAndRepository(Tag tag)
    {
        tag.Id = RepositoryContext.TagRepository.CreateTag(tag);

        if (Cache.TryGetValue(tag.Id, out var existingValue))
            Cache.TryUpdate(tag.Id, tag, existingValue);

        ServiceContext.PlayerCacheService.AddTag(tag.Id);
    }

    private void DeleteTagFromCacheAndRepository(Tag tag)
    {
        Cache.TryRemove(tag.Id, out _);
        RepositoryContext.TagRepository.DeleteTag(tag.Id);
    }

    private void ReloadTagCache() =>
        ExecuteReloadCache(() =>
        {
            var tags = RepositoryContext.TagRepository.GetAllTags();
            if (tags == null)
            {
                Cache = new ConcurrentDictionary<int, Tag>();
                return;
            }

            var collection = new ConcurrentDictionary<int, Tag>(tags.ToDictionary(tag => tag.Id));

            Cache = collection;
            BuildTagFilters();
            BuildTagNames();
        });

    private void BuildTagNames()
    {
        Plugin.PluginLog.Verbose("Entering TagService.BuildTagNames()");
        var tags = GetAllTags();
        TagNames = tags.Select(cat => cat.Name).ToList();
        TagNamesWithBlank = new List<string> { string.Empty }.Concat(TagNames).ToList();
    }

    private void BuildTagFilters()
    {
        Plugin.PluginLog.Verbose("Entering TagService.BuildTagFilters()");
        var tagsByRank = GetAllTags();
        var totalCategories = tagsByRank.Count;

        var tagFilterIds = tagsByRank.Select(tag => tag.Id).Prepend(0).ToList();
        var tagFilterNames = tagsByRank.Select(tag => tag.Name).Prepend(string.Empty).ToList();

        PlayerTagFilter = new PlayerFilter
        {
            FilterIds = tagFilterIds,
            FilterNames = tagFilterNames,
            TotalFilters = totalCategories,
        };
    }
}
