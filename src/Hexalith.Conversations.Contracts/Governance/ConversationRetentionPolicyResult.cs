// <copyright file="ConversationRetentionPolicyResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Reports the sanitized outcome of a governed retention policy command.
/// </summary>
/// <param name="schemaVersion">The result schema version.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The governed conversation identity.</param>
/// <param name="outcome">The bounded governance outcome.</param>
/// <param name="correlationId">The safe correlation identifier.</param>
/// <param name="auditEvidence">The safe audit evidence reference when policy allows disclosure.</param>
/// <param name="error">The optional Conversations-safe error.</param>
/// <param name="remediation">The optional bounded remediation class.</param>
public sealed record ConversationRetentionPolicyResult(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    GovernanceOutcome Outcome,
    string CorrelationId,
    GovernanceAuditEvidenceReference? AuditEvidence = null,
    ConversationError? Error = null,
    GovernanceRemediation? Remediation = null)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = GovernanceContractValidation.RequireNonNull(SchemaVersion, nameof(SchemaVersion));

    /// <summary>
    /// Gets the tenant binding.
    /// </summary>
    public TenantId TenantId { get; } = GovernanceContractValidation.RequireNonNull(TenantId, nameof(TenantId));

    /// <summary>
    /// Gets the governed conversation identity.
    /// </summary>
    public ConversationId ConversationId { get; } = GovernanceContractValidation.RequireNonNull(ConversationId, nameof(ConversationId));

    /// <summary>
    /// Gets the bounded governance outcome.
    /// </summary>
    public GovernanceOutcome Outcome { get; } = GovernanceContractValidation.RequireNonNull(Outcome, nameof(Outcome));

    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));
}
