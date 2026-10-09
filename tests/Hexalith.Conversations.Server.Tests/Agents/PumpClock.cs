using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Controlled monotonic test clock for the pump whole-operation budget.</summary>
internal sealed class PumpClock : TimeProvider
{
    private long _ticks;
    private readonly List<PumpTimer> _timers = [];
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);
    public override DateTimeOffset GetUtcNow() => F.At.AddTicks(GetTimestamp());
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    { var timer = new PumpTimer(callback, state, GetTimestamp() + dueTime.Ticks); lock (_timers) { _timers.Add(timer); } return timer; }
    internal void Advance(TimeSpan elapsed)
    { long now = Interlocked.Add(ref _ticks, elapsed.Ticks); PumpTimer[] timers; lock (_timers) { timers = _timers.ToArray(); } foreach (var timer in timers) { timer.Fire(now); } }
}
