from pathlib import Path
import json
root=Path('/home/administrator/projects/hexalith/conversations')
def write(p,s):
 p=root/p; p.parent.mkdir(parents=True,exist_ok=True); p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
def edit(p,a,b):
 p=root/p; s=p.read_text(); assert a in s,p; write(p,s.replace(a,b))
edit('src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj','    <!-- Consume only the serialization project selected by the Platform mapping. -->','    <ProjectReference Include="$(HexalithEventStoreRoot)/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj" />\n    <!-- Consume only the serialization project selected by the Platform mapping. -->')
edit('src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj','    <PackageReference Include="Hexalith.Commons.Serialization" />','    <PackageReference Include="Hexalith.Commons.Serialization" />\n    <PackageReference Include="Hexalith.EventStore.Contracts" />')
# Move the actual SDK payload types to the inner Contracts boundary; no duplicate wire DTOs.
p=root/'src/Hexalith.Conversations.Server/Queries/ConversationDomainQueryHandler.cs'
s=p.read_text()
start=s.index('    /// <summary>The query-specific fields carried in the list query envelope payload.')
end=s.index('\n}',start)
s=s[:start]+s[end:]
start=s.index('    /// <summary>The query-specific fields carried in the detail query envelope payload.')
end=s.index('\n}',start)
s=s[:start]+s[end:]
write(str(p.relative_to(root)),s)
write('src/Hexalith.Conversations.Contracts/Queries/ListConversationsPayload.cs','''namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>The actual list-query fields carried in the SDK query envelope payload.</summary>
/// <param name="Filter">The exact-match tenant-scoped filter, or the default empty filter.</param>
/// <param name="Page">The bounded page request, or the default first page.</param>
public sealed record ListConversationsPayload(ConversationListFilterV1? Filter, ConversationPageRequest? Page);
''')
write('src/Hexalith.Conversations.Contracts/Queries/GetConversationPayload.cs','''namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>The actual detail-query fields carried in the SDK query envelope payload.</summary>
/// <param name="ConversationId">The exact conversation identity, or the envelope identity when omitted.</param>
public sealed record GetConversationPayload(string? ConversationId);
''')
write('src/Hexalith.Conversations.Contracts/Routing/ConversationsCatalogDeclarations.cs','''using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Serialization;

[assembly: RouteCatalogDeclarationSource(typeof(Hexalith.Conversations.Contracts.Routing.ConversationsCatalogDeclarations))]

namespace Hexalith.Conversations.Contracts.Routing;

/// <summary>Complete internal owner metadata for actual Conversations SDK receivers; grants no business authority.</summary>
public static class ConversationsCatalogDeclarations
{
    /// <summary>The exact thirteen aggregate payloads and their local semantic policies.</summary>
    public static IReadOnlyList<CommandIntentDeclaration> Commands { get; } = [
        Command("CreateConversation"), Command("AddParticipant"), Command("ReassignConversationProject"),
        Command("SetConversationRetentionPolicy"), Command("MarkConversationContentSensitive"), Command("RedactMessageContent"),
        Command("AddAgentParticipant"), Command("RemoveAgentParticipant"), Command("AppendAgentMessage"),
        Command("ApproveConversationDeletion"), Command("RecordConversationDeletionDelivery"),
        Command(nameof(EditConversationMessageCommand), typeof(EditConversationMessageCommand)),
        Command(nameof(DeleteConversationMessageCommand), typeof(DeleteConversationMessageCommand))];

    /// <summary>The seven actual SDK query discriminators, retaining their original delegated context requirements.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Queries { get; } = [
        Query("conversation-list", "conversations", "ListConversationsDomainQueryHandler"),
        Query("conversation-detail", "conversations", "GetConversationDomainQueryHandler"),
        Query(GetConversationResolutionMetadataQuery.QueryType, "conversations", "GetConversationResolutionMetadataDomainQueryHandler", true),
        Query(GetConversationAdvisoryMetadataQuery.QueryType, "conversations", "GetConversationAdvisoryMetadataDomainQueryHandler", true),
        Query("conversation-agent-read", "conversation", "ConversationAgentReadQueryHandler"),
        Query("conversation-active-count", "conversation", "ConversationActiveCountQueryHandler"),
        Query("conversation-deletion-source", "conversation", "ConversationDeletionSourceQueryHandler")];

    /// <summary>The legacy and named projection receivers; their outputs are not additional callable routes.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Projections { get; } = [
        Projection("conversation", "ConversationProjectionHandler"),
        Projection("conversation-read-model", "ConversationAsyncProjectionHandler")];

    /// <summary>The complete compiled route inventory.</summary>
    public static IReadOnlyList<RouteContractDeclaration> Routes { get; } = [.. Commands.Select(command => command.Route), .. Queries, .. Projections];

    /// <summary>The exact finite receiver method for each declared route.</summary>
    public static IReadOnlyDictionary<string, string> ReceiverMethods => Routes.ToDictionary(route => route.RouteId,
        route => route.Kind switch { "command" => "process", "query" => "query", _ => route.ProjectionType == "conversation" ? "project" : "project/v2" }, StringComparer.Ordinal);

    /// <summary>Actual wrapper schemas from the selected adapter dependency closure; missing owners refuse.</summary>
    public static IReadOnlyList<RouteOperationContract> Operations => CreateOperations(SelectedDomainOwner());

    /// <summary>Exports only actual selected wrapper types, without referencing Domain from Contracts.</summary>
    /// <param name="domainOwner">The Domain assembly selected through the compiled adapter dependency.</param>
    /// <returns>The complete internal operation schemas and digests.</returns>
    public static IReadOnlyList<RouteOperationContract> CreateOperations(Assembly domainOwner)
    {
        ArgumentNullException.ThrowIfNull(domainOwner);
        Assembly contracts = typeof(ConversationsCatalogDeclarations).Assembly;
        if (domainOwner.GetName().Name != "Hexalith.Conversations"
            || AssemblyLoadContext.GetLoadContext(domainOwner) != AssemblyLoadContext.GetLoadContext(contracts)
            || !domainOwner.GetReferencedAssemblies().Any(reference => reference.FullName == contracts.FullName))
            throw new InvalidOperationException("The selected Conversations Domain schema owner is absent or foreign.");
        return Routes.Select(route => Operation(route, domainOwner)).ToArray();
    }

    private static Assembly SelectedDomainOwner()
    {
        Assembly contracts = typeof(ConversationsCatalogDeclarations).Assembly;
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(contracts)
            ?? throw new InvalidOperationException("The selected Conversations schema load context is absent.");
        Assembly adapter = context.Assemblies.SingleOrDefault(assembly => assembly.GetName().Name == "Hexalith.Conversations.EventStoreExtensions")
            ?? throw new InvalidOperationException("The selected Conversations adapter schema owner is absent.");
        if (!adapter.GetReferencedAssemblies().Any(reference => reference.FullName == contracts.FullName))
            throw new InvalidOperationException("The selected Conversations adapter references foreign Contracts.");
        AssemblyName owner = adapter.GetReferencedAssemblies().SingleOrDefault(reference => reference.Name == "Hexalith.Conversations")
            ?? throw new InvalidOperationException("The selected Conversations adapter has no Domain schema dependency.");
        Assembly domain = context.LoadFromAssemblyName(owner);
        if (domain.FullName != owner.FullName) throw new InvalidOperationException("The selected Conversations Domain identity differs from its adapter dependency.");
        return domain;
    }

    private static RouteOperationContract Operation(RouteContractDeclaration route, Assembly domain)
    {
        Type request;
        Type result;
        if (route.Kind == "command")
        {
            request = route.Execution!.HandlerType.StartsWith("Hexalith.Conversations.Commands.", StringComparison.Ordinal)
                ? domain.GetType(route.Execution.HandlerType, throwOnError: true)!
                : typeof(ConversationsCatalogDeclarations).Assembly.GetType(route.Execution.HandlerType, throwOnError: true)!;
            result = typeof(SubmitCommandResponse);
        }
        else if (route.Kind == "projection")
        {
            (request, result) = route.ProjectionType == "conversation"
                ? (typeof(ProjectionRequest), typeof(ProjectionResponse))
                : (typeof(ProjectionDispatchRequest), typeof(ProjectionDispatchResponse));
        }
        else
        {
            (request, result) = route.MessageType switch
            {
                "conversation-list" => (typeof(ListConversationsPayload), typeof(ConversationListResult)),
                "conversation-detail" => (typeof(GetConversationPayload), typeof(ConversationDetailResult)),
                GetConversationResolutionMetadataQuery.QueryType => (typeof(GetConversationResolutionMetadataQuery), typeof(ConversationResolutionMetadataResultV1)),
                GetConversationAdvisoryMetadataQuery.QueryType => (typeof(GetConversationAdvisoryMetadataQuery), typeof(ConversationAdvisoryMetadataResult)),
                "conversation-agent-read" => (typeof(ConversationAgentReadQuery), typeof(ConversationAgentReadResult)),
                "conversation-active-count" => (typeof(ConversationActiveCountQuery), typeof(ConversationActiveCountResult)),
                "conversation-deletion-source" => (typeof(ConversationDeletionSourceQuery), typeof(ConversationDeletionSourceResult)),
                _ => throw new InvalidOperationException("Unknown Conversations query schema."),
            };
        }
        return RouteOperationSchema.Create(route, request, result, "internal", "UI-only", EventStorePayloadSerialization.Options);
    }

    private static CommandIntentDeclaration Command(string wire, Type? contract = null)
    {
        string id = Kebab(wire);
        return new(new("conversation-" + id, "command", "conversation", wire, null, "v1", 1, "workload",
            EventStoreOperationIdentifiers.DomainServiceProcess, null, [],
            new(contract?.FullName ?? "Hexalith.Conversations.Commands." + wire, "clr-short-name-v1", [wire], "domain-service", null, [])),
            "hexalith:conversation:" + id + ":intent-v1", "conversation:" + id, "conversation:" + id + ":local-policy-v1",
            new("hexalith:conversation:" + id + ":descriptor-v1", 1, [new("target", "string"), new("intent-sha256", "string")]),
            IdempotencyReplayRetentionTier.Mutation, false);
    }

    private static RouteContractDeclaration Query(string wire, string domain, string handler, bool delegated = false) =>
        new("conversation-query-" + (wire == GetConversationResolutionMetadataQuery.QueryType ? "resolution-metadata"
            : wire == GetConversationAdvisoryMetadataQuery.QueryType ? "advisory-metadata" : wire), "query", domain, wire, null, "v1", null, "workload",
            EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
            new("Hexalith.Conversations.Server." + (domain == "conversation" ? "Agents." : "Queries.") + handler,
                "exact", [wire], "domain-service", "computed", [], RequiresDelegatedContext: delegated));

    private static RouteContractDeclaration Projection(string wire, string handler) => new(wire + "-projection", "projection", "conversation", null,
        wire, "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceProject, null, [],
        new("Hexalith.Conversations.Server.Projections." + handler, "exact", [wire], "domain-service", null, [wire]));

    private static string Kebab(string value) => string.Concat(value.Select((character, index) =>
        char.IsUpper(character) && index > 0 ? "-" + char.ToLowerInvariant(character) : char.ToLowerInvariant(character).ToString()));
}
''')
write('src/Hexalith.Conversations.EventStoreExtensions/Hexalith.Conversations.EventStoreExtensions.csproj','''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Conversations-owned internal compiled EventStore route and semantic intent adapters.</Description>
    <PackageId>Hexalith.Conversations.EventStoreExtensions</PackageId>
    <PackageTags>conversations;eventstore;routing;idempotency;catalog</PackageTags>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj" />
    <ProjectReference Include="../Hexalith.Conversations/Hexalith.Conversations.csproj" />
  </ItemGroup>
  <ItemGroup Condition="'$(UseHexalithProjectReferences)' == 'true'">
    <ProjectReference Include="$(HexalithEventStoreRoot)/src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj" />
  </ItemGroup>
  <ItemGroup Condition="'$(UseHexalithProjectReferences)' == 'false'">
    <PackageReference Include="Hexalith.EventStore.DomainService" />
  </ItemGroup>
</Project>
''')
# Extract the existing PostFingerprint unchanged as a pure shared Domain helper.
p=root/'src/Hexalith.Conversations/Aggregates/ConversationAggregate.Agents.cs'
s=p.read_text(); start=s.index('    private static string PostFingerprint('); end=s.index('    private static DomainResult AgentSuccess',start)
helper=s[start:end].replace('    private static string PostFingerprint(','''    /// <summary>Computes the original posting fingerprint shared by aggregate retry and internal catalog intent.</summary>
    /// <param name="request">The original portable posting request.</param>
    /// <param name="occurrence">The exact persisted occurrence.</param>
    /// <returns>The unchanged canonical original-post fingerprint.</returns>
    public static string Create(''')
