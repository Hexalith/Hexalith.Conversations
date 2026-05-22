// <copyright file="MessageContentRedacted.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Publishes that governed redaction intent was recorded for message content.
/// </summary>
/// <remarks>
/// This append-only governance fact has mandatory safe audit evidence. It does not include original
/// content, rewrite source events, delete history, process legal holds, mask projections, or implement
/// UI/export/log/trace behavior.
/// </remarks>
/// <param name="metadata">The public event metadata.</param>
/// <param name="target">The content-safe governed message or opaque content-segment target.</param>
/// <param name="category">The bounded redaction category.</param>
/// <param name="policyReference">The content-safe public policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="auditEvidence">The safe audit evidence reference paired with the mutation.</param>
public sealed record MessageContentRedacted(
    ConversationEventMetadata Metadata,
    GovernanceTarget Target,
    RedactionCategory Category,
    string PolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence)
{
    /// <summary>
    /// Gets the public event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    /// <summary>
    /// Gets the content-safe governed message or opaque content-segment target.
    /// </summary>
    public GovernanceTarget Target { get; } = GovernanceContractValidation.RequireNonNull(Target, nameof(Target));

    /// <summary>
    /// Gets the bounded redaction category.
    /// </summary>
    public RedactionCategory Category { get; } = GovernanceContractValidation.RequireNonNull(Category, nameof(Category));

    /// <summary>
    /// Gets the content-safe public policy reference.
    /// </summary>
    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the required content-safe governance rationale.
    /// </summary>
    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    /// <summary>
    /// Gets the safe audit evidence reference paired with the mutation.
    /// </summary>
    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        GovernanceContractValidation.RequireNonNull(AuditEvidence, nameof(AuditEvidence));

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(MessageContentRedacted))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", Target = ").Append(Target)
            .Append(", Category = ").Append(Category)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
