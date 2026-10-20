using System.Text.Json;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Serialization;
using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Queries;

public sealed class ConversationResolutionMetadataContractTest
{
    [Fact]
    public void QueryIsParameterlessAndSerializesAsEmptyObject()
    {
        GetConversationResolutionMetadataQuery.QueryType.ShouldBe("Hexalith.Conversations.Queries.GetConversationResolutionMetadata.v1");
        JsonSerializer.Serialize(new GetConversationResolutionMetadataQuery()).ShouldBe("{}");
        JsonSerializer.Deserialize<GetConversationResolutionMetadataQuery>("{}").ShouldNotBeNull();
        JsonSerializer.Serialize(new GetConversationResolutionMetadataQuery(), ConversationsJsonContext.Default.GetConversationResolutionMetadataQuery).ShouldBe("{}");
    }

    [Fact]
    public void SubsetAndUnavailableResultHaveOnlyTheAnnexFields()
    {
        ConversationResolutionMetadataV1 metadata = new("conversation", "project", " exact label ", "Open");
        JsonElement json = JsonSerializer.SerializeToElement(metadata, JsonSerializerOptions.Web);
        json.EnumerateObject().Select(property => property.Name).ShouldBe(["conversationId", "projectId", "safeLabel", "lifecycleState"]);
        JsonSerializer.Deserialize<ConversationResolutionMetadataV1>(json, JsonSerializerOptions.Web).ShouldBe(metadata);
        ConversationResolutionMetadataResultV1 unavailable = new(null, null, null, null, null);
        JsonElement result = JsonSerializer.SerializeToElement(unavailable, JsonSerializerOptions.Web);
        result.EnumerateObject().Select(property => property.Name).ShouldBe(["metadata", "freshness", "resourceVersion", "permissionVersion", "authorizedAt"]);
        result.EnumerateObject().ShouldAllBe(property => property.Value.ValueKind == JsonValueKind.Null);
        JsonSerializer.Deserialize<ConversationResolutionMetadataResultV1>("{}", JsonSerializerOptions.Web).ShouldBe(unavailable);
        JsonSerializer.Deserialize(JsonSerializer.Serialize(unavailable, ConversationsJsonContext.Default.ConversationResolutionMetadataResultV1),
            ConversationsJsonContext.Default.ConversationResolutionMetadataResultV1).ShouldBe(unavailable);
    }
}
