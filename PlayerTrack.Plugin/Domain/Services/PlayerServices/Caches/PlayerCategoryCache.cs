using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCategoryCache: IGroupedPlayerCache
{
    private readonly ReaderWriterLockSlim CacheLock = new();
    private Dictionary<int, Dictionary<int, Player>> CategoryPlayersDict = null!;
    private Dictionary<int, SortedSet<Player>> CategoryPlayersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            CategoryPlayersDict = new Dictionary<int, Dictionary<int, Player>>();
            CategoryPlayersSortedSet = new Dictionary<int, SortedSet<Player>>();

            var categories = ServiceContext.CategoryService.GetCategories();
            CategoryPlayersDict.TryAdd(0, new Dictionary<int, Player>());
            CategoryPlayersSortedSet.TryAdd(0, new SortedSet<Player>(comparer));
            foreach (var category in categories)
            {
                CategoryPlayersDict.TryAdd(category.Id, new Dictionary<int, Player>());
                CategoryPlayersSortedSet.TryAdd(category.Id, new SortedSet<Player>(comparer));
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
            if (playerToAdd.AssignedCategories.Count == 0)
            {
                CategoryPlayersDict[0].TryAdd(playerToAdd.Id, playerToAdd);
                CategoryPlayersSortedSet[0].Add(playerToAdd);
            }
            else
            {
                foreach (var category in playerToAdd.AssignedCategories)
                {
                    CategoryPlayersDict[category.Id].TryAdd(playerToAdd.Id, playerToAdd);
                    CategoryPlayersSortedSet[category.Id].Add(playerToAdd);
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
            foreach (var category in CategoryPlayersDict)
            {
                if (category.Value.ContainsKey(playerToRemove.Id))
                    if (CategoryPlayersDict[category.Key].Remove(playerToRemove.Id, out _))
                        CategoryPlayersSortedSet[category.Key].Remove(playerToRemove);
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
            foreach (var id in CategoryPlayersSortedSet.Keys)
                CategoryPlayersSortedSet[id] = new SortedSet<Player>(CategoryPlayersSortedSet[id], comparer);
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
            return CategoryPlayersDict.Values.SelectMany(d => d.Values).FirstOrDefault(p => p.Id == playerId);
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public Dictionary<int, Player>? GetGroup(int categoryId)
    {
        CacheLock.EnterReadLock();
        try
        {
            CategoryPlayersDict.TryGetValue(categoryId, out var playerCache);
            return playerCache;
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
            CategoryPlayersDict.TryAdd(groupId, new Dictionary<int, Player>());
            CategoryPlayersSortedSet.TryAdd(groupId, []);
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public Player? FindFirst(int groupId, Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return CategoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.FirstOrDefault(filter) : null;
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
            return CategoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.ToList() : new List<Player>();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int groupId, int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return CategoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Skip(start).Take(count).ToList() : new List<Player>();
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
            CategoryPlayersDict.Remove(groupId);
            CategoryPlayersSortedSet.Remove(groupId);
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
            return CategoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Count : 0;
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
            return CategoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Count(filter) : 0;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int categoryId, Func<Player, bool> filter, int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return CategoryPlayersSortedSet.TryGetValue(categoryId, out var playerCache) ? playerCache.Where(filter).Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }
}
