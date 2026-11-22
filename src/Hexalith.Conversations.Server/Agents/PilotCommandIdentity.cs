using System.Security.Cryptography;
using System.Text.Json;
using ByteAether.Ulid;
using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Stable owner allocations for one authenticated pilot command.</summary>
internal static class PilotCommandIdentity
{
    internal static string Allocate(ConversationCommandMetadata metadata, string subject, string purpose, string? conversation = null)
    {
        byte[] digest = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            new[] { "conversations-api-v1", metadata.TenantId.Value, subject, purpose, conversation, metadata.IdempotencyKey }));
        return Ulid.New(digest.AsSpan(0, 16)).ToString();
    }
}
