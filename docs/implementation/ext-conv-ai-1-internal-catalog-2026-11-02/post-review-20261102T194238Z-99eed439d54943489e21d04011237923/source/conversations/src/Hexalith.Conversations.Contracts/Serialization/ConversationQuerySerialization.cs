using System.Text.Json;

namespace Hexalith.Conversations.Contracts.Serialization;

/// <summary>Owner-internal strict JSON settings shared by ordinary query receivers and their catalog schemas.</summary>
internal static class ConversationQuerySerialization
{
    /// <summary>Gets the existing case-sensitive, strict-number ordinary query protocol.</summary>
    internal static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
