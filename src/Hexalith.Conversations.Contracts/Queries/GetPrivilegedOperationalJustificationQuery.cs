// <copyright file="GetPrivilegedOperationalJustificationQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Requests one tenant-scoped privileged-action justification record by safe audit handle.
/// </summary>
public sealed record GetPrivilegedOperationalJustificationQuery(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string CallerPrincipalId,
    string CorrelationId,
    ConversationId ConversationId,
    string AuditEvidenceHandle)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public string CallerPrincipalId { get; } = ValidateRequired(CallerPrincipalId, nameof(CallerPrincipalId));

    public string CorrelationId { get; } = GovernanceContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public string AuditEvidenceHandle { get; } = ValidateRequired(AuditEvidenceHandle, nameof(AuditEvidenceHandle));

    public override string ToString()
    {
        StringBuilder builder = new();
        builder
            .Append(nameof(GetPrivilegedOperationalJustificationQuery))
            .Append(" { SchemaVersion = ").Append(SchemaVersion)
            .Append(", TenantId = ").Append(TenantId)
            .Append(", ConversationId = ").Append(ConversationId)
            .Append(", CorrelationId = ").Append(CorrelationId)
            .Append(" }");
        return builder.ToString();
    }

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Length <= 128
            ? value
            : throw new ArgumentException("Value must be within the bounded public query length.", parameterName);
    }
}
