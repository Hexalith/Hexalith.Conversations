// <copyright file="PrivilegedOperationalJustificationDetailsV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries one coherent privileged-action justification record for authorized review.
/// </summary>
public sealed record PrivilegedOperationalJustificationDetailsV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId? ConversationId,
    GovernanceTarget GovernedScope,
    PartyId ActorPartyId,
    PrivilegedOperationalActionClass OperationClass,
    PrivilegedActionClass PrivilegedActionClass,
    string PolicyReference,
    string Rationale,
    DateTimeOffset Timestamp,
    GovernanceOutcome Outcome,
    GovernanceAuditEvidenceReference AuditEvidence,
    ProjectionTrustState VisibilityState,
    ProjectionFreshnessV1 Freshness,
    string SafeNextAction,
    string CorrelationId,
    string? CausationId = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public GovernanceTarget GovernedScope { get; } = GovernedScope ?? throw new ArgumentNullException(nameof(GovernedScope));

    public PartyId ActorPartyId { get; } = ActorPartyId ?? throw new ArgumentNullException(nameof(ActorPartyId));

    public PrivilegedOperationalActionClass OperationClass { get; } =
        OperationClass ?? throw new ArgumentNullException(nameof(OperationClass));

    public PrivilegedActionClass PrivilegedActionClass { get; } =
        PrivilegedActionClass ?? throw new ArgumentNullException(nameof(PrivilegedActionClass));

    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    public DateTimeOffset Timestamp { get; } = GovernanceContractValidation.RequiredUtcTimestamp(Timestamp, nameof(Timestamp));

    public GovernanceOutcome Outcome { get; } = Outcome ?? throw new ArgumentNullException(nameof(Outcome));

    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        AuditEvidence ?? throw new ArgumentNullException(nameof(AuditEvidence));

    public ProjectionTrustState VisibilityState { get; } =
        VisibilityState ?? throw new ArgumentNullException(nameof(VisibilityState));

    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    public string SafeNextAction { get; } = GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public string? CausationId { get; } = GovernanceContractValidation.OptionalSafeToken(CausationId, nameof(CausationId));
}
