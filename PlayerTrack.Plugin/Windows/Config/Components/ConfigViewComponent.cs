using System;
using System.Threading;
using PlayerTrack.Windows.Components;

namespace PlayerTrack.Windows.Config.Components;

public abstract class ConfigViewComponent : ViewComponent, IDisposable
{
    private readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(300);
    private Timer? DebounceTimer;

    public event Action OnPlayerConfigChanged = null!;

    public void Dispose()
    {
        DisposeDebounceTimer();
        GC.SuppressFinalize(this);
    }

    protected void NotifyConfigChanged()
    {
        ResetDebounceTimer();
        DebounceTimer = new Timer(DebounceCallback, null, DebounceDelay, Timeout.InfiniteTimeSpan);
    }

    private void DisposeDebounceTimer() => DebounceTimer?.Dispose();

    private void ResetDebounceTimer() => DisposeDebounceTimer();

    private void DebounceCallback(object? state) => OnPlayerConfigChanged();
}
