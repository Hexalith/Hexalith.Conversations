// <copyright file="ConversationContentMarkedSensitiveDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records a governed sensitivity mark mutation.
/// </summary>
/// <param name="Metadata">The Conversations event metadata.</param>
/// <param name="Target">The content-safe governed target reference.</param>
/// <param name="Category">The bounded sensitivity category.</param>
/// <param name="PolicyReference">The content-safe public policy reference.</param>
/// <param name="Rationale">The content-safe governance rationale.</param>
/// <param name="AuditEvidence">The safe audit evidence paired with the mutation.</param>
/// <param name="IdempotencyKey">The caller idempotency key copied from command metadata, when supplied.</param>
public sealed record ConversationContentMarkedSensitiveDomainEvent(
    ConversationEventMetadata Metadata,
    GovernanceTarget Target,
    SensitivityCategory Category,
    string PolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence,
    string? IdempotencyKey = null) : IEventPayload
{
    /// <summary>
    /// Gets the Conversations event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the content-safe governed target reference.
    /// </summary>
    public GovernanceTarget Target { get; } = Target ?? throw new ArgumentNullException(nameof(Target));

    /// <summary>
    /// Gets the bounded sensitivity category.
    /// </summary>
    public SensitivityCategory Category { get; } = Category ?? throw new ArgumentNullException(nameof(Category));

    /// <summary>
    /// Gets the content-safe public policy reference.
    /// </summary>
    public string PolicyReference { get; } = ValidateRequired(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the content-safe governance rationale.
    /// </summary>
    public string Rationale { get; } = ValidateRequired(Rationale, nameof(Rationale));

    /// <summary>
    /// Gets the safe audit evidence paired with the mutation.
    /// </summary>
    public GovernanceAuditEvidenceReference AuditEvidence { get; } = AuditEvidence ?? throw new ArgumentNullException(nameof(AuditEvidence));

    /// <summary>
    /// Gets the deterministic operation timestamp copied from event metadata.
    /// </summary>
    public DateTimeOffset MarkedAt => Metadata.CommittedAt;

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(ConversationContentMarkedSensitiveDomainEvent))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", Target = ").Append(Target)
            .Append(", Category = ").Append(Category)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
