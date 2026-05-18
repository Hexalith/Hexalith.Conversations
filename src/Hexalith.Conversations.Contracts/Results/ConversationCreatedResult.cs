// <copyright file="ConversationCreatedResult.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Results;

/// <summary>
/// Reports the assigned tenant-scoped conversation identity for create operations.
/// </summary>
/// <param name="schemaVersion">The result schema version.</param>
/// <param name="tenantId">The tenant binding.</param>
/// <param name="conversationId">The assigned tenant-scoped conversation identity.</param>
/// <param name="correlationId">The accepted correlation identifier.</param>
/// <param name="idempotencyKey">The accepted idempotency key, when provided.</param>
/// <param name="visibility">The read-model visibility caveat.</param>
public sealed record ConversationCreatedResult(
    SchemaVersion SchemaVersion,
    TenantId TenantId,
    ConversationId ConversationId,
    string CorrelationId,
    string? IdempotencyKey,
    ReadModelVisibility Visibility);
