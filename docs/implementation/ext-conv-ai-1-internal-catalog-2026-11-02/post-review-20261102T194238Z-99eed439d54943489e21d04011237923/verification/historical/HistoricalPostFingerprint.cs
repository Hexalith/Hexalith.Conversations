using System.Text.Json;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Idempotency;
internal static class HistoricalPostFingerprint
{
    public static string PostFingerprint(AppendMessageCommand request, DateTimeOffset occurrence)
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
