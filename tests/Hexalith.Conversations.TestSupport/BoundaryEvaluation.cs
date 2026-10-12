namespace Hexalith.Conversations.TestSupport;

/// <summary>The final evaluation of a nonempty assertion ledger.</summary>
/// <param name="Result">PASS, FAIL, or BLOCKED.</param>
/// <param name="Blockers">Measured failures and unavailable facts.</param>
public sealed record BoundaryEvaluation(string Result, IReadOnlyList<BoundaryBlocker> Blockers)
{
    /// <summary>Whether every applicable assertion was evaluated and passed.</summary>
    public bool Passed => Result == "PASS";
}
