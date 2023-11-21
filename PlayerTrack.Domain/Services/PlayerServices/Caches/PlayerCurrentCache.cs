using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCurrentCache: IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim cacheLock = new();
    private Dictionary<int, Player> currentDict = new();
    private SortedSet<Player> currentSortedSet = new();
    private HashSet<int> currentPlayerIds = new();

    public void Initialize(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.currentDict = new Dictionary<int, Player>();
            this.currentSortedSet = new SortedSet<Player>(comparer);
            this.currentPlayerIds = new HashSet<int>();
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
            if (this.currentPlayerIds.Contains(playerToAdd.Id) || playerToAdd.IsCurrent)
            {
                playerToAdd.IsCurrent = true;
                this.currentDict.TryAdd(playerToAdd.Id, playerToAdd);
                this.currentSortedSet.Add(playerToAdd);
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
            if (this.currentDict.Remove(playerToRemove.Id, out _))
            {
                this.currentSortedSet.Remove(playerToRemove);
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
            this.currentSortedSet = new SortedSet<Player>(this.currentSortedSet, comparer);
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
            return this.currentDict.TryGetValue(playerId, out var player) ? player : null;
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
            return this.currentSortedSet.FirstOrDefault(filter);
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
            return this.currentSortedSet.Where(filter).Skip(start).Take(count).ToList();
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
            return this.currentSortedSet.Where(filter).ToList();
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
            return this.currentSortedSet.ToList();
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
            return this.currentSortedSet.Count;
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
            return this.currentSortedSet.Count(filter);
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
            return this.currentSortedSet.Skip(start).Take(count).ToList();
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
            this.currentPlayerIds = new HashSet<int>();
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
            if (currentDict.Count != 0)
            {
                foreach (var player in this.currentDict.Keys)
                {
                    this.currentPlayerIds.Add(player);
                }
            }
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }

    public void SetIds(IEnumerable<int> ids)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.currentPlayerIds = new HashSet<int>(ids);
        }
        finally
        {
            this.cacheLock.ExitWriteLock();
        }
    }
    
    public int[] GetIds()
    {
        this.cacheLock.EnterReadLock();
        try
        {
            var ids = new HashSet<int>();
            if (currentDict.Count != 0)
            {
                foreach (var player in this.currentDict.Keys)
                {
                    ids.Add(player);
                }
            }

            return ids.ToArray();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }
}