// <copyright file="ConversationActiveCountQueryHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Existing SDK query transport for the restricted conversation-active-count seam.</summary>
/// <param name="service">Current admitted source service.</param>
public sealed class ConversationActiveCountQueryHandler(ConversationAgentQueryService service) : IDomainQueryHandler
{
    /// <inheritdoc />
    public string Domain => "conversation";
    /// <inheritdoc />
    public string QueryType => "conversation-active-count";
    /// <inheritdoc />
    public async Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        try
        {
            var request = JsonSerializer.Deserialize<ConversationActiveCountQuery>(query.Payload, options);
            if (request is null || request.TenantId.Value != query.TenantId)
            {
                return QueryResult.Failure("Denied");
            }
            ConversationActiveCountResult result = await service.CountAsync(query.UserId, request, cancellationToken).ConfigureAwait(false);
            return QueryResult.FromPayload(JsonSerializer.SerializeToElement(result, options));
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return QueryResult.Failure("Unavailable");
        }
    }
}
