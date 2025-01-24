using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerCurrentCache: IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim CacheLock = new();
    private Dictionary<int, Player> CurrentDict = new();
    private SortedSet<Player> CurrentSortedSet = [];
    private HashSet<int> CurrentPlayerIds = [];

    public void Initialize(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            CurrentDict = new Dictionary<int, Player>();
            CurrentSortedSet = new SortedSet<Player>(comparer);
            CurrentPlayerIds = [];
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
            if (CurrentPlayerIds.Contains(playerToAdd.Id) || playerToAdd.IsCurrent)
            {
                playerToAdd.IsCurrent = true;
                CurrentDict.TryAdd(playerToAdd.Id, playerToAdd);
                CurrentSortedSet.Add(playerToAdd);
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
            if (CurrentDict.Remove(playerToRemove.Id, out _))
                CurrentSortedSet.Remove(playerToRemove);
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
            CurrentSortedSet = new SortedSet<Player>(CurrentSortedSet, comparer);
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
            return CurrentDict.GetValueOrDefault(playerId);
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
            return CurrentSortedSet.FirstOrDefault(filter);
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
            return CurrentSortedSet.Where(filter).Skip(start).Take(count).ToList();
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
            return CurrentSortedSet.Where(filter).ToList();
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
            return CurrentSortedSet.ToList();
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
            return CurrentSortedSet.Count;
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
            return CurrentSortedSet.Count(filter);
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
            return CurrentSortedSet.Skip(start).Take(count).ToList();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public void ClearIds()
    {
        CacheLock.EnterWriteLock();
        try
        {
            CurrentPlayerIds = new HashSet<int>();
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public void SaveIds()
    {
        CacheLock.EnterWriteLock();
        try
        {
            if (CurrentDict.Count != 0)
            {
                foreach (var player in CurrentDict.Keys)
                {
                    CurrentPlayerIds.Add(player);
                }
            }
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public void SetIds(IEnumerable<int> ids)
    {
        CacheLock.EnterWriteLock();
        try
        {
            CurrentPlayerIds = [..ids];
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public int[] GetIds()
    {
        CacheLock.EnterReadLock();
        try
        {
            var ids = new HashSet<int>();
            if (CurrentDict.Count != 0)
                foreach (var player in CurrentDict.Keys)
                    ids.Add(player);

            return ids.ToArray();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }
}