write('src/Hexalith.Conversations/Idempotency/ConversationAgentPostFingerprint.cs','''using System.Text.Json;

using Hexalith.Conversations.Contracts.Commands;

namespace Hexalith.Conversations.Idempotency;

/// <summary>The aggregate's original posting fingerprint, shared without changing its canonical protocol.</summary>
public static class ConversationAgentPostFingerprint
{
'''+helper+'}\n')
s=s[:start]+s[end:]; s=s.replace('PostFingerprint(request, command.PostedAt)', 'ConversationAgentPostFingerprint.Create(request, command.PostedAt)'); write(str(p.relative_to(root)),s)
write('src/Hexalith.Conversations.EventStoreExtensions/ConversationsCatalogAdapters.cs','''using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.DomainService;

[assembly: RouteCatalogAdapterSource(typeof(Hexalith.Conversations.EventStoreExtensions.ConversationsCatalogAdapters))]

namespace Hexalith.Conversations.EventStoreExtensions;

/// <summary>Exact internal command adapters; owner admission and current business checks remain authoritative.</summary>
public static class ConversationsCatalogAdapters
{
    /// <summary>Creates all thirteen typed semantic adapters without enabling runtime admission.</summary>
    /// <returns>The exact aggregate receiver adapter inventory.</returns>
    public static IReadOnlyList<IDeclaredIdempotencyIntentAdapter> Create() => [
        Adapter<CreateConversation>(command => Ordinary(command.PublicCommand,
            // Creation allocation, boundary timestamps and event identities are generated fields.
            new ConversationId("catalog-create-allocation"))),
        Adapter<AddParticipant>(command => Ordinary(command.PublicCommand, command.PublicCommand.ConversationId)),
        Adapter<ReassignConversationProject>(command => Ordinary(command.PublicCommand, command.PublicCommand.ConversationId)),
        Adapter<SetConversationRetentionPolicy>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AuditEvidence }),
        Adapter<MarkConversationContentSensitive>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AuditEvidence }),
        Adapter<RedactMessageContent>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AuditEvidence }),
        Adapter<AddAgentParticipant>(command => new { Fingerprint = Ordinary(command.PublicCommand, command.PublicCommand.ConversationId), command.AddedAt, command.PublicCommand.OperationTimestamp }),
        Adapter<RemoveAgentParticipant>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.ParticipantPartyId, command.PublicCommand.OperationTimestamp }),
        Adapter<AppendAgentMessage>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            Fingerprint = ConversationAgentPostFingerprint.Create(command.PublicCommand, command.PostedAt), command.PublicCommand.OperationTimestamp }),
        Adapter<ApproveConversationDeletion>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.ApprovalReference, command.PublicCommand.SourceRevision, command.PublicCommand.OperationTimestamp, command.PublicCommand.AuditEvidence }),
        Adapter<RecordConversationDeletionDelivery>(command => new { Metadata = SemanticMetadata(command.PublicCommand.Metadata), command.PublicCommand.ConversationId,
            command.PublicCommand.Signal, command.PublicCommand.Action, command.PublicCommand.DeliveryAttemptId, command.PublicCommand.TargetVersion,
            command.PublicCommand.ExpectedDeliveryRevision, command.PublicCommand.Acknowledgement }),
        Adapter<EditConversationMessageCommand>(command => new { Metadata = SemanticMetadata(command.Metadata), command.ConversationId,
            command.MessageId, command.Text, command.OperationTimestamp }),
        Adapter<DeleteConversationMessageCommand>(command => new { Metadata = SemanticMetadata(command.Metadata), command.ConversationId,
            command.MessageId, command.OperationTimestamp })];

    private static DeclaredIdempotencyIntentAdapter<T> Adapter<T>(Func<T, object> semantic) => new(
        ConversationsCatalogDeclarations.Commands.Single(command => command.Route.Execution!.HandlerType == typeof(T).FullName),
        payload => new Dictionary<string, object?> { ["intent-sha256"] = CanonicalIntentPayload.Sha256(semantic(payload)) });

    private static object SemanticMetadata(ConversationCommandMetadata metadata) => new { metadata.SchemaVersion, metadata.TenantId,
        metadata.ActorPartyId, metadata.IdempotencyKey };

    private static object Ordinary(object command, ConversationId allocation)
    {
        ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(command, allocation);
        // The existing canonical payload owns text normalization and scoped target semantics. Portable
        // provenance remains meaningful even when it does not participate in the earlier public API retry key.
        return command switch
        {
            CreateConversationCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.ProviderCorrelation, request.CallerMetadata },
            AddParticipantCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.ProviderCorrelation, request.CallerMetadata },
            ReassignConversationProjectCommand request => new { fingerprint.Scope, fingerprint.PayloadFingerprint, request.ProviderCorrelation, request.CallerMetadata },
            _ => (object)new { fingerprint.Scope, fingerprint.PayloadFingerprint },
        };
    }
}
''')
edit('src/Hexalith.Conversations.Server/Hexalith.Conversations.Server.csproj','  <ItemGroup>','  <ItemGroup>\n    <ProjectReference Include="../Hexalith.Conversations.EventStoreExtensions/Hexalith.Conversations.EventStoreExtensions.csproj" />',)
edit('src/Hexalith.Conversations.Server/Program.cs','using Hexalith.Conversations;','using Hexalith.Conversations;\nusing Hexalith.Conversations.Contracts.Routing;\nusing Hexalith.Conversations.EventStoreExtensions;')
edit('src/Hexalith.Conversations.Server/Program.cs','WebApplication app = builder.Build();','''builder.Services.AddSingleton(new LegacyProjectionCatalogRegistration(
    ConversationsCatalogDeclarations.Projections.Where(route => route.ProjectionType == "conversation").ToArray()));
builder.Services.AddEventStoreDevelopmentDomainCatalog(builder.Configuration,
    typeof(ConversationsCatalogDeclarations).Assembly, typeof(ConversationsCatalogAdapters).Assembly);

WebApplication app = builder.Build();''')
edit('Hexalith.Conversations.slnx','    <Project Path="src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj" />','    <Project Path="src/Hexalith.Conversations.Contracts/Hexalith.Conversations.Contracts.csproj" />\n    <Project Path="src/Hexalith.Conversations.EventStoreExtensions/Hexalith.Conversations.EventStoreExtensions.csproj" />')
p=root/'tools/release-packages.json'; d=json.loads(p.read_text()); d['packages'].append({'id':'Hexalith.Conversations.EventStoreExtensions','project':'src/Hexalith.Conversations.EventStoreExtensions/Hexalith.Conversations.EventStoreExtensions.csproj'}); p.write_text(json.dumps(d,indent=2)+'\n')
edit('scripts/release_contract.py','EXACT_PACKAGE_IDS = tuple','EXACT_PACKAGES += (("Hexalith.Conversations.EventStoreExtensions", "src/Hexalith.Conversations.EventStoreExtensions/Hexalith.Conversations.EventStoreExtensions.csproj"),)\nEXACT_PACKAGE_IDS = tuple')
for path in ['scripts/release_contract.py','scripts/validate-nuget-packages.py','scripts/release_state.py']:
 p=root/path; p.write_text(p.read_text().replace('four-package','five-package').replace('four package','five package').replace('four symbol','five symbol'))
