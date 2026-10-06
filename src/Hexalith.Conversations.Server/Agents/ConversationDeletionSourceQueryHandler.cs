// <copyright file="ConversationDeletionSourceQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Existing SDK query transport for the restricted conversation-deletion-source seam.</summary>
/// <param name="service">Current admitted source service.</param>
public sealed class ConversationDeletionSourceQueryHandler(ConversationAgentQueryService service) : IDomainQueryHandler
{
    /// <inheritdoc />
    public string Domain => "conversation";
    /// <inheritdoc />
    public string QueryType => "conversation-deletion-source";
    /// <inheritdoc />
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        try
        {
            var request = JsonSerializer.Deserialize<ConversationDeletionSourceQuery>(query.Payload, options);
            if (request is null || request.TenantId.Value != query.TenantId || request.ConversationId.Value != query.AggregateId)
            {
                return QueryResult.Failure("Denied");
            }
            ConversationDeletionSourceResult result = await service.DeletionSourceAsync(query.UserId, request, cancellationToken).ConfigureAwait(false);
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, options));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return QueryResult.Failure("Unavailable");
        }
    }
}
