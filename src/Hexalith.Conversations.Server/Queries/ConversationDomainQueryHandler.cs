// <copyright file="ConversationDomainQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Base class for the conversation <see cref="IDomainQueryHandler"/> adapters discovered and dispatched by
/// the SDK <c>/query</c> seam (<see cref="DomainQueryDispatcher"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each concrete adapter is a <b>thin</b> bridge over the existing <see cref="ConversationQueryHandler"/>: it
/// deserializes the <see cref="QueryEnvelope.Payload"/> into the conversation-specific query inputs, carries
/// the authenticated identity (<see cref="QueryEnvelope.TenantId"/> / <see cref="QueryEnvelope.UserId"/> /
/// <see cref="QueryEnvelope.CorrelationId"/>) into the existing query request, delegates to the matching
/// <see cref="ConversationQueryHandler"/> method, and serializes the result into a <see cref="QueryResult"/>.
/// The filter, freshness aggregation, hydration, cursor, and tenant-access logic stay in
/// <see cref="ConversationQueryHandler"/> — the adapter reimplements none of it.
/// </para>
/// <para>
/// The envelope identity is never trusted beyond what the existing tenant-access gate already enforces: the
/// underlying handler authorizes before any projection read. A missing authenticated user is rejected here
/// before any state access, and no exception is allowed to leak past the seam — a failure maps to a coarse
/// <see cref="QueryResult.Failure"/> matching the safe-shape posture of the underlying results.
/// </para>
/// </remarks>
public abstract class ConversationDomainQueryHandlerBase : IDomainQueryHandler
{
    /// <summary>The kebab-case domain discriminator every conversation query handler serves.</summary>
    public const string ConversationsDomain = "conversations";

    /// <summary>The coarse adapter-edge failure text returned when identity is missing or a query faults.</summary>
    protected const string ForbiddenFailure = "Forbidden";

    private static readonly JsonSerializerOptions s_queryJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ConversationQueryHandler _queryHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationDomainQueryHandlerBase"/> class.
    /// </summary>
    /// <param name="queryHandler">The existing conversation query handler this adapter delegates to.</param>
    protected ConversationDomainQueryHandlerBase(ConversationQueryHandler queryHandler)
        => _queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));

    /// <inheritdoc/>
    public string Domain => ConversationsDomain;

    /// <inheritdoc/>
    public abstract string QueryType { get; }

    /// <summary>Gets the conversation query handler this adapter delegates to.</summary>
    protected ConversationQueryHandler QueryHandler => _queryHandler;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Reject an envelope without an authenticated user before any state access, mirroring the
        // tenant-query precedent — a missing identity must never reach the read model.
        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            return QueryResult.Failure(ForbiddenFailure);
        }

        try
        {
            return await ExecuteCoreAsync(query, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Never let an exception leak past the seam; fail closed with a coarse adapter-edge error.
            return QueryResult.Failure("The conversation query could not be served.");
        }
    }

    /// <summary>
    /// Executes the query after the shared authenticated-user gate has passed.
    /// </summary>
    /// <param name="query">The authenticated query envelope.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The serialized query result.</returns>
    protected abstract Task<QueryResult> ExecuteCoreAsync(QueryEnvelope query, CancellationToken cancellationToken);

    /// <summary>Serializes a result payload into a successful <see cref="QueryResult"/>.</summary>
    /// <typeparam name="T">The result payload type.</typeparam>
    /// <param name="value">The result payload.</param>
    /// <returns>A successful query result carrying the serialized payload.</returns>
    protected static QueryResult Success<T>(T value)
        => QueryResult.FromPayload(JsonSerializer.SerializeToElement(value, s_queryJsonOptions));

    /// <summary>Deserializes the envelope payload into the query-specific input shape.</summary>
    /// <typeparam name="T">The payload shape.</typeparam>
    /// <param name="payload">The UTF-8 JSON payload bytes.</param>
    /// <returns>The deserialized payload, or <see langword="null"/> when the payload is empty.</returns>
    protected static T? DeserializePayload<T>(byte[] payload)
        where T : class
        => payload is { Length: > 0 }
            ? JsonSerializer.Deserialize<T>(payload, s_queryJsonOptions)
            : null;

    /// <summary>Resolves the trusted tenant binding from the envelope.</summary>
    /// <param name="query">The query envelope.</param>
    /// <returns>The tenant identifier.</returns>
    protected static TenantId TenantOf(QueryEnvelope query) => new(query.TenantId);
}

/// <summary>
/// Serves the <c>conversation-list</c> query through the SDK <c>/query</c> seam by delegating to
/// <see cref="ConversationQueryHandler.ListAsync"/>.
/// </summary>
public sealed class ListConversationsDomainQueryHandler(ConversationQueryHandler queryHandler)
    : ConversationDomainQueryHandlerBase(queryHandler)
{
    /// <summary>The stable kebab-case query-type discriminator.</summary>
    public const string ConversationListQueryType = "conversation-list";

    /// <inheritdoc/>
    public override string QueryType => ConversationListQueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        ListConversationsPayload payload =
            DeserializePayload<ListConversationsPayload>(query.Payload) ?? new ListConversationsPayload(null, null);

        ListConversationsQuery request = new(
            SchemaVersion.Current,
            TenantOf(query),
            query.UserId,
            query.CorrelationId,
            payload.Filter,
            payload.Page);

        ConversationListResult result = await QueryHandler.ListAsync(request, cancellationToken).ConfigureAwait(false);
        return Success(result);
    }

    /// <summary>The query-specific fields carried in the list query envelope payload.</summary>
    /// <param name="Filter">The exact-match tenant-scoped filter (defaults to empty when omitted).</param>
    /// <param name="Page">The bounded page request (defaults to the first page when omitted).</param>
    public sealed record ListConversationsPayload(ConversationListFilterV1? Filter, ConversationPageRequest? Page);
}

/// <summary>
/// Serves the <c>conversation-detail</c> query through the SDK <c>/query</c> seam by delegating to
/// <see cref="ConversationQueryHandler.GetAsync"/>.
/// </summary>
public sealed class GetConversationDomainQueryHandler(ConversationQueryHandler queryHandler)
    : ConversationDomainQueryHandlerBase(queryHandler)
{
    /// <summary>The stable kebab-case query-type discriminator.</summary>
    public const string ConversationDetailQueryType = "conversation-detail";

    /// <inheritdoc/>
    public override string QueryType => ConversationDetailQueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        GetConversationPayload? payload = DeserializePayload<GetConversationPayload>(query.Payload);

        // Fall back to the envelope aggregate id when the payload omits the conversation id, so a
        // gateway that routes by aggregate identity does not need to duplicate it in the body.
        string? conversationId = payload?.ConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            conversationId = query.EntityId ?? query.AggregateId;
        }

        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return QueryResult.Failure(ForbiddenFailure);
        }

        GetConversationQuery request = new(
            SchemaVersion.Current,
            TenantOf(query),
            query.UserId,
            query.CorrelationId,
            new ConversationId(conversationId));

        ConversationDetailResult result = await QueryHandler.GetAsync(request, cancellationToken).ConfigureAwait(false);
        return Success(result);
    }

    /// <summary>The query-specific fields carried in the detail query envelope payload.</summary>
    /// <param name="ConversationId">The Conversations-owned conversation identity.</param>
    public sealed record GetConversationPayload(string? ConversationId);
}
