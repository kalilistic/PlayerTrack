using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCategoryCache: IGroupedPlayerCache
{
    private readonly ReaderWriterLockSlim cacheLock = new();
    private Dictionary<int, Dictionary<int, Player>> categoryPlayersDict = null!;
    private Dictionary<int, SortedSet<Player>> categoryPlayersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.categoryPlayersDict = new Dictionary<int, Dictionary<int, Player>>();
            this.categoryPlayersSortedSet = new Dictionary<int, SortedSet<Player>>();
        
            var categories = ServiceContext.CategoryService.GetCategories();
            this.categoryPlayersDict.TryAdd(0, new Dictionary<int, Player>());
            this.categoryPlayersSortedSet.TryAdd(0, new SortedSet<Player>(comparer));
            foreach (var category in categories)
            {
                this.categoryPlayersDict.TryAdd(category.Id, new Dictionary<int, Player>());
                this.categoryPlayersSortedSet.TryAdd(category.Id, new SortedSet<Player>(comparer));
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
            if (playerToAdd.AssignedCategories.Count == 0)
            {
                this.categoryPlayersDict[0].TryAdd(playerToAdd.Id, playerToAdd);
                this.categoryPlayersSortedSet[0].Add(playerToAdd);
            }
            else
            {
                foreach (var category in playerToAdd.AssignedCategories)
                {
                    this.categoryPlayersDict[category.Id].TryAdd(playerToAdd.Id, playerToAdd);
                    this.categoryPlayersSortedSet[category.Id].Add(playerToAdd);
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
            foreach (var category in this.categoryPlayersDict)
            {
                if (category.Value.ContainsKey(playerToRemove.Id))
                {
                    if (this.categoryPlayersDict[category.Key].Remove(playerToRemove.Id, out _))
                    {
                        this.categoryPlayersSortedSet[category.Key].Remove(playerToRemove);
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
            foreach (var id in this.categoryPlayersSortedSet.Keys)
            {
                this.categoryPlayersSortedSet[id] = new SortedSet<Player>(this.categoryPlayersSortedSet[id], comparer);
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
            return this.categoryPlayersDict.Values.SelectMany(d => d.Values).FirstOrDefault(p => p.Id == playerId);
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public Dictionary<int, Player>? GetGroup(int categoryId)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            this.categoryPlayersDict.TryGetValue(categoryId, out var playerCache);
            return playerCache;
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
            this.categoryPlayersDict.TryAdd(groupId, new Dictionary<int, Player>());
            this.categoryPlayersSortedSet.TryAdd(groupId, new SortedSet<Player>());
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public Player? FindFirst(int groupId, Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.categoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.FirstOrDefault(filter) : null;
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
            return this.categoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.ToList() : new List<Player>();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int groupId, int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.categoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Skip(start).Take(count).ToList() : new List<Player>();
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
            this.categoryPlayersDict.Remove(groupId);
            this.categoryPlayersSortedSet.Remove(groupId);
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
            return this.categoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Count : 0;
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
            return this.categoryPlayersSortedSet.TryGetValue(groupId, out var playerCache) ? playerCache.Count(filter) : 0;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int categoryId, Func<Player, bool> filter, int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.categoryPlayersSortedSet.TryGetValue(categoryId, out var playerCache) ? playerCache.Where(filter).Skip(start).Take(count).ToList() : new List<Player>();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }
}