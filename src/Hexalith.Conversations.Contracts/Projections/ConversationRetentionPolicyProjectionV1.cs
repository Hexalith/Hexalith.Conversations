// <copyright file="ConversationRetentionPolicyProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Describes the derived active retention policy state for authorized read paths.
/// </summary>
/// <remarks>
/// This projection is descriptive and rebuildable. It is not authoritative for command decisions and
/// must not schedule deletion, redaction, expiration, legal-hold changes, or workflow side effects.
/// </remarks>
/// <param name="policyReference">The active content-safe retention policy reference.</param>
/// <param name="rationale">The active content-safe governance rationale.</param>
/// <param name="actorPartyId">The stable Party actor attribution.</param>
/// <param name="appliedAt">The operation timestamp recorded by the accepted event.</param>
/// <param name="auditEvidence">The safe audit evidence paired with the accepted mutation.</param>
/// <param name="previousPolicyReference">The previous public retention policy reference when this state came from a replacement.</param>
public sealed record ConversationRetentionPolicyProjectionV1(
    string PolicyReference,
    string Rationale,
    PartyId ActorPartyId,
    DateTimeOffset AppliedAt,
    GovernanceAuditEvidenceReference AuditEvidence,
    string? PreviousPolicyReference = null)
{
    /// <summary>
    /// Gets the active content-safe retention policy reference.
    /// </summary>
    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    /// <summary>
    /// Gets the active content-safe governance rationale.
    /// </summary>
    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    /// <summary>
    /// Gets the stable Party actor attribution.
    /// </summary>
    public PartyId ActorPartyId { get; } = GovernanceContractValidation.RequireNonNull(ActorPartyId, nameof(ActorPartyId));

    /// <summary>
    /// Gets the operation timestamp recorded by the accepted event.
    /// </summary>
    public DateTimeOffset AppliedAt { get; } = GovernanceContractValidation.RequiredUtcTimestamp(AppliedAt, nameof(AppliedAt));

    /// <summary>
    /// Gets the safe audit evidence paired with the accepted mutation.
    /// </summary>
    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        GovernanceContractValidation.RequireNonNull(AuditEvidence, nameof(AuditEvidence));

    /// <summary>
    /// Gets the previous public retention policy reference when this state came from a replacement.
    /// </summary>
    public string? PreviousPolicyReference { get; } =
        GovernanceContractValidation.OptionalSafeToken(PreviousPolicyReference, nameof(PreviousPolicyReference));

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(ConversationRetentionPolicyProjectionV1))
            .Append(" { ActorPartyId = ").Append(ActorPartyId)
            .Append(", AppliedAt = ").Append(AppliedAt.ToString("O"))
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
