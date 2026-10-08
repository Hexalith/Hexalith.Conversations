using System.Collections;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Count-correct synthetic source collection which cancels upon its second visited event.</summary>
/// <param name="events">Otherwise valid contiguous source events.</param>
/// <param name="cancel">Independent caller cancellation.</param>
internal sealed class ConversationSourceCancellationFixture(IReadOnlyList<StreamReadEvent> events, Action cancel)
    : IReadOnlyList<StreamReadEvent>
{
    /// <summary>Gets the number of events actually visited.</summary>
    public int Visited { get; private set; }

    /// <inheritdoc/>
    public int Count => events.Count;

    /// <inheritdoc/>
    public StreamReadEvent this[int index] => events[index];

    /// <inheritdoc/>
    public IEnumerator<StreamReadEvent> GetEnumerator()
    {
        foreach (var item in events)
        {
            if (++Visited == 2) { cancel(); }
            yield return item;
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
