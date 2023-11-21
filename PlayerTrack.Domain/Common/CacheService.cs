using Dalamud.DrunkenToad.Collections;
using Dalamud.DrunkenToad.Core;

namespace PlayerTrack.Domain.Common;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public abstract class CacheService<T> : IDisposable
{
    private readonly ReaderWriterLockSlim resetLock = new ();
    private readonly Queue<Action> pendingOperations = new ();
    private volatile bool isResettingCache;
    protected ThreadSafeCollection<int, T> cache = null!;
    public event Action? CacheUpdated;
    
    public void Dispose()
    {
        this.resetLock.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void OnCacheUpdated() => this.CacheUpdated?.Invoke();

    protected void ExecuteOrEnqueue(Action operation)
    {
        if (this.isResettingCache)
        {
            this.resetLock.EnterReadLock();
            try
            {
                this.pendingOperations.Enqueue(operation);
            }
            finally
            {
                this.resetLock.ExitReadLock();
            }
        }
        else
        {
            operation();
        }
    }

    protected async Task ExecuteReloadCacheAsync(Func<Task> customAction)
    {
        if (this.isResettingCache)
        {
            DalamudContext.PluginLog.Verbose("A cache reset is already in progress. Ignoring this request.");
            return;
        }

        await customAction.Invoke();

        while (this.pendingOperations.TryDequeue(out var operation))
        {
            operation();
        }

        this.isResettingCache = false;
        this.CacheUpdated?.Invoke();
    }

    protected void ExecuteReloadCache(Action customAction)
    {
        if (this.isResettingCache)
        {
            DalamudContext.PluginLog.Verbose("A cache reset is already in progress. Ignoring this request.");
            return;
        }

        customAction.Invoke();

        while (this.pendingOperations.TryDequeue(out var operation))
        {
            operation();
        }

        this.isResettingCache = false;
        this.CacheUpdated?.Invoke();
    }
}