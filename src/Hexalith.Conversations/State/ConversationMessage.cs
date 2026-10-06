// <copyright file="ConversationMessage.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Agents;

namespace Hexalith.Conversations.State;

/// <summary>
/// Represents a replayed message using durable Conversations identifiers and content only.
/// </summary>
/// <param name="MessageId">The stable message identity.</param>
/// <param name="AuthorPartyId">The stable author Party reference.</param>
/// <param name="Text">The message text copied from the persisted event.</param>
/// <param name="CreatedAt">The deterministic message timestamp.</param>
/// <param name="ProviderCorrelation">Optional provider correlation metadata that is not authority.</param>
/// <param name="Provenance">Current provenance, retaining the original Agent Call reference.</param>
/// <param name="IdempotencyKey">Original immutable posting intent key.</param>
/// <param name="EditedAt">Latest persisted edit occurrence.</param>
/// <param name="Deleted">Whether current content is deleted.</param>
/// <param name="OriginalText">Original immutable posting text for retry comparison.</param>
/// <param name="OriginalProvenance">Original immutable provenance for retry comparison.</param>
/// <param name="OriginalIntentFingerprint">Immutable full posting intent fingerprint.</param>
public sealed record ConversationMessage(
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    DateTimeOffset CreatedAt,
    ProviderCorrelationMetadata? ProviderCorrelation = null,
    AgentMessageProvenance? Provenance = null,
    string? IdempotencyKey = null,
    DateTimeOffset? EditedAt = null,
    bool Deleted = false,
    string? OriginalText = null,
    AgentMessageProvenance? OriginalProvenance = null,
    string? OriginalIntentFingerprint = null);
