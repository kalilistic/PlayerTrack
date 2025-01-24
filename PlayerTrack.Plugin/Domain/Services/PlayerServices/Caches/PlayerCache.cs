using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCache : IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim CacheLock = new();
    private Dictionary<int, Player> PlayersDict = null!;
    private SortedSet<Player> PlayersSortedSet = null!;

    public void Initialize(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            PlayersDict = new Dictionary<int, Player>();
            PlayersSortedSet = new SortedSet<Player>(comparer);
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
            PlayersDict.TryAdd(playerToAdd.Id, playerToAdd);
            PlayersSortedSet.Add(playerToAdd);
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
            PlayersDict.Remove(playerToRemove.Id, out _);
            PlayersSortedSet.Remove(playerToRemove);
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
            PlayersSortedSet = new SortedSet<Player>(PlayersSortedSet, comparer);
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
            return PlayersDict.TryGetValue(playerId, out var player) ? player : null;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public Player? FindFirst(Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.FirstOrDefault(filter);
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.Where(filter).ToList();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> GetAll()
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.ToList();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public int Count()
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.Count;
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public int Count(Func<Player, bool> filter)
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.Count(filter);
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.Skip(start).Take(count).ToList();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public List<Player> Get(Func<Player, bool> filter, int start, int count)
    {
        CacheLock.EnterReadLock();
        try
        {
            return PlayersSortedSet.Where(filter).Skip(start).Take(count).ToList();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }
}
