using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dalamud.DrunkenToad.Helpers;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerRecentCache : IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim cacheLock = new();
    private Dictionary<int, Player> recentDict = null!;
    private SortedSet<Player> recentSortedSet = null!;
    private ConcurrentDictionary<int, long> recentPlayerExpiry = null!;
    private HashSet<int> recentPlayerIds = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.recentDict = new Dictionary<int, Player>();
            this.recentSortedSet = new SortedSet<Player>(comparer);
            this.recentPlayerExpiry = new ConcurrentDictionary<int, long>();
            this.recentPlayerIds = new HashSet<int>();
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
            if (this.recentPlayerIds.Contains(playerToAdd.Id) || playerToAdd.IsRecent)
            {
                playerToAdd.IsRecent = true;
                this.recentDict.TryAdd(playerToAdd.Id, playerToAdd);
                this.recentSortedSet.Add(playerToAdd);
            }

            if (playerToAdd.IsCurrent)
            {
                this.recentPlayerExpiry.TryRemove(playerToAdd.Id, out _);
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
            if (!this.recentPlayerExpiry.ContainsKey(playerToRemove.Id))
            {
                this.recentPlayerExpiry.TryAdd(playerToRemove.Id, UnixTimestampHelper.CurrentTime());
            }
            else
            {
                if (this.recentDict.Remove(playerToRemove.Id, out _))
                {
                    this.recentSortedSet.Remove(playerToRemove);
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
            this.recentSortedSet = new SortedSet<Player>(this.recentSortedSet, comparer);
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
            return this.recentDict.TryGetValue(playerId, out var player) ? player : null;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public Player? FindFirst(Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.FirstOrDefault(filter);
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(Func<Player, bool> filter, int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.Where(filter).Skip(start).Take(count).ToList();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.Where(filter).ToList();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> GetAll()
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.ToList();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public int Count()
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.Count;
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public int Count(Func<Player, bool> filter)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.Count(filter);
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int start, int count)
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentSortedSet.Skip(start).Take(count).ToList();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public void ClearIds()
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.recentPlayerIds = new HashSet<int>();
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public void SaveIds()
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            if (recentDict.Count != 0)
            {
                foreach (var player in this.recentDict.Keys)
                {
                    this.recentPlayerIds.Add(player);
                }
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public Dictionary<int, long> GetExpiry()
    {
        this.cacheLock.EnterReadLock();
        try
        {
            return this.recentPlayerExpiry.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }

    public bool RemoveExpiry(int playerId)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            return this.recentPlayerExpiry.Remove(playerId, out _);
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }
}