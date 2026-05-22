// <copyright file="SetConversationRetentionPolicyCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Requests an append-only governed retention policy set or replacement for a conversation.
/// </summary>
/// <remarks>
/// Retention policy changes require paired audit evidence at the application boundary. This command
/// does not schedule deletion, redaction, legal-hold changes, enforcement jobs, or UI workflows.
/// </remarks>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The governed conversation identity.</param>
/// <param name="policyReference">The content-safe public retention policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="operationTimestamp">The validated UTC operation timestamp supplied by the command boundary.</param>
public sealed record SetConversationRetentionPolicyCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    string PolicyReference,
    string Rationale,
    DateTimeOffset OperationTimestamp)
{
    /// <summary>
    /// Gets the command metadata.
    /// </summary>
    public ConversationCommandMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    /// <summary>
    /// Gets the governed conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = GovernanceContractValidation.RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the content-safe public retention policy reference.
    /// </summary>
    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the required content-safe governance rationale.
    /// </summary>
    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    /// <summary>
    /// Gets the validated UTC operation timestamp supplied by the command boundary.
    /// </summary>
    public DateTimeOffset OperationTimestamp { get; } =
        GovernanceContractValidation.RequiredUtcTimestamp(OperationTimestamp, nameof(OperationTimestamp));

    /// <summary>
    /// Creates the Story 2.1 governance metadata view for this retention command.
    /// </summary>
    /// <returns>The governance operation metadata.</returns>
    public GovernanceOperationMetadata ToGovernanceMetadata()
        => GovernanceOperationMetadata.FromCommandMetadata(
            Metadata,
            ConversationId,
            Rationale,
            PolicyReference,
            OperationTimestamp);

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(SetConversationRetentionPolicyCommand))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", ConversationId = ").Append(ConversationId)
            .Append(", OperationTimestamp = ").Append(OperationTimestamp.ToString("O"))
            .Append(" }");
        return builder.ToString();
    }
}
