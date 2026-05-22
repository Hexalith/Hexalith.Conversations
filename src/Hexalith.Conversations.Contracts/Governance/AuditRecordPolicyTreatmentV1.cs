// <copyright file="AuditRecordPolicyTreatmentV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Describes governed treatment for an audit-record view without implying source-event deletion.
/// </summary>
public sealed record AuditRecordPolicyTreatmentV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    AuditEvidenceHandle AuditEvidenceHandle,
    ProjectionTrustState RetentionState,
    ProjectionTrustState RedactionState,
    AuditRecordActionClassification AccessDecision,
    bool ExportEligible,
    bool SeparateLogRequired,
    string PolicyReference,
    string SafeNextAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public AuditEvidenceHandle AuditEvidenceHandle { get; } =
        GovernanceContractValidation.RequireNonNull(AuditEvidenceHandle, nameof(AuditEvidenceHandle));

    public ProjectionTrustState RetentionState { get; } =
        GovernanceContractValidation.RequireNonNull(RetentionState, nameof(RetentionState));

    public ProjectionTrustState RedactionState { get; } =
        GovernanceContractValidation.RequireNonNull(RedactionState, nameof(RedactionState));

    public AuditRecordActionClassification AccessDecision { get; } =
        GovernanceContractValidation.RequireNonNull(AccessDecision, nameof(AccessDecision));

    public string PolicyReference { get; } =
        GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public string SafeNextAction { get; } =
        GovernanceContractValidation.RequiredSafeText(SafeNextAction, nameof(SafeNextAction));
}
