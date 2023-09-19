using System.Collections.Generic;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

using System.Linq;
using Dalamud.DrunkenToad.Caching;
using Dalamud.DrunkenToad.Collections;
using Dalamud.Logging;

public class TagService : UnsortedCacheService<Tag>
{
    public TagService() => this.ReloadTagCache();

    public void CreateTag(string name)
    {
        PluginLog.LogVerbose($"Entering TagService.CreateTag(): {name}");
        var tag = new Tag
        {
            Name = name,
            Color = DalamudContext.DataManager.GetRandomUIColor().Id,
        };
        this.AddTagToCacheAndRepository(tag);
    }

    public void UpdateTag(Tag tag) => this.UpdateTagInCacheAndRepository(tag);

    public List<Tag> GetAllTags() => this.cache.GetAll();

    public Tag? GetTagByName(string name) => this.cache.FindFirst(tag => tag.Name == name);

    public Tag? GetTagById(int id) => this.cache.FindFirst(tag => tag.Id == id);

    public void DeleteTag(Tag tag)
    {
        PluginLog.LogVerbose($"Entering TagService.DeleteTag(): {tag.Name}");
        PlayerTagService.DeletePlayerTagsByTagId(tag.Id);
        this.DeleteTagFromCacheAndRepository(tag);
    }

    public void RefreshTags()
    {
        PluginLog.LogVerbose("Entering TagService.RefreshTags()");
        this.ReloadTagCache();
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
    });
}
