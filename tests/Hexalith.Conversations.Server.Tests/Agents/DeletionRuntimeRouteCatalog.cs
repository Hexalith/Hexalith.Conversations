using System.Text.Json;

using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Hexalith.Conversations.Server.Tests.Agents;

// Retains the synthetic receiver's exact lookup inventory while the real SDK invocation guard runs.
// This local adapter simulation establishes no installation or deployed receiver authority.
/// <summary>Supplies a validated exact lookup inventory to the SDK invocation guard in a local simulation.</summary>
internal sealed class DeletionRuntimeRouteCatalog : IRouteCatalogAuthority
{
    /// <summary>Gets the immutable receiver lookup generation.</summary>
    public CommittedRouteCatalogSnapshot Snapshot { get; } = CreateSnapshot();

    /// <inheritdoc/>
    public Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new RouteCatalogDispatchLease(Snapshot, static () => Task.CompletedTask));
    }

    /// <inheritdoc/>
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var envelope = Snapshot.Envelope;
        return Task.FromResult(new RouteCatalogGenerationReadback(Snapshot.Epoch, envelope.Generation,
            Snapshot.RootDigest, envelope.RouteFacetDigest, envelope.IdempotencyFacetDigest, envelope.Profile, [], true));
    }

    /// <inheritdoc/>
    public Task PrepareAsync(RouteCatalogActivation candidate, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc/>
    public Task<RouteCatalogHostAttestation> ReadyAsync(string attemptId, CancellationToken cancellationToken = default)
        => Task.FromException<RouteCatalogHostAttestation>(Immutable());
    /// <inheritdoc/>
    public Task CommitAsync(string attemptId, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc/>
    public Task RollbackAsync(RouteCatalogRollbackRequest request, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc/>
    public Task ReconcileAsync(RouteCatalogQuiescenceRequest request, CancellationToken cancellationToken = default) => Task.FromException(Immutable());

    private static CommittedRouteCatalogSnapshot CreateSnapshot()
    {
        List<RouteContractDeclaration> declarations = [];
        List<RouteCatalogEntry> targets = [];
        List<RouteOperationContract> operations = [];
        foreach (string query in new[] { ConformanceDeletionWire.IdentityQueryType, ConformanceDeletionWire.ReceiptQueryType })
        {
            RouteContractDeclaration declaration = new(query, "query", ConversationDeletionRuntimeOptions.ReceiverDomain,
                query, null, "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
                new(typeof(ConformanceConversationDeletionReceiver).FullName!, "exact", [query], "domain-service", "computed", []));
            RouteOperationContract operation = RouteOperationSchema.Create(declaration, typeof(QueryEnvelope), typeof(QueryResult),
                "internal", "agent-eligible", JsonSerializerOptions.Web);
            declarations.Add(declaration);
            operations.Add(operation);
            targets.Add(new(query, 1, RouteCatalogCodec.ComputeDeclarationDigest(declaration), DeletionRuntimeTestSettings.Receiver,
                "query", "v1", declaration.CredentialKind, declaration.Operation, false,
                ConversationAgentLocalFixture.Tenant.Value, operation.Digest));
        }
        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new(3, 1, "Production", false,
            declarations, targets, [], string.Empty, string.Empty, operations, []));
        return new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
    }

    private static RouteCatalogUnavailableException Immutable() => new("The synthetic receiver lookup catalog is immutable.");
}
