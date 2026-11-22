// <copyright file="GetConversationDomainQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Serves the <c>conversation-detail</c> query through the SDK <c>/query</c> seam by delegating to
/// <see cref="ConversationQueryHandler.GetAsync"/>.
/// </summary>
public sealed class GetConversationDomainQueryHandler(ConversationQueryHandler queryHandler, Microsoft.AspNetCore.Http.IHttpContextAccessor? accessor = null)
    : ConversationDomainQueryHandlerBase(queryHandler)
{
    /// <summary>Preserves the original direct owner constructor signature.</summary>
    /// <param name="queryHandler">The ordinary owner authorization and native read handler.</param>
    public GetConversationDomainQueryHandler(ConversationQueryHandler queryHandler) : this(queryHandler, null) { }

    /// <summary>The stable kebab-case query-type discriminator.</summary>
    public const string ConversationDetailQueryType = GetConversationQuery.QueryType;

    /// <inheritdoc/>
    public override string QueryType => ConversationDetailQueryType;

    /// <inheritdoc/>
    protected override async Task<QueryResult> ExecuteCoreAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        VerifiedQueryCaller? caller = VerifiedQueryCaller.Get(accessor?.HttpContext);
        bool trustedDirectCall = accessor?.HttpContext is null && !query.IsDelegated && query.CallerAuthority is null;
        if (!trustedDirectCall && (caller is null || !ReferenceEquals(caller.Query, query)
            || query.OriginalActorId != query.UserId || string.IsNullOrWhiteSpace(query.TenantId)
            || query.Domain != Domain || query.QueryType != QueryType || query.IsGlobalAdmin
            || query.EntityId is not null && query.EntityId != query.AggregateId))
            return QueryResult.Failure("safe-denial");
        if (query.CallerAuthority?.Origin == "delegated-owner-query")
        {
            OriginatingQueryContext? origin = caller?.OriginatingContext;
            if (query.AuthenticatedWorkloadId != "projects" || !query.IsDelegated || origin is null
                || origin.OriginalActorId != query.UserId || string.IsNullOrWhiteSpace(origin.AuthenticatedWorkloadId)
                || origin.IsDelegated && string.IsNullOrWhiteSpace(origin.DelegationId)
                || !origin.IsDelegated && origin.DelegationId is not null
                || origin.Scopes?.Contains("projects.read", StringComparer.Ordinal) != true)
                return QueryResult.Failure("safe-denial");
        }
        else if (!trustedDirectCall && query.CallerAuthority?.Origin != "authenticated-caller") return QueryResult.Failure("safe-denial");

        GetConversationPayload? payload = DeserializePayload<GetConversationPayload>(query.Payload);

        // Fall back to the envelope aggregate id when the payload omits the conversation id, so a
        // gateway that routes by aggregate identity does not need to duplicate it in the body.
        string? conversationId = payload?.ConversationId;
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            conversationId = query.EntityId ?? query.AggregateId;
        }

        if (string.IsNullOrWhiteSpace(conversationId) || conversationId != query.AggregateId)
        {
            return QueryResult.Failure(ForbiddenFailure);
        }

        GetConversationQuery request = new(
            SchemaVersion.Current,
            TenantOf(query),
            query.UserId,
            query.CorrelationId,
            new ConversationId(conversationId))
        {
            AdvisoryMetadataOnly = payload?.AdvisoryMetadataOnly == true,
            NativeMembershipOnly = query.CallerAuthority?.Origin == "delegated-owner-query" || payload?.NativeMembershipOnly == true,
        };

        ConversationDetailResult result = await QueryHandler.GetAsync(request, cancellationToken).ConfigureAwait(false);
        return Success(result);
    }
}
