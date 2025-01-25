using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlayerTrack.Domain.Caches.Interfaces;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches;

public class PlayerRecentCache : IBasicPlayerCache
{
    private readonly ReaderWriterLockSlim CacheLock = new();
    private Dictionary<int, Player> RecentDict = new();
    private SortedSet<Player> RecentSortedSet = [];
    private ConcurrentDictionary<int, long> RecentPlayerExpiry = new();
    private HashSet<int> RecentPlayerIds = [];

    public void Initialize(IComparer<Player> comparer)
    {
        CacheLock.EnterWriteLock();
        try
        {
            RecentDict = new Dictionary<int, Player>();
            RecentSortedSet = new SortedSet<Player>(comparer);
            RecentPlayerExpiry = new ConcurrentDictionary<int, long>();
            RecentPlayerIds = [];
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
            if (RecentPlayerIds.Contains(playerToAdd.Id) || playerToAdd.IsRecent)
            {
                playerToAdd.IsRecent = true;
                RecentDict.TryAdd(playerToAdd.Id, playerToAdd);
                RecentSortedSet.Add(playerToAdd);
            }

            if (playerToAdd.IsCurrent)
                RecentPlayerExpiry.TryRemove(playerToAdd.Id, out _);
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
            if (!RecentPlayerExpiry.ContainsKey(playerToRemove.Id))
            {
                RecentPlayerExpiry.TryAdd(playerToRemove.Id, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            else
            {
                if (RecentDict.Remove(playerToRemove.Id, out _))
                    RecentSortedSet.Remove(playerToRemove);
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
            RecentSortedSet = new SortedSet<Player>(RecentSortedSet, comparer);
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
            return RecentDict.GetValueOrDefault(playerId);
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
            return RecentSortedSet.FirstOrDefault(filter);
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
            return RecentSortedSet.Where(filter).Skip(start).Take(count).ToList();
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
            return RecentSortedSet.Where(filter).ToList();
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
            return RecentSortedSet.ToList();
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
            return RecentSortedSet.Count;
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
            return RecentSortedSet.Count(filter);
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
            return RecentSortedSet.Skip(start).Take(count).ToList();
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
            RecentPlayerIds = [];
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
            if (RecentDict.Count != 0)
                foreach (var player in RecentDict.Keys)
                    RecentPlayerIds.Add(player);
        }
        finally
        {
            CacheLock.ExitWriteLock();
        }
    }

    public Dictionary<int, long> GetExpiry()
    {
        CacheLock.EnterReadLock();
        try
        {
            return RecentPlayerExpiry.ToDictionary(entry => entry.Key, entry => entry.Value);
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }

    public bool RemoveExpiry(int playerId)
    {
        CacheLock.EnterWriteLock();
        try
        {
            return RecentPlayerExpiry.Remove(playerId, out _);
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
            RecentPlayerIds = [..ids];
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
            if (RecentDict.Count != 0)
                foreach (var player in RecentDict.Keys)
                    ids.Add(player);

            return ids.ToArray();
        }
        finally
        {
            CacheLock.ExitReadLock();
        }
    }
}
