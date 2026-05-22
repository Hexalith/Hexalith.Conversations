// <copyright file="ConversationPublicationDiagnostic.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Publication;

/// <summary>
/// Describes a bounded publication compatibility or quarantine diagnostic.
/// </summary>
/// <param name="Code">The safe machine-readable diagnostic code.</param>
/// <param name="SchemaVersion">The observed schema version when available.</param>
/// <param name="EventType">The observed event type when available.</param>
/// <param name="TenantId">The event tenant identifier when safe to disclose.</param>
/// <param name="ConversationId">The event conversation identifier when available.</param>
/// <param name="EventId">The event identity when available.</param>
/// <param name="CorrelationId">The correlation identifier when available.</param>
/// <param name="CausationId">The causation identifier when available.</param>
public sealed record ConversationPublicationDiagnostic(
    ConversationErrorCode Code,
    SchemaVersion? SchemaVersion = null,
    ConversationEventType? EventType = null,
    TenantId? TenantId = null,
    ConversationId? ConversationId = null,
    string? EventId = null,
    string? CorrelationId = null,
    string? CausationId = null);
