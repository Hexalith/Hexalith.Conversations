using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>Produces exactly one acceptance for a logical source signal; SDK/EventStore owns persistence.</summary>
[EventStoreDomain(DeletionConformanceContract.Domain)]
public sealed class DeletionConformanceAggregate : EventStoreAggregate<DeletionConformanceState>
{
    /// <summary>Produces an event or exact retry no-op, never a confirmed persistence acknowledgement.</summary>
    public static DomainResult Handle(AcceptConformanceDeletion command, DeletionConformanceState? state)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!DeletionConformanceContract.ValidSignal(command.Signal) || !DeletionConformanceContract.ValidTarget(command.TargetVersion)
            || string.IsNullOrWhiteSpace(command.DeliveryAttemptId) || command.DeliveryAttemptId.Length > 256)
        {
            return DomainResult.Rejection([new ConformanceDeletionRejected(ConversationAgentsOutcome.Invalid)]);
        }
        if (state?.Acceptance is { } previous)
        {
            return previous.Signal == command.Signal && previous.Acknowledgement.TargetVersion == command.TargetVersion
                ? DomainResult.NoOp() : DomainResult.Rejection([new ConformanceDeletionRejected(ConversationAgentsOutcome.Conflict)]);
        }
        long revision = checked((state?.Revision ?? 0) + 1);
        var receipt = new ConversationDeletionAcknowledgement(command.Signal.ConversationDeletionSignalId,
            command.Signal.SourceRevision, revision, command.TargetVersion,
            DeletionConformanceContract.Evidence(command.Signal, command.TargetVersion, revision));
        return DomainResult.Success([new ConformanceDeletionAccepted(command.Signal, command.DeliveryAttemptId, receipt)]);
    }
}
