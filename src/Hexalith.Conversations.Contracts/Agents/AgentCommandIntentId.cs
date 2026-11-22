// <copyright file="AgentCommandIntentId.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;

namespace Hexalith.Conversations.Contracts.Agents;

/// <summary>Allocates stable command and event identities for restricted Agent intents.</summary>
internal static class AgentCommandIntentId
{
    /// <summary>Computes the existing wire identity from the exact scoped command intent.</summary>
    internal static string Create(ConversationCommandMetadata metadata, ConversationId conversation, string purpose)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new[] { metadata.TenantId.Value, conversation.Value, metadata.ActorPartyId.Value, purpose, metadata.IdempotencyKey })));
}
