// <copyright file="GetConversationAuditRecordQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Requests a governed, tenant-scoped audit-record view by safe audit evidence handle.
/// </summary>
public sealed record GetConversationAuditRecordQuery(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    string CallerPrincipalId,
    string CorrelationId,
    ConversationId ConversationId,
    string AuditEvidenceHandle,
    AuditRecordActionClassification RequestedAction)
{
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    public TenantId TenantId { get; } = TenantId ?? throw new ArgumentNullException(nameof(TenantId));

    public string CallerPrincipalId { get; } = ValidateRequired(CallerPrincipalId, nameof(CallerPrincipalId));

    public string CorrelationId { get; } = ValidateRequired(CorrelationId, nameof(CorrelationId));

    public ConversationId ConversationId { get; } = ConversationId ?? throw new ArgumentNullException(nameof(ConversationId));

    public string AuditEvidenceHandle { get; } = ValidateRequired(AuditEvidenceHandle, nameof(AuditEvidenceHandle));

    public AuditRecordActionClassification RequestedAction { get; } =
        RequestedAction ?? throw new ArgumentNullException(nameof(RequestedAction));

    public override string ToString()
        => $"{nameof(GetConversationAuditRecordQuery)} {{ SchemaVersion = {SchemaVersion}, RequestedAction = {RequestedAction} }}";

    private static string ValidateRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Length <= 128
            ? value
            : throw new ArgumentException("Value must be within the bounded public query length.", parameterName);
    }
}
