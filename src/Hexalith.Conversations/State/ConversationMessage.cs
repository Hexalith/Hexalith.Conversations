// <copyright file="ConversationMessage.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.State;

/// <summary>
/// Represents a replayed message using durable Conversations identifiers and content only.
/// </summary>
/// <param name="MessageId">The stable message identity.</param>
/// <param name="AuthorPartyId">The stable author Party reference.</param>
/// <param name="Text">The message text copied from the persisted event.</param>
/// <param name="CreatedAt">The deterministic message timestamp.</param>
/// <param name="ProviderCorrelation">Optional provider correlation metadata that is not authority.</param>
public sealed record ConversationMessage(
    MessageId MessageId,
    PartyId AuthorPartyId,
    string Text,
    DateTimeOffset CreatedAt,
    ProviderCorrelationMetadata? ProviderCorrelation = null);
