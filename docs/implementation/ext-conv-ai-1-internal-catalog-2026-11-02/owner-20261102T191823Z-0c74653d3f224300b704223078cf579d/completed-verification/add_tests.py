from pathlib import Path
r=Path('/home/administrator/projects/hexalith/conversations/tests/Hexalith.Conversations.Server.Tests/Routing');r.mkdir(exist_ok=True)
def write(n,s): (r/n).write_bytes(s.replace('\n','\r\n').encode())
write('ConversationsCatalogTest.cs','''using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

public sealed class ConversationsCatalogTest
{
    [Fact]
    public void CompleteCompiledSchemasUseActualWrappersAndOnlyInternalWorkloadOperations()
    {
        ConversationsCatalogAdapters.Create().Count.ShouldBe(13);
        var owners = new[] { typeof(ConversationsCatalogDeclarations).Assembly };
        var routes = CompiledRouteCatalogDeclarations.Read(owners);
        var operations = CompiledRouteCatalogDeclarations.ReadOperations(owners);
        routes.Count.ShouldBe(22);
        routes.Count(route => route.Kind == "command").ShouldBe(13);
        routes.Count(route => route.Kind == "query").ShouldBe(7);
        routes.Count(route => route.Kind == "projection").ShouldBe(2);
        routes.All(route => route.CredentialKind == "workload" && route.PlatformAdmission is null).ShouldBeTrue();
        routes.Where(route => route.Kind == "command").All(route => route.Domain == "conversation"
            && route.Operation == EventStoreOperationIdentifiers.DomainServiceProcess).ShouldBeTrue();
        operations.All(operation => operation.Ingress == "internal"
            && operation.Digest == RouteCatalogCodec.ComputeOperationContractDigest(operation)).ShouldBeTrue();
        foreach (var route in routes.Where(route => route.Execution!.HandlerType.StartsWith("Hexalith.Conversations.Commands.", StringComparison.Ordinal)))
        {
            var properties = operations.Single(operation => operation.RouteId == route.RouteId).RequestSchema.GetProperty("properties");
            properties.TryGetProperty("publicCommand", out _).ShouldBeTrue();
            properties.TryGetProperty("eventId", out _).ShouldBeTrue();
        }
        operations.Single(operation => operation.RouteId == "conversation-create-conversation").RequestSchema
            .GetProperty("properties").TryGetProperty("createdAt", out _).ShouldBeTrue();
        routes.Where(route => route.Kind == "query" && route.Execution!.RequiresDelegatedContext).Count().ShouldBe(2);
        routes.Where(route => route.Kind == "query" && route.Domain == "conversations").Count().ShouldBe(4);
        routes.Where(route => route.Kind == "query" && route.Domain == "conversation").Count().ShouldBe(3);
        typeof(ConversationsCatalogDeclarations).Assembly.GetReferencedAssemblies().Any(reference => reference.Name == "Hexalith.Conversations").ShouldBeFalse();
    }

    [Fact]
    public void MissingOrForeignSelectedOwnerRefusesSchemaExport()
    {
        Should.Throw<ArgumentNullException>(() => ConversationsCatalogDeclarations.CreateOperations(null!));
        Should.Throw<InvalidOperationException>(() => ConversationsCatalogDeclarations.CreateOperations(typeof(string).Assembly));
        using var missing = new AssemblyLoadContext("conversations-catalog-missing-owner", isCollectible: true);
        Assembly isolatedContracts = missing.LoadFromAssemblyPath(typeof(ConversationsCatalogDeclarations).Assembly.Location);
        var source = isolatedContracts.GetType(typeof(ConversationsCatalogDeclarations).FullName!, throwOnError: true)!;
        Should.Throw<TargetInvocationException>(() => source.GetProperty("Operations")!.GetValue(null))
            .InnerException.ShouldBeOfType<InvalidOperationException>();
        using var foreign = new AssemblyLoadContext("conversations-catalog-foreign-owner", isCollectible: true);
        Assembly foreignDomain = foreign.LoadFromAssemblyPath(typeof(ConversationAggregate).Assembly.Location);
        Should.Throw<InvalidOperationException>(() => ConversationsCatalogDeclarations.CreateOperations(foreignDomain));
    }

    [Fact]
    public async Task ActualSdkDiscoveryAndFiniteReceiversMatchEveryOwnerDeclarationAndRefuseDrift()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(ConversationAggregate).Assembly, typeof(ServerAssemblyMarker).Assembly);
        builder.Services.AddConversationTenantAccess();
        builder.Services.AddConversationQueries(_ => { });
        builder.Services.Configure<DomainProjectionIdentityOptions>(options => { options.AppId = "conversation"; options.ServiceVersion = "v1"; });
        builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(
            ConversationsCatalogDeclarations.Projections.Where(route => route.ProjectionType == "conversation").ToArray()));
        await using var app = builder.Build();
        app.MapEventStoreDomainService();
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var routes = ConversationsCatalogDeclarations.Routes;
        var payloads = services.GetRequiredKeyedService<IDomainProcessor>("conversation")
            .ShouldBeAssignableTo<IDomainCommandPayloadInventory>().CommandPayloadTypes;
        payloads.Select(type => type.FullName).Order().ShouldBe(routes.Where(route => route.Kind == "command").Select(route => route.Execution!.HandlerType).Order());
        services.GetServices<IDomainQueryHandler>().Select(handler => (handler.Domain, handler.QueryType, handler.GetType().FullName)).Order()
            .ShouldBe(routes.Where(route => route.Kind == "query").Select(route => (route.Domain, route.MessageType!, route.Execution!.HandlerType)).Order());
        services.GetServices<IAsyncDomainProjectionHandler>().Select(handler => (handler.Domain, handler.ProjectionType)).ShouldBe([("conversation", "conversation-read-model")]);
        Endpoint[] endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ToArray();
        var targets = DevelopmentDomainRouteCatalog.Targets(services, endpoints, routes);
        targets.Count.ShouldBe(22);
        foreach (var target in targets)
        {
            target.AppId.ShouldBe("conversation");
            target.Method.ShouldBe(ConversationsCatalogDeclarations.ReceiverMethods[target.RouteId]);
        }
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services, endpoints, routes.Skip(1).ToArray()));
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services,
            endpoints.OfType<RouteEndpoint>().Where(endpoint => endpoint.RoutePattern.RawText != "/project").ToArray(), routes));
        Should.Throw<RouteCatalogUnavailableException>(() => DevelopmentDomainRouteCatalog.Targets(services, endpoints,
            routes.Select(route => route.ProjectionType == "conversation-read-model" ? route with { Execution = route.Execution! with { HandlerType = "foreign" } } : route).ToArray()));
    }
}
''')
write('ConversationsCatalogIntentTest.cs','''using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.EventStore.DomainService;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

public sealed class ConversationsCatalogIntentTest
{
    private static readonly DateTimeOffset Occurrence = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ConversationId Conversation = new("conversation-one");
    private static readonly PartyId Actor = new("party-one");
    private static readonly ConversationCommandMetadata Metadata = new(SchemaVersion.Current, new("tenant-one"), Actor, "correlation-one", "causation-one", "retry-one");
    private static readonly GovernanceAuditEvidenceReference Audit = new(new("audit-one"), "policy-one", Occurrence.AddMinutes(-1));

    [Fact]
    public void EverySerializedReceiverExcludesGeneratedMetadataButBindsSemanticChangesAndRealAliases()
    {
        var cases = Cases();
        cases.Count.ShouldBe(13);
        var adapters = ConversationsCatalogAdapters.Create();
        foreach (var (payload, fields) in cases)
        {
            var adapter = adapters.Single(candidate => candidate.Declaration.Route.Execution!.HandlerType == payload.GetType().FullName);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), EventStorePayloadSerialization.Options);
            var original = Intent(adapter, bytes);
            foreach ((string path, JsonNode? value) in fields)
            {
                var changed = JsonNode.Parse(bytes)!;
                Set(changed, path, value?.DeepClone());
                Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(changed)).SemanticPayload.ShouldNotBe(original.SemanticPayload,
                    $"{payload.GetType().Name} must bind {path}");
            }
            var retry = JsonNode.Parse(bytes)!;
            string prefix = retry["publicCommand"] is null ? "" : "publicCommand.";
            Set(retry, prefix + "metadata.correlationId", JsonValue.Create("correlation-retry"));
            Set(retry, prefix + "metadata.causationId", JsonValue.Create("causation-retry"));
            if (retry["eventId"] is not null) retry["eventId"] = "generated-event-retry";
            if (payload is CreateConversation) { retry["conversationId"] = "generated-allocation-retry"; retry["createdAt"] = Occurrence.AddMinutes(1); }
            if (payload is AddParticipant) retry["addedAt"] = Occurrence.AddMinutes(1);
            if (payload is ReassignConversationProject) retry["changedAt"] = Occurrence.AddMinutes(1);
            Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(retry)).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, payload.GetType().FullName).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, payload.GetType().AssemblyQualifiedName).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, aggregate: "conversation-other").CanonicalTarget.ShouldNotBe(original.CanonicalTarget);
            Should.Throw<InvalidOperationException>(() => adapter.CreateIntent(new(adapter.CommandType, "tenant-one", "conversations", "conversation-one", bytes, null)));
            Should.Throw<InvalidOperationException>(() => Intent(adapter, bytes, "ForeignReceiver"));
        }
    }

    [Fact]
    public void CanonicalPublicNormalizationAndOriginalAgentPostingFingerprintArePreserved()
    {
        var create = new CreateConversation(new(Metadata, Label: "Cafe\\u0301"), new("allocated-one"), Occurrence, "event-one");
        var adapter = ConversationsCatalogAdapters.Create().Single(candidate => candidate.CommandType == nameof(CreateConversation));
        var first = JsonSerializer.SerializeToUtf8Bytes(create, EventStorePayloadSerialization.Options);
        var second = JsonNode.Parse(first)!; second["publicCommand"]!["label"] = "Café";
        Intent(adapter, first).SemanticPayload.ShouldBe(Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(second)).SemanticPayload);
        var post = Cases().Select(item => item.Payload).OfType<AppendAgentMessage>().Single();
        var postAdapter = ConversationsCatalogAdapters.Create().Single(candidate => candidate.CommandType == nameof(AppendAgentMessage));
        var intent = Intent(postAdapter, JsonSerializer.SerializeToUtf8Bytes(post, EventStorePayloadSerialization.Options));
        using var descriptor = JsonDocument.Parse(intent.SemanticPayload);
        descriptor.RootElement.GetProperty("intent-sha256").GetString().ShouldBe(CanonicalIntentPayload.Sha256(new
        {
            Metadata = new { post.PublicCommand.Metadata.SchemaVersion, post.PublicCommand.Metadata.TenantId, post.PublicCommand.Metadata.ActorPartyId, post.PublicCommand.Metadata.IdempotencyKey },
            post.PublicCommand.ConversationId,
            Fingerprint = ConversationAgentPostFingerprint.Create(post.PublicCommand, post.PostedAt), post.PublicCommand.OperationTimestamp,
        }));
    }

    private static IdempotencyCanonicalIntent Intent(IDeclaredIdempotencyIntentAdapter adapter, byte[] bytes, string? wire = null, string aggregate = "conversation-one")
        => adapter.CreateIntent(new(wire ?? adapter.CommandType, "tenant-one", "conversation", aggregate, bytes, null));

    private static void Set(JsonNode node, string path, JsonNode? value)
    {
        string[] parts = path.Split('.');
        foreach (string part in parts[..^1]) node = node[part]!;
        node[parts[^1]] = value;
    }

    private static IReadOnlyList<(object Payload, IReadOnlyList<(string Path, JsonNode? Value)> Fields)> Cases()
    {
        var signal = new ConversationDeletionSignal("signal-one", Metadata.TenantId, Conversation, "stream-one", 4, "approval-one");
        var acknowledgement = new ConversationDeletionAcknowledgement("signal-one", 4, 7, "target-one", "evidence-one");
        AddParticipantCommand add = new(Metadata, Conversation, Actor, ParticipantType.AiAgent, ParticipantRole.Member,
            new("provider-one", "model", SchemaVersion.Current), Occurrence);
        AppendMessageCommand post = new(Metadata, Conversation, new("message-one"), Actor, "original-text",
            new("provider-one", "model", SchemaVersion.Current), new(SchemaVersion.Current, ClientName: "client-one"),
            new("trace-one", true, false), Occurrence);
        List<(object, IReadOnlyList<(string, JsonNode?)>)> cases = [
            (new CreateConversation(new(Metadata, Label: "label-one", CallerMetadata: new(SchemaVersion.Current, ClientName: "client-one")), new("allocated-one"), Occurrence, "event-one"),
                [("publicCommand.label", JsonValue.Create("label-two")), ("publicCommand.callerMetadata.clientName", JsonValue.Create("client-two"))]),
            (new AddParticipant(add, Occurrence, "event-one"), [("publicCommand.participantPartyId", JsonValue.Create("party-two")), ("publicCommand.providerCorrelation.providerName", JsonValue.Create("provider-two"))]),
            (new ReassignConversationProject(new(Metadata, Conversation, new(ConversationProjectAssignmentOperation.Assign, new("project-one"))), Occurrence, "event-one"), [("publicCommand.target.projectId", JsonValue.Create("project-two"))]),
            (new SetConversationRetentionPolicy(new(Metadata, Conversation, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1))), ("auditEvidence.handle.value", JsonValue.Create("audit-two"))]),
            (new MarkConversationContentSensitive(new(Metadata, Conversation, new(GovernedTargetKind.Message, new("message-one")), SensitivityCategory.Sensitive, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.target.messageId", JsonValue.Create("message-two")), ("auditEvidence.policyReference", JsonValue.Create("policy-two"))]),
            (new RedactMessageContent(new(Metadata, Conversation, new(GovernedTargetKind.Message, new("message-one")), RedactionCategory.DisplayMask, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.target.messageId", JsonValue.Create("message-two")), ("auditEvidence.capturedAt", JsonValue.Create(Occurrence.AddMinutes(-2)))]),
            (new AddAgentParticipant(add, Occurrence, "event-one"), [("addedAt", JsonValue.Create(Occurrence.AddSeconds(1))), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new RemoveAgentParticipant(new(Metadata, Conversation, Actor, Occurrence), "event-one"), [("publicCommand.participantPartyId", JsonValue.Create("party-two")), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new AppendAgentMessage(post, Occurrence, "event-one"), [("postedAt", JsonValue.Create(Occurrence.AddSeconds(1))), ("publicCommand.agentProvenance.agentCallTraceReference", JsonValue.Create("trace-two")), ("publicCommand.providerCorrelation.providerName", JsonValue.Create("provider-two")), ("publicCommand.callerMetadata.clientName", JsonValue.Create("client-two")), ("publicCommand.text", JsonValue.Create("changed-text"))]),
            (new ApproveConversationDeletion(new(Metadata, Conversation, "approval-one", 3, Occurrence, Audit), "event-one"), [("publicCommand.approvalReference", JsonValue.Create("approval-two")), ("publicCommand.sourceRevision", JsonValue.Create(4L)), ("publicCommand.auditEvidence.handle.value", JsonValue.Create("audit-two")), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new RecordConversationDeletionDelivery(new(Metadata, Conversation, signal, ConversationDeletionDeliveryAction.Acknowledge, "attempt-one", "target-one", 2, acknowledgement), "event-one"), [("publicCommand.deliveryAttemptId", JsonValue.Create("attempt-two")), ("publicCommand.targetVersion", JsonValue.Create("target-two")), ("publicCommand.expectedDeliveryRevision", JsonValue.Create(3L)), ("publicCommand.signal.sourceRevision", JsonValue.Create(5L)), ("publicCommand.acknowledgement.evidence", JsonValue.Create("evidence-two"))]),
            (new EditConversationMessageCommand(Metadata, Conversation, new("message-one"), "text-one", Occurrence), [("text", JsonValue.Create("text-two")), ("messageId", JsonValue.Create("message-two")), ("operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new DeleteConversationMessageCommand(Metadata, Conversation, new("message-one"), Occurrence), [("messageId", JsonValue.Create("message-two")), ("operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
        ];
        return cases.Select(item =>
        {
            string prefix = item.Item1 is EditConversationMessageCommand or DeleteConversationMessageCommand ? "" : "publicCommand.";
            return (item.Item1, (IReadOnlyList<(string, JsonNode?)>)[.. item.Item2,
                (prefix + "metadata.actorPartyId", JsonValue.Create("party-other")), (prefix + "metadata.tenantId", JsonValue.Create("tenant-other"))]);
        }).ToArray();
    }
}
''')
