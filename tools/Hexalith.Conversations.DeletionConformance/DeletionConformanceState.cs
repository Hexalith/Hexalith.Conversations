namespace Hexalith.Conversations.DeletionConformance;

/// <summary>The receipt stream's fold; receipts retain their original event position through retries.</summary>
public sealed class DeletionConformanceState
{
    /// <summary>Gets the contiguous stream position, including typed rejections.</summary>
    [System.Text.Json.Serialization.JsonInclude]
    public long Revision { get; private set; }

    /// <summary>Gets the sole immutable conformance acceptance.</summary>
    [System.Text.Json.Serialization.JsonInclude]
    public ConformanceDeletionAccepted? Acceptance { get; private set; }

    /// <summary>Folds one exactly positioned acceptance and rejects malformed or repeated acceptance.</summary>
    /// <param name="accepted">The persisted acceptance.</param>
    public void Apply(ConformanceDeletionAccepted accepted)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        if (Acceptance is not null || !DeletionConformanceContract.ValidSignal(accepted.Signal)
            || string.IsNullOrWhiteSpace(accepted.DeliveryAttemptId)
            || accepted.Acknowledgement is not { } receipt
            || receipt.SignalId != accepted.Signal.ConversationDeletionSignalId
            || receipt.SourceRevision != accepted.Signal.SourceRevision
            || receipt.ProtectedDeletionRevision != checked(Revision + 1)
            || !DeletionConformanceContract.ValidTarget(receipt.TargetVersion)
            || receipt.Evidence != DeletionConformanceContract.Evidence(accepted.Signal, receipt.TargetVersion, receipt.ProtectedDeletionRevision))
        {
            throw new InvalidOperationException("Invalid conformance acceptance.");
        }
        Revision++;
        Acceptance = accepted;
    }

    /// <summary>Folds a non-acceptance without changing any original receipt.</summary>
    /// <param name="rejected">The typed rejection.</param>
    public void Apply(ConformanceDeletionRejected rejected)
    {
        ArgumentNullException.ThrowIfNull(rejected);
        if (rejected.Outcome is not (Contracts.Agents.ConversationAgentsOutcome.Denied
            or Contracts.Agents.ConversationAgentsOutcome.Unavailable or Contracts.Agents.ConversationAgentsOutcome.Conflict
            or Contracts.Agents.ConversationAgentsOutcome.Invalid))
        {
            throw new InvalidOperationException("Invalid conformance rejection.");
        }
        Revision = checked(Revision + 1);
    }
}
