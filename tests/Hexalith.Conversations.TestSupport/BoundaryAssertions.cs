namespace Hexalith.Conversations.TestSupport;

/// <summary>Small neutral assertion helpers that record their evaluation.</summary>
public static class BoundaryAssertions
{
    /// <summary>Records whether an expected value equals an observed value.</summary>
    public static bool Equal<T>(AssertionLedger ledger, string identity, T expected, T observed)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        bool passed = EqualityComparer<T>.Default.Equals(expected, observed);
        ledger.Record(identity, passed);
        return passed;
    }

    /// <summary>Records a predicate that the caller has evaluated.</summary>
    public static bool True(AssertionLedger ledger, string identity, bool condition)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ledger.Record(identity, condition);
        return condition;
    }
}
