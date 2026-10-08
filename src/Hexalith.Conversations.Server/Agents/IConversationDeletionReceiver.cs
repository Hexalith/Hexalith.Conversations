using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Private authenticated exact receiver transport. No available production default or inferred receipt exists.</summary>
public interface IConversationDeletionReceiver
{
    /// <summary>Returns the independently authenticated current exact target; null disables delivery.</summary>
    Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken cancellationToken = default);
    /// <summary>Looks up the original signal/attempt at its exact target, including after response loss and target rollover.</summary>
    Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default);
    /// <summary>Dispatches only that same immutable source signal and durable attempt to the authenticated exact target.</summary>
    Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target,
        CancellationToken cancellationToken = default);
}
