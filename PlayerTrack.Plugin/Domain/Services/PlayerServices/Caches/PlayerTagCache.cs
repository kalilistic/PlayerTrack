using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerTagCache : IGroupedPlayerCache
{
    private readonly ReaderWriterLockSlim CacheLock = new();
    private Dictionary<int, Dictionary<int, Player>> TagPlayersDict = null!;
    private Dictionary<int, SortedSet<Player>> TagPlayersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            TagPlayersDict = new Dictionary<int, Dictionary<int, Player>>();
            TagPlayersSortedSet = new Dictionary<int, SortedSet<Player>>();

            var tags = ServiceContext.TagService.GetAllTags();
            TagPlayersDict.TryAdd(0, new Dictionary<int, Player>());
            TagPlayersSortedSet.TryAdd(0, new SortedSet<Player>(comparer));
            foreach (var tag in tags)
            {
                TagPlayersDict.TryAdd(tag.Id, new Dictionary<int, Player>());
                TagPlayersSortedSet.TryAdd(tag.Id, new SortedSet<Player>(comparer));
            }
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public void Add(Player playerToAdd)
    {
        CacheLock.EnterWriteLock();
        try
        {
            if (playerToAdd.AssignedTags.Count == 0)
            {
                TagPlayersDict[0].TryAdd(playerToAdd.Id, playerToAdd);
                TagPlayersSortedSet[0].Add(playerToAdd);
            }
            else
            {
                foreach (var tag in playerToAdd.AssignedTags)
                {
                    TagPlayersDict[tag.Id].TryAdd(playerToAdd.Id, playerToAdd);
                    TagPlayersSortedSet[tag.Id].Add(playerToAdd);
                }
            }
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public void Remove(Player playerToRemove)
    {
        CacheLock.EnterWriteLock();
        try
        {
            foreach (var tag in TagPlayersDict)
            {
                if (tag.Value.ContainsKey(playerToRemove.Id))
                    if (TagPlayersDict[tag.Key].Remove(playerToRemove.Id, out _))
                        TagPlayersSortedSet[tag.Key].Remove(playerToRemove);
            }
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public void Resort(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            foreach (var tagId in TagPlayersSortedSet.Keys)
                TagPlayersSortedSet[tagId] = new SortedSet<Player>(TagPlayersSortedSet[tagId], comparer);
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public Player? Get(int playerId)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersDict.SelectMany(x => x.Value).FirstOrDefault(x => x.Key == playerId).Value;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public Player? FindFirst(int groupId, Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.FirstOrDefault(filter) : null;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int groupId, Func<Player, bool> filter, int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Where(filter).Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> GetAll(int groupId)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.ToList() : new List<Player>();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public Dictionary<int, Player>? GetGroup(int groupId)
    {
        CacheLock.EnterReadLock();
        try
        {
            TagPlayersDict.TryGetValue(groupId, out var dict);
            return dict;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public void AddGroup(int groupId)
    {
        CacheLock.EnterWriteLock();
        try
        {
            TagPlayersDict.TryAdd(groupId, new Dictionary<int, Player>());
            TagPlayersSortedSet.TryAdd(groupId, []);
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public List<Player> Get(int groupId, int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public void RemoveGroup(int groupId)
    {
        CacheLock.EnterWriteLock();
        try
        {
            TagPlayersDict.Remove(groupId);
            TagPlayersSortedSet.Remove(groupId);
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public int Count(int groupId)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Count : 0;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public int Count(int groupId, Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return TagPlayersSortedSet.TryGetValue(groupId, out var sortedSet) ? sortedSet.Count(filter) : 0;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }
}
