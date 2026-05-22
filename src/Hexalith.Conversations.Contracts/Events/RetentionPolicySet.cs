// <copyright file="RetentionPolicySet.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Publishes that a governed conversation retention policy was set.
/// </summary>
/// <remarks>
/// This is an append-only governance fact with mandatory safe audit evidence. It does not
/// authorize retention enforcement, deletion, redaction, legal-hold changes, or UI workflows.
/// </remarks>
/// <param name="metadata">The public event metadata.</param>
/// <param name="policyReference">The content-safe public retention policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="auditEvidence">The safe audit evidence reference paired with the mutation.</param>
public sealed record RetentionPolicySet(
    ConversationEventMetadata Metadata,
    string PolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence)
{
    /// <summary>
    /// Gets the public event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    /// <summary>
    /// Gets the content-safe public retention policy reference.
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
            .Append(nameof(RetentionPolicySet))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
