// <copyright file="RetentionPolicyReplaced.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Publishes that a governed conversation retention policy was replaced.
/// </summary>
/// <param name="metadata">The public event metadata.</param>
/// <param name="policyReference">The new content-safe public retention policy reference.</param>
/// <param name="previousPolicyReference">The previous content-safe public retention policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="auditEvidence">The safe audit evidence reference paired with the mutation.</param>
public sealed record RetentionPolicyReplaced(
    ConversationEventMetadata Metadata,
    string PolicyReference,
    string PreviousPolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence)
{
    /// <summary>
    /// Gets the public event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    /// <summary>
    /// Gets the new content-safe public retention policy reference.
    /// </summary>
    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the previous content-safe public retention policy reference.
    /// </summary>
    public string PreviousPolicyReference { get; } =
        GovernanceContractValidation.RequiredSafeToken(PreviousPolicyReference, nameof(PreviousPolicyReference));

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
            .Append(nameof(RetentionPolicyReplaced))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
