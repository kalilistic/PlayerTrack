using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PlayerTrack.Domain.Common;

public abstract class CacheService<T> : IDisposable
{
    private readonly ReaderWriterLockSlim ResetLock = new ();
    private volatile bool IsResettingCache;
    protected ConcurrentDictionary<int, T> Cache = null!;

    public void Dispose()
    {
        ResetLock.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void ExecuteReloadCache(Action customAction)
    {
        if (IsResettingCache)
        {
            Plugin.PluginLog.Verbose("A cache reset is already in progress. Ignoring this request.");
            return;
        }

        customAction.Invoke();
        IsResettingCache = false;
    }
}
