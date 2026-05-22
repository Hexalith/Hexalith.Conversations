// <copyright file="ConversationProjectionSnapshot.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Captures the duplicate-safe local projection state used as Story 1.6 evidence.
/// </summary>
/// <param name="TenantId">The tenant binding.</param>
/// <param name="ConversationId">The conversation identity.</param>
/// <param name="Lifecycle">The projected lifecycle state.</param>
/// <param name="Label">The safe projected label.</param>
/// <param name="BusinessReference">The safe projected business reference.</param>
/// <param name="ParticipantPartyIds">The stable participant Party identities.</param>
/// <param name="MessageIds">The stable message identities.</param>
/// <param name="FileIds">The stable file reference identities.</param>
/// <param name="Attributes">Safe projected metadata attributes.</param>
/// <param name="ProcessedEventIds">The processed public event identities.</param>
public sealed record ConversationProjectionSnapshot(
    TenantId? TenantId,
    ConversationId? ConversationId,
    ConversationProjectionLifecycleState Lifecycle,
    string? Label,
    BusinessReference? BusinessReference,
    IReadOnlyList<PartyId> ParticipantPartyIds,
    IReadOnlyList<MessageId> MessageIds,
    IReadOnlyList<FileId> FileIds,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyList<string> ProcessedEventIds,
    ConversationRetentionPolicyProjectionV1? ActiveRetentionPolicy = null);
