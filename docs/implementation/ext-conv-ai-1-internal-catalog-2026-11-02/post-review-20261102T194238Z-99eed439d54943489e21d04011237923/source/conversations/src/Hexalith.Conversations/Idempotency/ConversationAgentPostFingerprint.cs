using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Idempotency;

/// <summary>The aggregate's original posting fingerprint, shared without changing its canonical protocol.</summary>
public static class ConversationAgentPostFingerprint
{
    /// <summary>Computes the original posting fingerprint shared by aggregate retry and internal catalog intent.</summary>
    /// <param name="request">The original portable posting request.</param>
    /// <param name="occurrence">The exact persisted occurrence.</param>
    /// <returns>The unchanged canonical original-post fingerprint.</returns>
    public static string Create(AppendMessageCommand request, DateTimeOffset occurrence)
    {
        var parts = new List<KeyValuePair<string, string?>> {
            new("message", request.MessageId.Value), new("author", request.AuthorPartyId.Value),
            new("text", request.Text), new("occurrence", occurrence.ToString("O")),
            new("key", request.Metadata.IdempotencyKey) };
        AddParts(parts, "provenance", JsonSerializer.SerializeToElement(request.AgentProvenance));
        AddParts(parts, "provider", JsonSerializer.SerializeToElement(request.ProviderCorrelation));
        AddParts(parts, "caller", JsonSerializer.SerializeToElement(request.CallerMetadata));
        return ConversationPayloadFingerprint.FromParts(parts).Value;
    }

    private static void AddParts(List<KeyValuePair<string, string?>> parts, string prefix, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                AddParts(parts, $"{prefix}.{property.Name}", property.Value);
            }
        }
        else
        {
            parts.Add(new(prefix, value.GetRawText()));
        }
    }

}
