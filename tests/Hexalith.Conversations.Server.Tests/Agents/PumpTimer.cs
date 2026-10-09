namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Single controlled test-clock deadline signal.</summary>
internal sealed class PumpTimer(TimerCallback callback, object? state, long due) : ITimer
{
    private int _disposed;
    internal void Fire(long now) { if (now >= due && Interlocked.Exchange(ref _disposed, 1) == 0) { callback(state); } }
    public bool Change(TimeSpan dueTime, TimeSpan period) => false;
    public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
}
