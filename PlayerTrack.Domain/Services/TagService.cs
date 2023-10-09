using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Linq;
using Dalamud.DrunkenToad.Caching;
using Dalamud.DrunkenToad.Collections;

public class TagService : UnsortedCacheService<Tag>
{
    private PlayerFilter playerTagFilter = new();
    private List<string> tagNames = new();
    private List<string> tagNamesWithBlank = new() { string.Empty };

    public TagService() => this.ReloadTagCache();

    public void CreateTag(string name)
    {
        DalamudContext.PluginLog.Verbose($"Entering TagService.CreateTag(): {name}");
        var tag = new Tag
        {
            Name = name,
            Color = DalamudContext.DataManager.GetRandomUIColor().Id,
        };
        this.AddTagToCacheAndRepository(tag);
    }

    public PlayerFilter GetTagFilters() => this.playerTagFilter;

    public void UpdateTag(Tag tag) => this.UpdateTagInCacheAndRepository(tag);

    public List<Tag> GetAllTags() => this.cache.GetAll();

    public Tag? GetTagByName(string name) => this.cache.FindFirst(tag => tag.Name == name);

    public Tag? GetTagById(int id) => this.cache.FindFirst(tag => tag.Id == id);

    public void DeleteTag(Tag tag)
    {
        DalamudContext.PluginLog.Verbose($"Entering TagService.DeleteTag(): {tag.Name}");
        PlayerTagService.DeletePlayerTagsByTagId(tag.Id);
        this.DeleteTagFromCacheAndRepository(tag);
    }

    public void RefreshTags()
    {
        DalamudContext.PluginLog.Verbose("Entering TagService.RefreshTags()");
        this.ReloadTagCache();
    }

    public List<string> GetTagNames(bool includeBlank = true)
    {
        if (includeBlank)
        {
            return this.tagNamesWithBlank;
        }

        return this.tagNames;
    }

    private void UpdateTagInCacheAndRepository(Tag tag)
    {
        this.cache.Update(tag.Id, tag);
        RepositoryContext.TagRepository.UpdateTag(tag);
        ServiceContext.PlayerDataService.RefreshAllPlayers();
        this.OnCacheUpdated();
    }

    private void AddTagToCacheAndRepository(Tag tag)
    {
        tag.Id = RepositoryContext.TagRepository.CreateTag(tag);
        this.cache.Add(tag.Id, tag);
        this.OnCacheUpdated();
    }

    private void DeleteTagFromCacheAndRepository(Tag tag)
    {
        this.cache.Remove(tag.Id);
        RepositoryContext.TagRepository.DeleteTag(tag.Id);
        this.OnCacheUpdated();
    }

    private void ReloadTagCache() => this.ExecuteReloadCache(() =>
    {
        var tags = RepositoryContext.TagRepository.GetAllTags();

        if (tags == null)
        {
            this.cache = new ThreadSafeCollection<int, Tag>();
            return;
        }

        var collection = new ThreadSafeCollection<int, Tag>(tags.ToDictionary(tag => tag.Id));

        this.cache = collection;
        this.BuildTagFilters();
        this.BuildTagNames();
    });

    private void BuildTagNames()
    {
        DalamudContext.PluginLog.Verbose("Entering TagService.BuildTagNames()");
        var tags = this.GetAllTags();
        this.tagNames = tags.Select(cat => cat.Name).ToList();
        this.tagNamesWithBlank = new List<string> { string.Empty }.Concat(this.tagNames).ToList();
    }

    private void BuildTagFilters()
    {
        DalamudContext.PluginLog.Verbose("Entering TagService.BuildTagFilters()");
        var tagsByRank = this.GetAllTags();
        var totalCategories = tagsByRank.Count;

        var tagFilterIds = tagsByRank.Select(tag => tag.Id).ToList();
        var tagFilterNames = tagsByRank.Select(tag => tag.Name).ToList();

        tagFilterIds.Insert(0, 0);
        tagFilterNames.Insert(0, string.Empty);

        this.playerTagFilter = new PlayerFilter
        {
            FilterIds = tagFilterIds,
            FilterNames = tagFilterNames,
            TotalFilters = totalCategories,
        };
    }
}
