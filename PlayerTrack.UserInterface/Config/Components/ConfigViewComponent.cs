using System;
using System.Threading;
using PlayerTrack.UserInterface.Components;

namespace PlayerTrack.UserInterface.Config.Components;

public abstract class ConfigViewComponent : ViewComponent, IDisposable
{
    private readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(300);
    private Timer? debounceTimer;

    public event Action OnPlayerConfigChanged = null!;

    public void Dispose()
    {
        this.DisposeDebounceTimer();
        GC.SuppressFinalize(this);
    }

    protected void NotifyConfigChanged()
    {
        this.ResetDebounceTimer();
        this.debounceTimer = new Timer(this.DebounceCallback, null, this.debounceDelay, Timeout.InfiniteTimeSpan);
    }

    private void DisposeDebounceTimer() => this.debounceTimer?.Dispose();

    private void ResetDebounceTimer() => this.DisposeDebounceTimer();

    private void DebounceCallback(object? state) => this.OnPlayerConfigChanged();
}
