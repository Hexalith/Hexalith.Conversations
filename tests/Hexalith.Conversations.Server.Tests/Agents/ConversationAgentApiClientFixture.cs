using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Local admitted gateway simulation that persists the aggregate result before returning.</summary>
public sealed class ConversationAgentApiClientFixture(ConversationAgentLocalFixture source) : IConversationClient
{
    /// <summary>Number of dispatched membership requests.</summary>
    public int MembershipCalls { get; private set; }

    /// <inheritdoc />
    public async Task<ConversationAgentCommandResult> AddParticipantAsync(AddParticipantCommand request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        MembershipCalls++;
        var before = await source.ReplayAsync().ConfigureAwait(false);
        var result = ConversationAggregate.Handle(new AddAgentParticipant(request, request.OperationTimestamp!.Value, "api-membership"), before);
        source.Persist(result);
        return new(result.IsRejection ? ConversationAgentsOutcome.Conflict : ConversationAgentsOutcome.Available,
            "api-command", Persisted: false);
    }

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(CreateConversationCommand command,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(AppendMessageCommand command,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(ReassignConversationProjectCommand command,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(GetConversationQuery query,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(ListConversationsQuery query,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
