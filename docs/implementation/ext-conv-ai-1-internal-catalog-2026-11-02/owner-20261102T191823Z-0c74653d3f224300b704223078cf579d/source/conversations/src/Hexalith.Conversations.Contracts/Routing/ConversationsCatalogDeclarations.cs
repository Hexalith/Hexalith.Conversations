using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;

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
        string ownerPath = Path.Combine(Path.GetDirectoryName(adapter.Location)
            ?? throw new InvalidOperationException("The selected Conversations adapter has no physical dependency closure."), owner.Name + ".dll");
        if (!File.Exists(ownerPath) || AssemblyName.GetAssemblyName(ownerPath).FullName != owner.FullName)
            throw new InvalidOperationException("The selected Conversations Domain dependency is absent or foreign.");
        Assembly domain = context.LoadFromAssemblyPath(ownerPath);
        if (domain.FullName != owner.FullName || domain.Location != ownerPath)
            throw new InvalidOperationException("The loaded Conversations Domain differs from the selected adapter dependency closure.");
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
        var operation = RouteOperationSchema.Create(route, request, result, "internal", "UI-only", EventStorePayloadSerialization.Options);
        if (request == typeof(GetConversationResolutionMetadataQuery) || request == typeof(GetConversationAdvisoryMetadataQuery))
        {
            // These receivers explicitly reject any payload member before authorization or source reads.
            JsonObject schema = JsonNode.Parse(operation.RequestSchema.GetRawText())!.AsObject();
            schema["additionalProperties"] = false;
            operation = operation with { RequestSchema = JsonSerializer.SerializeToElement(schema) };
            operation = operation with { Digest = RouteCatalogCodec.ComputeOperationContractDigest(operation) };
        }
        return operation;
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
