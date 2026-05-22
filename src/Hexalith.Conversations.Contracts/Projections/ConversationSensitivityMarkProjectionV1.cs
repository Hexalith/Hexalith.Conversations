// <copyright file="ConversationSensitivityMarkProjectionV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Describes derived sensitivity state for an authorized read path.
/// </summary>
/// <remarks>
/// This projection is descriptive and rebuildable. It is not authoritative for command decisions
/// and carries only safe category, target, policy, audit, and freshness/trust metadata.
/// </remarks>
/// <param name="target">The content-safe governed target reference.</param>
/// <param name="category">The bounded sensitivity category.</param>
/// <param name="policyReference">The content-safe public policy reference.</param>
/// <param name="rationale">The content-safe governance rationale.</param>
/// <param name="actorPartyId">The stable Party actor attribution.</param>
/// <param name="markedAt">The operation timestamp recorded by the accepted event.</param>
/// <param name="auditEvidence">The safe audit evidence paired with the accepted mutation.</param>
/// <param name="trustState">The derived trust state for this sensitivity state.</param>
public sealed record ConversationSensitivityMarkProjectionV1(
    GovernanceTarget Target,
    SensitivityCategory Category,
    string PolicyReference,
    string Rationale,
    PartyId ActorPartyId,
    DateTimeOffset MarkedAt,
    GovernanceAuditEvidenceReference AuditEvidence,
    ProjectionTrustState TrustState)
{
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
    /// Gets the content-safe governance rationale.
    /// </summary>
    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    /// <summary>
    /// Gets the stable Party actor attribution.
    /// </summary>
    public PartyId ActorPartyId { get; } = GovernanceContractValidation.RequireNonNull(ActorPartyId, nameof(ActorPartyId));

    /// <summary>
    /// Gets the operation timestamp recorded by the accepted event.
    /// </summary>
    public DateTimeOffset MarkedAt { get; } = GovernanceContractValidation.RequiredUtcTimestamp(MarkedAt, nameof(MarkedAt));

    /// <summary>
    /// Gets the safe audit evidence paired with the accepted mutation.
    /// </summary>
    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        GovernanceContractValidation.RequireNonNull(AuditEvidence, nameof(AuditEvidence));

    /// <summary>
    /// Gets the derived trust state for this sensitivity state.
    /// </summary>
    public ProjectionTrustState TrustState { get; } = GovernanceContractValidation.RequireNonNull(TrustState, nameof(TrustState));

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(ConversationSensitivityMarkProjectionV1))
            .Append(" { Target = ").Append(Target)
            .Append(", Category = ").Append(Category)
            .Append(", ActorPartyId = ").Append(ActorPartyId)
            .Append(", MarkedAt = ").Append(MarkedAt.ToString("O"))
            .Append(", TrustState = ").Append(TrustState)
            .Append(", AuditEvidence = ").Append(AuditEvidence)
            .Append(" }");
        return builder.ToString();
    }
}
