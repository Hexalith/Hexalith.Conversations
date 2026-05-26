// <copyright file="IConversationClient.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;

namespace Hexalith.Conversations.Client;

/// <summary>
/// Provides the supported v1 .NET client workflow for Conversations adopters.
/// </summary>
public interface IConversationClient
{
    /// <summary>
    /// Creates a tenant-scoped conversation.
    /// </summary>
    /// <param name="command">The v1 create-conversation command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed create result or typed Conversations errors.</returns>
    Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(
        CreateConversationCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a message to an existing tenant-scoped conversation.
    /// </summary>
    /// <param name="command">The v1 append-message command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed command-accepted result or typed Conversations errors.</returns>
    Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(
        AppendMessageCommand command,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a tenant-scoped conversation timeline and freshness metadata.
    /// </summary>
    /// <param name="query">The v1 get-conversation query contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed detail result or typed Conversations errors.</returns>
    Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(
        GetConversationQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists tenant-scoped conversation summaries using supported v1 filters.
    /// </summary>
    /// <param name="query">The v1 list-conversations query contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed list result or typed Conversations errors.</returns>
    Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(
        ListConversationsQuery query,
        CancellationToken cancellationToken = default);
}
