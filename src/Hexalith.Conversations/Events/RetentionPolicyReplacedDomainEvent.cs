// <copyright file="RetentionPolicyReplacedDomainEvent.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Conversations.Events;

/// <summary>
/// Records a governed retention policy replacement mutation.
/// </summary>
/// <param name="Metadata">The Conversations event metadata.</param>
/// <param name="PolicyReference">The new active public retention policy reference.</param>
/// <param name="PreviousPolicyReference">The previous public retention policy reference.</param>
/// <param name="Rationale">The content-safe governance rationale.</param>
/// <param name="AuditEvidence">The safe audit evidence paired with the mutation.</param>
/// <param name="IdempotencyKey">The caller idempotency key copied from command metadata, when supplied.</param>
public sealed record RetentionPolicyReplacedDomainEvent(
    ConversationEventMetadata Metadata,
    string PolicyReference,
    string PreviousPolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence,
    string? IdempotencyKey = null) : IEventPayload
{
    /// <summary>
    /// Gets the Conversations event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = Metadata ?? throw new ArgumentNullException(nameof(Metadata));

    /// <summary>
    /// Gets the new active public retention policy reference.
    /// </summary>
    public string PolicyReference { get; } = ValidateRequired(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the previous public retention policy reference.
    /// </summary>
    public string PreviousPolicyReference { get; } = ValidateRequired(PreviousPolicyReference, nameof(PreviousPolicyReference));

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
    public DateTimeOffset AppliedAt => Metadata.CommittedAt;

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(RetentionPolicyReplacedDomainEvent))
            .Append(" { Metadata = ").Append(Metadata)
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
