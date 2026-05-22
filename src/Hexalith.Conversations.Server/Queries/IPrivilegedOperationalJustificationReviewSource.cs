// <copyright file="IPrivilegedOperationalJustificationReviewSource.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Reads privileged operational justification review records from an existing audit/projection-safe source.
/// </summary>
public interface IPrivilegedOperationalJustificationReviewSource
{
    /// <summary>
    /// Reads one privileged justification record by safe audit evidence handle.
    /// </summary>
    ValueTask<PrivilegedOperationalJustificationDetailsV1?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        AuditEvidenceHandle auditEvidenceHandle,
        CancellationToken cancellationToken = default);
}
