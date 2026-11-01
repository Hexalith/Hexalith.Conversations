using System.Text.Json;

using Hexalith.Conversations.Server.Projections;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

// An immutable, validated source-fixture generation. Receiver filters and capability checks remain
// the SDK implementation; this authority supplies no activation or installation qualification.
/// <summary>Supplies an immutable validated generation for the actual projection receiver registrations.</summary>
/// <param name="services">The isolated owner services.</param>
/// <param name="tenant">The exact committed tenant scope.</param>
/// <param name="endpoints">The actual mapped SDK receiver endpoints.</param>
internal sealed class ResolutionRouteCatalog(
    IServiceProvider services, string tenant, Func<IEnumerable<Endpoint>> endpoints) : IRouteCatalogAuthority
{
    /// <summary>Gets the exact application identity of the isolated owner.</summary>
    public const string AppId = "conversations-enrollment-fixture";
    private CommittedRouteCatalogSnapshot? _snapshot;

    /// <summary>Gets the validated generation and observed projection capabilities.</summary>
    public CommittedRouteCatalogSnapshot Snapshot => _snapshot ??= CreateSnapshot();

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
    public Task PrepareAsync(RouteCatalogActivation candidate, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task<RouteCatalogHostAttestation> ReadyAsync(string attemptId, CancellationToken cancellationToken = default)
        => Task.FromException<RouteCatalogHostAttestation>(Immutable());

    /// <inheritdoc/>
    public Task CommitAsync(string attemptId, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task RollbackAsync(RouteCatalogRollbackRequest request, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    /// <inheritdoc/>
    public Task ReconcileAsync(RouteCatalogQuiescenceRequest request, CancellationToken cancellationToken = default)
        => Task.FromException(Immutable());

    private CommittedRouteCatalogSnapshot CreateSnapshot()
    {
        using IServiceScope scope = services.CreateScope();
        IServiceProvider local = scope.ServiceProvider;
        List<RouteContractDeclaration> declarations = [];
        List<RouteCatalogEntry> targets = [];
        List<RouteOperationContract> operations = [];
        foreach (IAsyncDomainProjectionHandler handler in local.GetServices<IAsyncDomainProjectionHandler>())
        {
            Add(handler.Domain + "-" + handler.ProjectionType, handler.Domain, handler.ProjectionType,
                handler.GetType(), "project/v2");
        }
        foreach (IDomainProjectionHandler handler in local.GetServices<IDomainProjectionHandler>())
        {
            Add(handler.Domain + "-legacy", handler.Domain, ConversationProjectionHandler.ConversationProjectionType,
                handler.GetType(), "project");
        }

        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new(3, 1, "Production", false,
            declarations, targets, [], string.Empty, string.Empty, operations, []));
        return new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1,
            RouteCatalogDomainObservation.Auxiliary(local, endpoints()),
            RouteCatalogDomainObservation.Capabilities(local), AppId);

        void Add(string id, string domain, string projection, Type handler, string method)
        {
            RouteContractDeclaration declaration = new(id, "projection", domain, null, projection, "v1", null,
                "workload", EventStoreOperationIdentifiers.DomainServiceProject, null, [],
                new(handler.FullName!, "exact", [projection], "domain-service", null, [projection]));
            RouteOperationContract operation = RouteOperationSchema.Create(declaration,
                method == "project" ? typeof(ProjectionRequest) : typeof(ProjectionDispatchRequest),
                method == "project" ? typeof(ProjectionResponse) : typeof(ProjectionDispatchResponse),
                "internal", "agent-eligible", JsonSerializerOptions.Web);
            declarations.Add(declaration);
            operations.Add(operation);
            targets.Add(new(id, 1, RouteCatalogCodec.ComputeDeclarationDigest(declaration), AppId, method,
                "v1", declaration.CredentialKind, declaration.Operation, false, tenant, operation.Digest));
        }
    }

    private static RouteCatalogUnavailableException Immutable()
        => new("The isolated projection fixture generation is immutable.");
}
