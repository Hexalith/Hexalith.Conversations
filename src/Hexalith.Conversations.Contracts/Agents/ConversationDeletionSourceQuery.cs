// <copyright file="ConversationDeletionSourceQuery.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Exact authenticated source-revision and acknowledgement lookup.</summary>
/// <param name="TenantId">Exact tenant.</param>
/// <param name="ConversationId">Exact source.</param>
/// <param name="SourceRevision">Optional original approval revision compare.</param>
/// <param name="SignalId">Optional immutable signal compare.</param>
public sealed record ConversationDeletionSourceQuery(TenantId TenantId, ConversationId ConversationId,
    long? SourceRevision = null, string? SignalId = null);
