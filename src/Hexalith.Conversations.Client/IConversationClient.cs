// <copyright file="IConversationClient.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Agents;
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
    /// Assigns, reassigns, or explicitly clears a conversation project reference.
    /// </summary>
    /// <param name="command">The v1 project reassignment command contract.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A typed command-accepted result or typed Conversations errors.</returns>
    Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(
        ReassignConversationProjectCommand command,
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

    /// <summary>Executes the restricted AddParticipantAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentCommandResult> AddParticipantAsync(AddParticipantCommand request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentCommandResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted RemoveAgentParticipantAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentCommandResult> RemoveAgentParticipantAsync(RemoveAgentParticipantCommand request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentCommandResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted PostAgentMessageAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentCommandResult> PostAgentMessageAsync(AppendMessageCommand request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentCommandResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted GetAgentConversationAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentReadResult> GetAgentConversationAsync(ConversationAgentReadQuery request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentReadResult>(new(ConversationAgentsOutcome.Unavailable, request.TenantId, request.ConversationId));

    /// <summary>Executes the restricted GetActiveConversationCountAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationActiveCountResult> GetActiveConversationCountAsync(ConversationActiveCountQuery request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationActiveCountResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted ApproveConversationDeletionAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentCommandResult> ApproveConversationDeletionAsync(ApproveConversationDeletionCommand request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentCommandResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted RecordConversationDeletionDeliveryAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationAgentCommandResult> RecordConversationDeletionDeliveryAsync(RecordConversationDeletionDeliveryCommand request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationAgentCommandResult>(new(ConversationAgentsOutcome.Unavailable));

    /// <summary>Executes the restricted GetConversationDeletionSourceAsync owner seam through the existing gateway.</summary>
    /// <param name="request">Exact portable intent or current source query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>A safe typed outcome; default implementors fail closed.</returns>
    Task<ConversationDeletionSourceResult> GetConversationDeletionSourceAsync(ConversationDeletionSourceQuery request, CancellationToken cancellationToken = default)
        => Task.FromResult<ConversationDeletionSourceResult>(new(ConversationAgentsOutcome.Unavailable));
}
