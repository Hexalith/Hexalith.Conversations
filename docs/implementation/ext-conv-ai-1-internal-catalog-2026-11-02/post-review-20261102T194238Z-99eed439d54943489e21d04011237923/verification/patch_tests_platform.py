from pathlib import Path
r=Path('/home/administrator/projects/hexalith/conversations');p=Path('/home/administrator/projects/hexalith/platform')
def edit(root,n,a,b):
 f=root/n;s=f.read_text();assert a in s,(n,a);raw=f.read_bytes();v=s.replace(a,b);f.write_bytes(v.replace('\n','\r\n').encode() if b'\r\n' in raw else v.encode())
def write(root,n,s):
 f=root/n;f.parent.mkdir(exist_ok=True,parents=True);f.write_bytes(s.replace('\n','\r\n').encode())
edit(r,'tests/Hexalith.Conversations.Server.Tests/Queries/ConversationDomainQueryDispatchTest.cs','    private static ServiceProvider BuildProvider()', '''    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\\"page\\":{\\"pageSize\\":10}}", true)]
    [InlineData("{\\"page\\":{\\"pageSize\\":\\"10\\"}}", false)]
    public async Task OrdinaryPayloadOptionalityAndStrictNumbersMatchCatalog(string json, bool accepted)
    {
        await using ServiceProvider provider = BuildProvider();
        using var scope = provider.CreateScope();
        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider,
            new QueryEnvelope(Tenant.Value, "conversations", "conversation-001", "conversation-list", Encoding.UTF8.GetBytes(json), "correlation", "actor"),
            TestContext.Current.CancellationToken);
        result.Success.ShouldBe(accepted);
        if (json == "{}")
        {
            result = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider,
                new QueryEnvelope(Tenant.Value, "conversations", "conversation-001", "conversation-detail", Encoding.UTF8.GetBytes(json), "correlation", "actor"),
                TestContext.Current.CancellationToken);
            result.Success.ShouldBeTrue();
        }
    }

    private static ServiceProvider BuildProvider()''')
write(r,'tests/Hexalith.Conversations.Server.Tests/Routing/ConversationsCatalogSchemaTest.cs','''using System.Reflection;
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
        var list = operations.Single(operation => operation.MessageType == "conversation-list").RequestSchema.GetRawText();
        list.ShouldNotContain("integer\\\",\\\"string");
        list.ShouldNotContain("AllowReadingFromString");
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
            var direct = RouteOperationSchema.Create(route, type, typeof(object), "internal", "UI-only", EventStorePayloadSerialization.Options,
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
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<TenantId>("\\\"party:identifier\\\""));
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<SchemaVersion>("\\\"1\\\""));
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData("null", false)]
    [InlineData("{\\"member\\":1}", false)]
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
        (await fixture.DispatchAdvisoryAsync(query with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType })).Success.ShouldBe(accepted);
    }
}
''')
# Null semantics: all thirteen real fixtures, eleven wrappers plus direct commands.
f=r/'tests/Hexalith.Conversations.Server.Tests/Routing/ConversationsCatalogIntentTest.cs';s=f.read_text();pos=s.index('    private static IdempotencyCanonicalIntent Intent(');s=s[:pos]+'''    [Fact]
    public void MissingRequiredWrapperAndMetadataFieldsAreInvalidArgumentsForEveryReceiver()
    {
        var adapters = ConversationsCatalogAdapters.Create();
        foreach (var (payload, _) in Cases())
        {
            var adapter = adapters.Single(candidate => candidate.Declaration.Route.Execution!.HandlerType == payload.GetType().FullName);
            var original = JsonNode.Parse(JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), EventStorePayloadSerialization.Options))!;
            foreach (string path in original["publicCommand"] is null ? new[] { "metadata" } : new[] { "publicCommand", "publicCommand.metadata" })
            {
                var invalid = original.DeepClone(); Set(invalid, path, null);
                Should.Throw<ArgumentException>(() => Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(invalid)), payload.GetType().Name + " " + path);
            }
        }
    }

'''+s[pos:];f.write_bytes(s.replace('\n','\r\n').encode())
# Receipt is the actual loaded subset of the selected reader output, including schema/fingerprint dependencies.
edit(p,'src/Hexalith.Platform.CatalogBuilder/Program.cs','    loadedArtifacts = owners.Concat(adapters).Append(typeof(RouteCatalogCodec).Assembly.Location).Distinct(StringComparer.Ordinal).Select(path => new','''    // This records actual reader-output dependencies, not an installed execution image measurement.
    loadedArtifacts = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic && assembly.Location.Length > 0
        && Path.GetDirectoryName(assembly.Location) == Path.GetDirectoryName(typeof(RouteCatalogCodec).Assembly.Location))
        .Select(assembly => assembly.Location).Concat(owners).Concat(adapters).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Select(path => new''')
