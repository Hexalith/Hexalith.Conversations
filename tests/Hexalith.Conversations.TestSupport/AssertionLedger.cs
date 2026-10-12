using System.Collections.ObjectModel;

namespace Hexalith.Conversations.TestSupport;

/// <summary>Records evaluated assertion identities and outcomes.</summary>
public sealed class AssertionLedger
{
    private readonly Dictionary<string, bool> _results = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BoundaryBlocker> _skips = new(StringComparer.Ordinal);
    private readonly List<AssertionLedgerRow> _rows = [];
    private readonly List<BoundaryBlocker> _blockers = [];

    /// <summary>Evaluated assertion identities and pass states.</summary>
    public IReadOnlyDictionary<string, bool> Results => new ReadOnlyDictionary<string, bool>(_results);

    /// <summary>All evaluations and skips in their recorded order.</summary>
    public IReadOnlyList<AssertionLedgerRow> Rows => _rows.AsReadOnly();

    /// <summary>Skipped identities and the facts that could not be established.</summary>
    public IReadOnlyDictionary<string, BoundaryBlocker> Skips => new ReadOnlyDictionary<string, BoundaryBlocker>(_skips);

    /// <summary>Adds exactly one evaluated assertion.</summary>
    public void Record(string identity, bool passed)
        => Record(identity, identity, passed);

    /// <summary>Adds an evaluated assertion with its measured subject.</summary>
    public void Record(string identity, string subject, bool passed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        if (!_results.TryAdd(identity, passed)) throw new ArgumentException("The assertion identity was already evaluated.", nameof(identity));
        _rows.Add(new(identity, subject, passed ? "PASS" : "FAIL"));
    }

    /// <summary>Records unavailable authority without evaluating an assertion.</summary>
    public void Skip(string identity, string subject, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (!_skips.TryAdd(identity, new(code, subject, message)))
            throw new ArgumentException("The assertion identity was already skipped.", nameof(identity));
        _rows.Add(new(identity, subject, "SKIPPED"));
    }

    /// <summary>Adds a measured blocker; the assertion is recorded separately.</summary>
    public void Block(string code, string subject, string message)
        => _blockers.Add(new(code, subject, message));

    /// <summary>Rejects empty, failing, skipped, or falsely passed evaluations.</summary>
    public BoundaryEvaluation Evaluate()
    {
        List<BoundaryBlocker> blockers = [.. _blockers, .. _skips.Values];
        if (_results.Count == 0)
            blockers.Add(new("ASSERTION_LEDGER_EMPTY", "ledger", "No assertion was evaluated."));
        foreach (string identity in _skips.Keys.Where(identity => _results.GetValueOrDefault(identity)))
            blockers.Add(new("UNAVAILABLE_HISTORY_PASSED", identity, "A skipped fact was recorded as passed."));
        bool failed = _results.Count == 0 || _results.Values.Any(passed => !passed)
            || _blockers.Count != 0 || _skips.Keys.Any(identity => _results.GetValueOrDefault(identity));
        string result = failed ? "FAIL" : _skips.Count != 0 ? "BLOCKED" : "PASS";
        return new(result, blockers.OrderBy(row => row.Code, StringComparer.Ordinal)
            .ThenBy(row => row.Subject, StringComparer.Ordinal).ToArray());
    }
}
