// <copyright file="GovernanceAuditEvidence.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Pairs a governance operation with public audit evidence and an explicit outcome state.
/// </summary>
/// <remarks>
/// A timestamp, correlation identifier, or evidence handle alone is not proof of a successful mutation.
/// Consumers must inspect <see cref="Outcome"/> to distinguish success, denial, audit-unavailable failure,
/// and policy-blocked results.
/// </remarks>
/// <param name="metadata">The governance operation metadata.</param>
/// <param name="operationKind">The governance operation family.</param>
/// <param name="target">The governed target reference.</param>
/// <param name="outcome">The public governance outcome.</param>
/// <param name="auditEvidence">The safe audit evidence reference.</param>
/// <param name="remediation">The optional bounded remediation class.</param>
public sealed record GovernanceAuditEvidence(
    GovernanceOperationMetadata Metadata,
    GovernanceOperationKind OperationKind,
    GovernanceTarget Target,
    GovernanceOutcome Outcome,
    GovernanceAuditEvidenceReference AuditEvidence,
    GovernanceRemediation? Remediation = null)
{
    public GovernanceOperationMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    public GovernanceOperationKind OperationKind { get; } = GovernanceContractValidation.RequireNonNull(OperationKind, nameof(OperationKind));

    public GovernanceTarget Target { get; } = GovernanceContractValidation.RequireNonNull(Target, nameof(Target));

    public GovernanceOutcome Outcome { get; } = GovernanceContractValidation.RequireNonNull(Outcome, nameof(Outcome));

    public GovernanceAuditEvidenceReference AuditEvidence { get; } = GovernanceContractValidation.RequireNonNull(AuditEvidence, nameof(AuditEvidence));
}
