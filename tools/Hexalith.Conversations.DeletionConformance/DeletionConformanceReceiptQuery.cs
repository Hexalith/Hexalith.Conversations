using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.DeletionConformance;

/// <summary>SDK query handler for independent exact receipt lookup, with no acceptance or mutation path.</summary>
public sealed class DeletionConformanceReceiptQuery(IOptions<DeletionConformanceOptions> options,
    DeletionConformanceAuthorization authorization, DeletionConformanceStreams streams) : IDomainQueryHandler
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    /// <inheritdoc/>
    public string Domain => DeletionConformanceContract.Domain;
    /// <inheritdoc/>
    public string QueryType => "deletion-conformance-receipt";
    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = options.Value;
        if (!current.IsConfigured() || query.Domain != Domain || query.QueryType != QueryType || query.TenantId != current.TenantId
            || query.UserId != current.LookupPrincipalId || query.IsGlobalAdmin || query.IsDelegated
            || !authorization.Authorize(current.LookupPrincipalId, EventStoreWorkloadOperations.DomainServiceQuery, query.TenantId, true))
            return Result(ConversationAgentsOutcome.Denied);
        try
        {
            var request = JsonSerializer.Deserialize<DeletionConformanceLookup>(query.Payload, Json);
            if (request is null || !DeletionConformanceContract.ValidSignal(request.Signal)
                || request.Signal.TenantId.Value != query.TenantId || request.Signal.ConversationDeletionSignalId != query.AggregateId
                || string.IsNullOrWhiteSpace(request.DeliveryAttemptId) || request.DeliveryAttemptId.Length > 256
                || !current.AllowsTarget(request.TargetVersion)) return Result(ConversationAgentsOutcome.Denied);
            var state = await streams.ReadReceiptAsync(query.AggregateIdentity, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (state is null) return Result(ConversationAgentsOutcome.Unavailable);
            if (state.Acceptance is not { } accepted) return Result(ConversationAgentsOutcome.Absent);
            return accepted.Signal == request.Signal && accepted.Acknowledgement.TargetVersion == request.TargetVersion
                ? QueryResult.FromPayload(JsonSerializer.SerializeToElement(new DeletionConformanceResult(ConversationAgentsOutcome.Available, accepted.Acknowledgement), Json))
                : Result(ConversationAgentsOutcome.Conflict);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return Result(ConversationAgentsOutcome.Unavailable); }
    }

    private static QueryResult Result(ConversationAgentsOutcome outcome)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(new DeletionConformanceResult(outcome), Json));
}
