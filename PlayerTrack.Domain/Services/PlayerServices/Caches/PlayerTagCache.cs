using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerTagCache : IGroupedPlayerCache
{
    private readonly ReaderWriterLockSlim cacheLock = new();
    private Dictionary<int, Dictionary<int, Player>> tagPlayersDict = null!;
    private Dictionary<int, SortedSet<Player>> tagPlayersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.tagPlayersDict = new Dictionary<int, Dictionary<int, Player>>();
            this.tagPlayersSortedSet = new Dictionary<int, SortedSet<Player>>();
        
            var tags = ServiceContext.TagService.GetAllTags();
            this.tagPlayersDict.TryAdd(0, new Dictionary<int, Player>());
            this.tagPlayersSortedSet.TryAdd(0, new SortedSet<Player>(comparer));
            foreach (var tag in tags)
            {
                this.tagPlayersDict.TryAdd(tag.Id, new Dictionary<int, Player>());
                this.tagPlayersSortedSet.TryAdd(tag.Id, new SortedSet<Player>(comparer));
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public void Add(Player playerToAdd)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            if (playerToAdd.AssignedTags.Count == 0)
            {
                this.tagPlayersDict[0].TryAdd(playerToAdd.Id, playerToAdd);
                this.tagPlayersSortedSet[0].Add(playerToAdd);
            }
            else
            {
                foreach (var tag in playerToAdd.AssignedTags)
                {
                    this.tagPlayersDict[tag.Id].TryAdd(playerToAdd.Id, playerToAdd);
                    this.tagPlayersSortedSet[tag.Id].Add(playerToAdd);
                }
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public void Remove(Player playerToRemove)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            foreach (var tag in this.tagPlayersDict)
            {
                if (tag.Value.ContainsKey(playerToRemove.Id))
                {
                    if (this.tagPlayersDict[tag.Key].Remove(playerToRemove.Id, out _))
                    {
                        this.tagPlayersSortedSet[tag.Key].Remove(playerToRemove);
                    }
                }
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public void Resort(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            foreach (var tagId in this.tagPlayersSortedSet.Keys)
            {
                this.tagPlayersSortedSet[tagId] = new SortedSet<Player>(this.tagPlayersSortedSet[tagId], comparer);
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public Player? Get(int playerId)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersDict.SelectMany(x => x.Value).FirstOrDefault(x => x.Key == playerId).Value;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public Player? FindFirst(int groupId, Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.FirstOrDefault(filter) : null;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int groupId, Func<Player, bool> filter, int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Where(filter).Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> GetAll(int groupId)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.ToList() : new List<Player>();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public Dictionary<int, Player>? GetGroup(int groupId)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            this.tagPlayersDict.TryGetValue(groupId, out var dict);
            return dict;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public void AddGroup(int groupId)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.tagPlayersDict.TryAdd(groupId, new Dictionary<int, Player>());
            this.tagPlayersSortedSet.TryAdd(groupId, new SortedSet<Player>());
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public List<Player> Get(int groupId, int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public void RemoveGroup(int groupId)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.tagPlayersDict.Remove(groupId);
            this.tagPlayersSortedSet.Remove(groupId);
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public int Count(int groupId)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Count : 0;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public int Count(int groupId, Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.tagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Count(filter) : 0;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }
}