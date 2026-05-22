// <copyright file="ConversationContentMarkedSensitive.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Events;

/// <summary>
/// Publishes that governed conversation content metadata was marked sensitive.
/// </summary>
/// <remarks>
/// This append-only governance fact has mandatory safe audit evidence. It does not include raw
/// content, redact source events, delete history, enforce retention, or implement export/UI behavior.
/// </remarks>
/// <param name="metadata">The public event metadata.</param>
/// <param name="target">The content-safe governed target reference.</param>
/// <param name="category">The bounded sensitivity category.</param>
/// <param name="policyReference">The content-safe public policy reference.</param>
/// <param name="rationale">The required content-safe governance rationale.</param>
/// <param name="auditEvidence">The safe audit evidence reference paired with the mutation.</param>
public sealed record ConversationContentMarkedSensitive(
    ConversationEventMetadata Metadata,
    GovernanceTarget Target,
    SensitivityCategory Category,
    string PolicyReference,
    string Rationale,
    GovernanceAuditEvidenceReference AuditEvidence)
{
    /// <summary>
    /// Gets the public event metadata.
    /// </summary>
    public ConversationEventMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

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
    /// Gets the safe audit evidence reference paired with the mutation.
    /// </summary>
    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        GovernanceContractValidation.RequireNonNull(AuditEvidence, nameof(AuditEvidence));

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(ConversationContentMarkedSensitive))
            .Append(" { Metadata = ").Append(Metadata)
            .Append(", Target = ").Append(Target)
            .Append(", Category = ").Append(Category)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
