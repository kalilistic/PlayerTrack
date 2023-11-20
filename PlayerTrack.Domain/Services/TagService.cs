using System.Collections.Generic;
using System.Threading;
using Dalamud.DrunkenToad.Core;
using PlayerTrack.Infrastructure;
using PlayerTrack.Models;
using System.Linq;

namespace PlayerTrack.Domain;

public class TagService
{
    private PlayerFilter playerTagFilter = new();
    private List<string> tagNames = new();
    private List<string> tagNamesWithBlank = new() { string.Empty };
    private Dictionary<int, Tag> tags = new();
    private readonly ReaderWriterLockSlim setLock = new(LockRecursionPolicy.SupportsRecursion);

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

    public List<Tag> GetAllTags()
    {
        setLock.EnterReadLock();
        try
        {
            return tags.Values.ToList();
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    public Tag? GetTagByName(string name)
    {
        setLock.EnterReadLock();
        try
        {
            return tags.Values.FirstOrDefault(tag => tag.Name == name);
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    public Tag? GetTagById(int id)
    {
        setLock.EnterReadLock();
        try
        {
            return tags.TryGetValue(id, out var tag) ? tag : null;
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

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
        setLock.EnterReadLock();
        try
        {
            return includeBlank ? this.tagNamesWithBlank : this.tagNames;
        }
        finally
        {
            setLock.ExitReadLock();
        }
    }

    private void UpdateTagInCacheAndRepository(Tag tag)
    {
        setLock.EnterWriteLock();
        try
        {
            this.tags[tag.Id] = tag;
            RepositoryContext.TagRepository.UpdateTag(tag);
            ServiceContext.PlayerDataService.RefreshAllPlayers();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void AddTagToCacheAndRepository(Tag tag)
    {
        setLock.EnterWriteLock();
        try
        {
            tag.Id = RepositoryContext.TagRepository.CreateTag(tag);
            this.tags.Add(tag.Id, tag);
            ServiceContext.PlayerCacheService.AddTag(tag.Id);
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void DeleteTagFromCacheAndRepository(Tag tag)
    {
        setLock.EnterWriteLock();
        try
        {
            this.tags.Remove(tag.Id);
            RepositoryContext.TagRepository.DeleteTag(tag.Id);
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void ReloadTagCache()
    {
        setLock.EnterWriteLock();
        try
        {
            var tagsList = RepositoryContext.TagRepository.GetAllTags() ?? new List<Tag>();
            this.tags = tagsList.ToDictionary(tag => tag.Id, tag => tag);
            this.BuildTagFilters();
            this.BuildTagNames();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void BuildTagNames()
    {
        setLock.EnterWriteLock();
        try
        {
            DalamudContext.PluginLog.Verbose("Entering TagService.BuildTagNames()");
            var tagsList = this.GetAllTags();
            this.tagNames = tagsList.Select(cat => cat.Name).ToList();
            this.tagNamesWithBlank = new List<string> { string.Empty }.Concat(this.tagNames).ToList();
        }
        finally
        {
            setLock.ExitWriteLock();
        }
    }

    private void BuildTagFilters()
    {
        setLock.EnterReadLock();
        try
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
        finally
        {
            setLock.ExitReadLock();
        }
    }
}