# Normal CI lane: exercise existing genuine fixture adapter swaps, no new synthetic owner.
edit(p,'tests/catalog-builder/verify_catalog_builder.py','        before = json.loads((args.evidence / "baseline-0-catalog.json").read_text())','''        for index, replacement in enumerate([owners[0]["assembly"], owners[1]["adapterAssembly"]]):
            target = work / f"invalid-adapter-{index}"
            target.mkdir()
            wrong = [dict(owner) for owner in owners]
            wrong[0]["adapterAssembly"] = replacement
            request = {"generation": 1, "instanceId": "catalog-binding-check", "gatewayAppId": "eventstore-binding-check",
                       "outputDirectory": str(target), "owners": wrong}
            path = work / f"invalid-adapter-{index}.json"
            path.write_text(json.dumps(request))
            execute(["dotnet", str(outputs["actual"]), str(path)], args.evidence / f"invalid-adapter-{index}.log", expected=1)
            assert not list(target.iterdir()), "Invalid adapter emitted output"
            cases.append({"case": f"invalid-adapter-{index}", "refusedBeforeOutput": True})
        before = json.loads((args.evidence / "baseline-0-catalog.json").read_text())''')
# Conversations-specific schema owner refusals, alongside generic owner negatives.
f=p/'tests/Hexalith.Platform.Composition.Tests/ConversationsCatalogSourceVerificationTests.cs';s=f.read_text();needle='            // Execute the actual gateway Program registration.';pos=s.index(needle);s=s[:pos]+'''            foreach (var (label, wrongAdapter) in new[] { ("conversations-missing-adapter", "Hexalith.Conversations.Contracts"),
                ("conversations-foreign-adapter", "Hexalith.Tenants.Server") })
            {
                JsonObject request = JsonNode.Parse(File.ReadAllBytes(Path.Combine(reader, "request.json")))!.AsObject();
                request["owners"]!.AsArray().Single(owner => owner!["assembly"]!.GetValue<string>() == "Hexalith.Conversations.Contracts")!["adapterAssembly"] = wrongAdapter;
                string output = Path.Combine(workspace, label); Directory.CreateDirectory(output); request["outputDirectory"] = output;
                string negativeInput = Path.Combine(evidence, label + "-request.json"); File.WriteAllText(negativeInput, request.ToJsonString());
                var refusal = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                    [Path.Combine(reader, "output", "Hexalith.Platform.CatalogBuilder.dll"), negativeInput], reader, null), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
                File.WriteAllText(Path.Combine(evidence, label + ".log"), refusal.Output + refusal.ErrorOutput);
                refusal.ExitCode.ShouldNotBe(0);
                (refusal.Output + refusal.ErrorOutput).ShouldContain("selected Conversations adapter schema owner is absent");
                Directory.EnumerateFiles(output).ShouldBeEmpty();
            }
            var domainArtifact = receipt.RootElement.GetProperty("loadedArtifacts").EnumerateArray().Single(artifact =>
                AssemblyName.GetAssemblyName(artifact.GetProperty("path").GetString()!).Name == "Hexalith.Conversations");
            string domainPath = domainArtifact.GetProperty("path").GetString()!;
            domainArtifact.GetProperty("sha256").GetString().ShouldBe(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(domainPath))));
            string alteredOutput = Path.Combine(workspace, "altered-domain"); Directory.CreateDirectory(alteredOutput);
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(domainPath)!)) File.Copy(file, Path.Combine(alteredOutput, Path.GetFileName(file)));
            File.WriteAllBytes(Path.Combine(alteredOutput, "Hexalith.Conversations.dll"), "invalid-selected-domain"u8.ToArray());
            JsonObject alteredRequest = JsonNode.Parse(File.ReadAllBytes(Path.Combine(reader, "request.json")))!.AsObject();
            string rejected = Path.Combine(workspace, "altered-domain-rejected"); Directory.CreateDirectory(rejected); alteredRequest["outputDirectory"] = rejected;
            string alteredInput = Path.Combine(evidence, "altered-domain-request.json"); File.WriteAllText(alteredInput, alteredRequest.ToJsonString());
            var alteredRefusal = await CompositionProcess.RunAsync(CompositionProcess.CreateStartInfo("dotnet",
                [Path.Combine(alteredOutput, "Hexalith.Platform.CatalogBuilder.dll"), alteredInput], alteredOutput, null), TimeSpan.FromMinutes(1), TestContext.Current.CancellationToken);
            File.WriteAllText(Path.Combine(evidence, "altered-domain.log"), alteredRefusal.Output + alteredRefusal.ErrorOutput);
            alteredRefusal.ExitCode.ShouldNotBe(0); Directory.EnumerateFiles(rejected).ShouldBeEmpty();
'''+s[pos:];f.write_bytes(s.replace('\n','\r\n').encode())
