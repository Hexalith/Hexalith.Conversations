// <copyright file="ConversationAuditRecordDetailsV1.cs" company="ITANEO">
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
/// Carries the public, policy-treated audit-record detail shape for authorized review.
/// </summary>
public sealed record ConversationAuditRecordDetailsV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    PartyId ActorPartyId,
    DateTimeOffset Timestamp,
    AuditRecordActionClassification ActionClass,
    GovernanceOutcome Outcome,
    string PolicyBasis,
    string RationaleClass,
    GovernanceTarget GovernedTarget,
    GovernanceAuditEvidenceReference AuditEvidence,
    AuditRecordPolicyTreatmentV1 PolicyTreatment,
    ProjectionFreshnessV1 Freshness,
    ProjectionTrustState VisibilityState,
    ProjectionFreshnessReasonCode ReasonCode,
    string CorrelationId,
    string? CausationId = null)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public PartyId ActorPartyId { get; } = ActorPartyId ?? throw new ArgumentNullException(nameof(ActorPartyId));

    public DateTimeOffset Timestamp { get; } = GovernanceContractValidation.RequiredUtcTimestamp(Timestamp, nameof(Timestamp));

    public AuditRecordActionClassification ActionClass { get; } =
        ActionClass ?? throw new ArgumentNullException(nameof(ActionClass));

    public GovernanceOutcome Outcome { get; } = Outcome ?? throw new ArgumentNullException(nameof(Outcome));

    public string PolicyBasis { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyBasis, nameof(PolicyBasis));

    public string RationaleClass { get; } = GovernanceContractValidation.RequiredSafeText(RationaleClass, nameof(RationaleClass));

    public GovernanceTarget GovernedTarget { get; } = GovernedTarget ?? throw new ArgumentNullException(nameof(GovernedTarget));

    public GovernanceAuditEvidenceReference AuditEvidence { get; } =
        AuditEvidence ?? throw new ArgumentNullException(nameof(AuditEvidence));

    public AuditRecordPolicyTreatmentV1 PolicyTreatment { get; } =
        PolicyTreatment ?? throw new ArgumentNullException(nameof(PolicyTreatment));

    public ProjectionFreshnessV1 Freshness { get; } = Freshness ?? throw new ArgumentNullException(nameof(Freshness));

    public ProjectionTrustState VisibilityState { get; } =
        VisibilityState ?? throw new ArgumentNullException(nameof(VisibilityState));

    public ProjectionFreshnessReasonCode ReasonCode { get; } =
        ReasonCode ?? throw new ArgumentNullException(nameof(ReasonCode));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public string? CausationId { get; } = GovernanceContractValidation.OptionalSafeToken(CausationId, nameof(CausationId));
}
