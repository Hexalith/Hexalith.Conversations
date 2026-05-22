// <copyright file="MarkConversationContentSensitiveCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Requests an append-only governed sensitivity mark for conversation content metadata.
/// </summary>
/// <remarks>
/// Sensitivity marking requires paired audit evidence at the application boundary. This command
/// does not redact content, delete source events, enforce retention, govern audit records, or
/// implement UI, export, or evidence-bundle workflows.
/// </remarks>
/// <param name="metadata">The command metadata.</param>
/// <param name="conversationId">The governed conversation identity.</param>
/// <param name="target">The content-safe governed target reference.</param>
/// <param name="category">The bounded sensitivity category.</param>
/// <param name="policyReference">The content-safe public policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="operationTimestamp">The validated UTC operation timestamp supplied by the command boundary.</param>
public sealed record MarkConversationContentSensitiveCommand(
    ConversationCommandMetadata Metadata,
    ConversationId ConversationId,
    GovernanceTarget Target,
    SensitivityCategory Category,
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
    /// Gets the content-safe governed target reference.
    /// </summary>
    public GovernanceTarget Target { get; } = GovernanceContractValidation.RequireNonNull(Target, nameof(Target));

    /// <summary>
    /// Gets the bounded sensitivity category.
    /// </summary>
    public SensitivityCategory Category { get; } = GovernanceContractValidation.RequireNonNull(Category, nameof(Category));

    /// <summary>
    /// Gets the content-safe public policy reference.
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
    /// Creates the Story 2.1 governance metadata view for this sensitivity command.
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
            .Append(nameof(MarkConversationContentSensitiveCommand))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", ConversationId = ").Append(ConversationId)
            .Append(", Target = ").Append(Target)
            .Append(", Category = ").Append(Category)
            .Append(", OperationTimestamp = ").Append(OperationTimestamp.ToString("O"))
            .Append(" }");
        return builder.ToString();
    }
}
