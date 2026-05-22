// <copyright file="PrivilegedOperationalJustificationV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Carries structured justification required before privileged tenant conversation-data operations.
/// </summary>
public sealed record PrivilegedOperationalJustificationV1(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId? ConversationId,
    GovernanceTarget GovernedScope,
    PartyId ActorPartyId,
    PrivilegedOperationalActionClass OperationClass,
    PrivilegedActionClass PrivilegedActionClass,
    string PolicyReference,
    string Rationale,
    DateTimeOffset OperationTimestamp,
    string CorrelationId,
    string? CausationId = null,
    GovernanceAuditEvidenceReference? AffectedAuditEvidence = null)
{
    public SchemaVersion SchemaVersion { get; } = GovernanceContractValidation.RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    public TenantId TenantId { get; } = GovernanceContractValidation.RequireNonNull(TenantId, nameof(TenantId));

    public GovernanceTarget GovernedScope { get; } = GovernanceContractValidation.RequireNonNull(GovernedScope, nameof(GovernedScope));

    public PartyId ActorPartyId { get; } = GovernanceContractValidation.RequireNonNull(ActorPartyId, nameof(ActorPartyId));

    public PrivilegedOperationalActionClass OperationClass { get; } =
        GovernanceContractValidation.RequireNonNull(OperationClass, nameof(OperationClass));

    public PrivilegedActionClass PrivilegedActionClass { get; } =
        GovernanceContractValidation.RequireNonNull(PrivilegedActionClass, nameof(PrivilegedActionClass));

    public string PolicyReference { get; } = GovernanceContractValidation.RequiredSafeToken(PolicyReference, nameof(PolicyReference));

    public string Rationale { get; } = GovernanceContractValidation.RequiredSafeText(Rationale, nameof(Rationale));

    public DateTimeOffset OperationTimestamp { get; } =
        GovernanceContractValidation.RequiredUtcTimestamp(OperationTimestamp, nameof(OperationTimestamp));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public string? CausationId { get; } = GovernanceContractValidation.OptionalSafeToken(CausationId, nameof(CausationId));

    public GovernanceAuditEvidenceReference? AffectedAuditEvidence { get; } = AffectedAuditEvidence;

    // Rationale, policy, and audit reference are omitted to keep logs content-safe.
    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(PrivilegedOperationalJustificationV1))
            .Append(" { SchemaVersion = ").Append(SchemaVersion)
            .Append(", TenantId = ").Append(TenantId)
            .Append(", ConversationId = ").Append(ConversationId?.ToString() ?? "<scope>")
            .Append(", ActorPartyId = ").Append(ActorPartyId)
            .Append(", OperationClass = ").Append(OperationClass)
            .Append(", PrivilegedActionClass = ").Append(PrivilegedActionClass)
            .Append(", OperationTimestamp = ").Append(OperationTimestamp.ToString("O"))
            .Append(", CorrelationId = ").Append(CorrelationId)
            .Append(", CausationId = ").Append(CausationId ?? "<none>")
            .Append(" }");
        return builder.ToString();
    }
}
