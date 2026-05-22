// <copyright file="UnavailablePrivilegedOperationalJustificationReviewSource.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Default fail-closed privileged justification review source used until a host supplies a durable source.
/// </summary>
internal sealed class UnavailablePrivilegedOperationalJustificationReviewSource : IPrivilegedOperationalJustificationReviewSource
{
    public static UnavailablePrivilegedOperationalJustificationReviewSource Instance { get; } = new();

    private UnavailablePrivilegedOperationalJustificationReviewSource()
    {
    }

    public ValueTask<PrivilegedOperationalJustificationDetailsV1?> ReadAsync(
        TenantId tenantId,
        ConversationId conversationId,
        AuditEvidenceHandle auditEvidenceHandle,
        CancellationToken cancellationToken = default)
        => throw new IOException("Privileged operational justification review source is not configured.");
}
