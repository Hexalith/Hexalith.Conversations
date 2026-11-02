using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Serialization;
using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

/// <summary>Compares generated schemas to actual owner scalar and query receiver protocols.</summary>
public sealed class ConversationsCatalogSchemaTest
{
    [Fact]
    public void OptionalQueryFieldsAndStrictNumberSchemasMatchActualPayloadDefaults()
    {
        _ = ConversationsCatalogAdapters.Create();
        var operations = ConversationsCatalogDeclarations.Operations;
        foreach (string wire in new[] { "conversation-list", "conversation-detail" })
        {
            var schema = operations.Single(operation => operation.MessageType == wire).RequestSchema;
            if (schema.TryGetProperty("required", out var required)) required.EnumerateArray().ShouldBeEmpty();
        }
        JsonSerializer.Deserialize<ListConversationsPayload>("{}")!.ShouldBe(new ListConversationsPayload());
        JsonSerializer.Deserialize<GetConversationPayload>("{}")!.ShouldBe(new GetConversationPayload());
        var pageSize = operations.Single(operation => operation.MessageType == "conversation-list").RequestSchema
            .GetProperty("properties").GetProperty("page").GetProperty("properties").GetProperty("pageSize");
        pageSize.GetProperty("type").GetString().ShouldBe("integer");
    }

    [Fact]
    public void ScalarConvertersHaveConcreteWireSchemasAndConstraintsParticipateInDigest()
    {
        _ = ConversationsCatalogAdapters.Create();
        var operations = ConversationsCatalogDeclarations.Operations;
        var route = ConversationsCatalogDeclarations.Commands.Single(command => command.Route.MessageType == nameof(AddParticipant)).Route;
        var operation = operations.Single(operation => operation.RouteId == route.RouteId);
        var request = operation.RequestSchema.GetProperty("properties").GetProperty("publicCommand").GetProperty("properties");
        var metadata = request.GetProperty("metadata").GetProperty("properties");
        foreach (var (schema, prefix) in new[] { (metadata.GetProperty("tenantId"), "tenant"), (metadata.GetProperty("actorPartyId"), "party"),
            (request.GetProperty("conversationId"), "conv"), (request.GetProperty("participantPartyId"), "party") })
        {
            schema.GetProperty("type").GetString().ShouldBe("string");
            string pattern = schema.GetProperty("pattern").GetString()!;
            Regex.IsMatch(prefix + ":identifier", pattern).ShouldBeTrue();
            Regex.IsMatch("foreign:identifier", pattern).ShouldBeFalse();
            Regex.IsMatch(prefix + ":   ", pattern).ShouldBeFalse();
        }
        var version = metadata.GetProperty("schemaVersion");
        version.GetProperty("type").GetString().ShouldBe("integer");
        version.GetProperty("minimum").GetInt32().ShouldBe(1);
        version.GetProperty("maximum").GetInt32().ShouldBe(int.MaxValue);
        request.GetProperty("participantType").GetProperty("enum").EnumerateArray().Select(value => value.GetString())
            .ShouldContain(JsonSerializer.SerializeToElement(ParticipantType.AiAgent).GetString());
        foreach (Type type in typeof(ConversationId).Assembly.GetExportedTypes().Where(type => type.GetCustomAttribute<System.Text.Json.Serialization.JsonConverterAttribute>() is not null))
        {
            // Every declared owner converter family is encountered directly, including less common vocabularies.
            var direct = RouteOperationSchema.Create(route, type, typeof(Hexalith.EventStore.Contracts.Commands.SubmitCommandResponse), "internal", "UI-only", EventStorePayloadSerialization.Options,
                (System.Text.Json.Schema.JsonSchemaExporterOptions)typeof(ConversationId).Assembly.GetType("Hexalith.Conversations.Contracts.Serialization.ConversationScalarSchemas")!
                    .GetProperty("ExporterOptions", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!);
            direct.RequestSchema.ValueKind.ShouldBe(JsonValueKind.Object, type.Name);
            direct.RequestSchema.GetProperty("type").GetString().ShouldBe(type == typeof(SchemaVersion) ? "integer" : "string");
        }
        var original = RouteOperationSchema.Create(route, typeof(AddParticipant), typeof(Hexalith.EventStore.Contracts.Commands.SubmitCommandResponse),
            "internal", "UI-only", EventStorePayloadSerialization.Options);
        operation.Digest.ShouldNotBe(original.Digest);
        JsonObject altered = JsonNode.Parse(operation.RequestSchema.GetRawText())!.AsObject();
        altered["properties"]!["publicCommand"]!["properties"]!["metadata"]!["properties"]!["tenantId"] = true;
        RouteCatalogCodec.ComputeOperationContractDigest(operation with { RequestSchema = JsonSerializer.SerializeToElement(altered) }).ShouldNotBe(operation.Digest);
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TenantId>("\"party:identifier\""));
        // JSON Schema integer constraints are structural; the converter still owns lexical token rules.
        foreach (string malformed in new[] { "\"1\"", "1.0", "1e0" })
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>(malformed));
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData("null", false)]
    [InlineData("{\"member\":1}", false)]
    public async Task DelegatedMetadataSchemasRequireExactlyTheObjectAcceptedByBothReceivers(string json, bool accepted)
    {
        _ = ConversationsCatalogAdapters.Create();
        foreach (var operation in ConversationsCatalogDeclarations.Operations.Where(operation => operation.MessageType is GetConversationResolutionMetadataQuery.QueryType or GetConversationAdvisoryMetadataQuery.QueryType))
        {
            operation.RequestSchema.GetProperty("type").GetString().ShouldBe("object");
            operation.RequestSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
        }
        var fixture = new ResolutionMetadataFixture();
        await fixture.ProjectAsync();
        var query = ResolutionMetadataFixture.Query() with { Payload = Encoding.UTF8.GetBytes(json) };
        (await fixture.DispatchAsync(query)).Success.ShouldBe(accepted);
        var advisory = new ResolutionMetadataFixture(); await advisory.ProjectAsync();
        (await advisory.DispatchAdvisoryAsync(query with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType })).Success.ShouldBe(accepted);
    }
}
