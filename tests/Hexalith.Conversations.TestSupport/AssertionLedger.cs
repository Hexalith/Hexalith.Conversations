namespace Hexalith.Conversations.TestSupport;

/// <summary>Records evaluated assertion identities and outcomes.</summary>
public sealed class AssertionLedger
{
    private readonly Dictionary<string, bool> _results = new(StringComparer.Ordinal);

    /// <summary>Evaluated assertion identities and pass states.</summary>
    public IReadOnlyDictionary<string, bool> Results => _results;

    /// <summary>Adds exactly one evaluated assertion.</summary>
    public void Record(string identity, bool passed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        if (!_results.TryAdd(identity, passed)) throw new ArgumentException("The assertion identity was already evaluated.", nameof(identity));
    }
}
