namespace Hexalith.Conversations.TestSupport;

/// <summary>An evaluated or visibly skipped assertion.</summary>
/// <param name="Id">The stable assertion identity.</param>
/// <param name="Subject">The path or fact checked.</param>
/// <param name="State">PASS, FAIL, or SKIPPED.</param>
public sealed record AssertionLedgerRow(string Id, string Subject, string State);
