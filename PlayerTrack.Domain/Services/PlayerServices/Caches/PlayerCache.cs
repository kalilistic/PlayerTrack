using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCache : IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim cacheLock = new();
    private Dictionary<int, Player> playersDict = null!;
    private SortedSet<Player> playersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        this.cacheLock.EnterWriteLock();
        try
        {
            this.playersDict = new Dictionary<int, Player>();
            this.playersSortedSet = new SortedSet<Player>(comparer);
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
            this.playersDict.TryAdd(playerToAdd.Id, playerToAdd);
            this.playersSortedSet.Add(playerToAdd);
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
            this.playersDict.Remove(playerToRemove.Id, out _);
            this.playersSortedSet.Remove(playerToRemove);
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
            this.playersSortedSet = new SortedSet<Player>(this.playersSortedSet, comparer);
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
            return this.playersDict.TryGetValue(playerId, out var player) ? player : null;
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
            return this.playersSortedSet.FirstOrDefault(filter);
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
            return this.playersSortedSet.Where(filter).ToList();
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
            return this.playersSortedSet.ToList();
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
            return this.playersSortedSet.Count;
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
            return this.playersSortedSet.Count(filter);
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
            return this.playersSortedSet.Skip(start).Take(count).ToList();
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
            return this.playersSortedSet.Where(filter).Skip(start).Take(count).ToList();
        }
        finally
        {
            this.cacheLock.ExitReadLock();
        }
    }
}