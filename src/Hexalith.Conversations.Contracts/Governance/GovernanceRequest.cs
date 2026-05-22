// <copyright file="GovernanceRequest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Describes a future governance mutation request without implementing command handling.
/// </summary>
/// <remarks>
/// Redaction requests represented by this contract are append-only and policy governed by default.
/// Event history, projected/displayed content, audit records, derived materializations, archival,
/// logical deletion, retention enforcement, and legal-hold deferral are distinct concepts. This
/// contract does not authorize irreversible source-event deletion.
/// </remarks>
/// <param name="metadata">The governance operation metadata.</param>
/// <param name="operationKind">The requested governance operation.</param>
/// <param name="target">The governed target reference.</param>
/// <param name="retentionAction">The optional retention action.</param>
/// <param name="sensitivityCategory">The optional sensitivity category.</param>
/// <param name="redactionCategory">The optional redaction category.</param>
/// <param name="archivalState">The optional archival state.</param>
/// <param name="legalHoldDeferral">The optional legal-hold deferral state.</param>
/// <param name="privilegedActionClass">The optional privileged action class.</param>
public sealed record GovernanceRequest(
    GovernanceOperationMetadata Metadata,
    GovernanceOperationKind OperationKind,
    GovernanceTarget Target,
    RetentionAction? RetentionAction = null,
    SensitivityCategory? SensitivityCategory = null,
    RedactionCategory? RedactionCategory = null,
    ArchivalState? ArchivalState = null,
    LegalHoldDeferral? LegalHoldDeferral = null,
    PrivilegedActionClass? PrivilegedActionClass = null)
{
    public GovernanceOperationMetadata Metadata { get; } = GovernanceContractValidation.RequireNonNull(Metadata, nameof(Metadata));

    public GovernanceOperationKind OperationKind { get; } = GovernanceContractValidation.RequireNonNull(OperationKind, nameof(OperationKind));

    public GovernanceTarget Target { get; } = GovernanceContractValidation.RequireNonNull(Target, nameof(Target));
}
