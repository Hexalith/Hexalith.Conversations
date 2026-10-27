using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

using Dapr.Client;
using Hexalith.EventStore.Client.Queries;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Conversations.Contracts.Queries;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>Verifies enrolled component binding, persistent state, and provider isolation without a runtime launch.</summary>
public sealed class ConversationProjectionConfigurationTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>Provider bindings agree across reads, writes, recovery, rebuilds, and native proof enrollment.</summary>
    /// <returns>A task for the persisted-state assertions.</returns>
    [Fact]
    public async Task TwoProvidersKeepIndependentBindingsAcrossAllProjectionPaths()
    {
        InMemoryReadModelStore store = new();
        string unrelatedStore = "unrelated-retained-state";
        store.SeedRaw(unrelatedStore, "sentinel", new ConversationProjectionIndexReadModel());
        byte[] sentinel = store.SnapshotRaw(unrelatedStore, "sentinel")!;
        using ServiceProvider first = Provider(store, "enrolled-first-state");
        using ServiceProvider second = Provider(store, "enrolled-second-state");
        ConversationAsyncProjectionHandler firstHandler = first.GetRequiredService<ConversationAsyncProjectionHandler>();
        ConversationAsyncProjectionHandler secondHandler = second.GetRequiredService<ConversationAsyncProjectionHandler>();
        ProjectionRequest firstInput = ResolutionMetadataFixture.Request("First provider");
        ProjectionRequest secondInput = ResolutionMetadataFixture.Request("Second provider");

        (await firstHandler.ProjectAsync(firstInput, "same-dispatch", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        IReadOnlyDictionary<string, byte[]> firstBefore = store.SnapshotRawEntries("enrolled-first-state");
        (await secondHandler.ProjectAsync(secondInput, "same-dispatch", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        store.SnapshotRawEntries("enrolled-first-state").Keys.ShouldBe(firstBefore.Keys, ignoreOrder: true);
        foreach ((string key, byte[] value) in firstBefore) store.SnapshotRaw("enrolled-first-state", key).ShouldBe(value);

        foreach ((ServiceProvider provider, string component, string label) in new[]
            { (first, "enrolled-first-state", "First provider"), (second, "enrolled-second-state", "Second provider") })
        {
            ConversationProjectionReadStore reader = (ConversationProjectionReadStore)provider.GetRequiredService<IConversationProjectionReadStore>();
            var actual = await reader.ReadAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token);
            actual.ShouldNotBeNull();
            actual.Summary.Label.ShouldBe(label);
            var metadata = await reader.ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token);
            metadata.ShouldNotBeNull();
            metadata.Metadata.SafeLabel.ShouldBe(label);
            await AssertConfiguredAdvisoryAsync(store, provider.GetRequiredService<ConversationProjectionStoreOptions>(), label);
            string indexKey = ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant);
            store.Snapshot<ConversationProjectionIndexReadModel>(component, indexKey)!.Summaries.ShouldHaveSingleItem();
            DomainProjectionOwnerProofRegistration registration = provider.GetServices<DomainProjectionOwnerProofRegistration>()
                .Single(item => item.Domain == ConversationProjectionHandler.ConversationDomain);
            registration.Binding.StoreName.ShouldBe(component);
            string selectionKey = registration.Binding.SelectionKey!(new(firstInput.TenantId, firstInput.Domain, firstInput.AggregateId,
                ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, "test-catalog"));
            selectionKey.ShouldBe(ConversationProjectionReadModelKeys.ResolutionSelectionKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation));
            store.Snapshot<ConversationResolutionNativeSelection>(component, selectionKey).ShouldNotBeNull();
            DomainProjectionRebuildPlan rebuild = await provider.GetRequiredService<ConversationAsyncProjectionHandler>()
                .PrepareRebuildAsync(ResolutionMetadataFixture.Request(label, changedProject: true), "configured-rebuild", Token);
            rebuild.StoreName.ShouldBe(component);
            ReadModelBatchResult rebuilt = await store.ExecuteAsync(new(new(component, firstInput.TenantId, firstInput.Domain,
                firstInput.AggregateId, ConversationAsyncProjectionHandler.ConversationReadModelProjectionType, "configured-rebuild"), rebuild.Operations), Token);
            rebuilt.IsSuccess.ShouldBeTrue();
            (await reader.ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token))!
                .Metadata.ProjectId.ShouldBe("project-new");
        }

        store.SnapshotRawEntries(ConversationProjectionReadModelKeys.StateStoreName).ShouldBeEmpty();
        store.SnapshotRaw(unrelatedStore, "sentinel").ShouldBe(sentinel);
    }

    /// <summary>Interrupted SDK metadata batches recover in the enrolled store and leave other-store bytes untouched.</summary>
    /// <returns>A task for the recovery assertions.</returns>
    [Fact]
    public async Task PendingRecoveryUsesTheConfiguredStore()
    {
        InMemoryReadModelStore store = new();
        const string alternate = "alternate-pending-recovery-state";
        using ServiceProvider alternateProvider = Provider(store, alternate);
        bool alternateInterrupted = false;
        store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!alternateInterrupted && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 1)
            {
                alternateInterrupted = true;
                throw new InvalidOperationException("Bounded alternate-component interruption.");
            }
            return Task.CompletedTask;
        };
        (await alternateProvider.GetRequiredService<ConversationAsyncProjectionHandler>().ProjectAsync(
            ResolutionMetadataFixture.Request("Alternate pending generation"), "alternate-recovery", Token))
            .Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        alternateInterrupted.ShouldBeTrue();
        IReadOnlyDictionary<string, byte[]> alternateBefore = store.SnapshotRawEntries(alternate);
        alternateBefore.ShouldNotBeEmpty();
        using ServiceProvider provider = Provider(store, "enrolled-recovery-state");
        bool interrupted = false;
        store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!interrupted && phase == ReadModelBatchPhase.AfterInstallOperation && ordinal == 1)
            {
                interrupted = true;
                throw new InvalidOperationException("Bounded injected interruption.");
            }
            return Task.CompletedTask;
        };
        ProjectionRequest request = ResolutionMetadataFixture.Request();
        ConversationAsyncProjectionHandler handler = provider.GetRequiredService<ConversationAsyncProjectionHandler>();
        (await handler.ProjectAsync(request, "configured-recovery", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        interrupted.ShouldBeTrue();
        var reader = provider.GetRequiredService<IConversationProjectionReadStore>();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await reader.ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        store.BatchFaultHook = null;
        (await handler.ProjectAsync(request, "configured-recovery", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await reader.ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token)).ShouldNotBeNull();
        store.Snapshot<ConversationResolutionMetadataProof>("enrolled-recovery-state",
            ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation))!
            .Current!.IsPending.ShouldBeFalse();
        AssertUntouched(store, alternate, alternateBefore);
        store.SnapshotRawEntries(ConversationProjectionReadModelKeys.StateStoreName).ShouldBeEmpty();
    }

    /// <summary>Public writes invalidate the configured proof and terminal compensation never touches another component.</summary>
    /// <param name="pendingOnly">Whether the public path only marks pending instead of advancing rich state.</param>
    /// <returns>A task for persisted generation, barrier, and terminal reconciliation assertions.</returns>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfiguredPublicWritesInvalidateProofAndReconcileOnlyTheirTerminalState(bool pendingOnly)
    {
        const string component = "configured-public-write-state";
        const string alternate = "alternate-public-write-state";
        const string terminal = "zz-public-terminal";
        InMemoryReadModelStore store = new();
        using ServiceProvider provider = Provider(store, component);
        using ServiceProvider otherProvider = Provider(store, alternate);
        foreach (ServiceProvider current in new[] { provider, otherProvider })
            (await current.GetRequiredService<ConversationAsyncProjectionHandler>().ProjectAsync(
                ResolutionMetadataFixture.Request(), "completed-generation", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        string detailKey = ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        string indexKey = ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant);
        ConversationProjectedReadModels original = store.Snapshot<ConversationProjectedReadModels>(component, detailKey)!;
        ConversationProjectedReadModels incoming = original with { DispatchId = terminal };
        await otherProvider.GetRequiredService<ConversationProjectionReadModelWriter>().MarkPendingAsync(incoming, Token);
        IReadOnlyDictionary<string, byte[]> alternateBefore = store.SnapshotRawEntries(alternate);
        ConversationProjectionReadModelWriter writer = provider.GetRequiredService<ConversationProjectionReadModelWriter>();
        if (pendingOnly) await writer.MarkPendingAsync(incoming, Token);
        else
        {
            await writer.MarkPendingAsync(incoming, Token);
            // Interrupt only the completed-index CAS after public PersistAsync durably advances detail.
            store.ConcurrentWriteBeforeTrySave = () =>
            {
                if (store.Snapshot<ConversationProjectedReadModels>(component, detailKey)?.DispatchId == terminal)
                    throw new InvalidOperationException("Bounded completed-index write interruption.");
            };
            (await Should.ThrowAsync<InvalidOperationException>(() => writer.PersistAsync(incoming, Token)))
                .Message.ShouldBe("Bounded completed-index write interruption.");
            store.ConcurrentWriteBeforeTrySave = null;
            store.Snapshot<ConversationProjectedReadModels>(component, detailKey)!.DispatchId.ShouldBe(terminal);
        }
        ConversationResolutionMetadataProof proof = store.Snapshot<ConversationResolutionMetadataProof>(component,
            ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation))!;
        proof.Barrier.ShouldNotBeNull();
        proof.Barrier.DispatchId.ShouldBe(terminal);
        proof.Barrier.RequiresNewerGeneration.ShouldBeTrue();
        var reader = provider.GetRequiredService<IConversationProjectionReadStore>();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await reader.ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        store.Snapshot<ConversationProjectionIndexReadModel>(component, indexKey)!.Dispatches[ResolutionMetadataFixture.Conversation.Value]
            .IsPending.ShouldBeTrue();
        (await writer.TryReconcileTerminalDispatchAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, terminal, Token))
            .ShouldBe(pendingOnly);
        ConversationProjectionDispatchReference reference = store.Snapshot<ConversationProjectionIndexReadModel>(component, indexKey)!
            .Dispatches[ResolutionMetadataFixture.Conversation.Value];
        reference.DispatchId.ShouldBe(pendingOnly ? original.DispatchId : terminal);
        reference.IsPending.ShouldBe(!pendingOnly);
        store.Snapshot<ConversationProjectedReadModels>(component, detailKey)!.DispatchId.ShouldBe(pendingOnly ? original.DispatchId : terminal);
        store.Snapshot<ConversationResolutionMetadataProof>(component,
            ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation))!
            .Barrier.ShouldBe(proof.Barrier, "terminal index compensation must not falsely requalify isolated metadata");
        AssertUntouched(store, alternate, alternateBefore);
        store.SnapshotRawEntries(ConversationProjectionReadModelKeys.StateStoreName).ShouldBeEmpty();
    }

    /// <summary>Missing or blank enrollment inputs fail during registration, before storage use.</summary>
    /// <param name="key">The required input.</param>
    /// <param name="value">The absent or blank value.</param>
    [Theory]
    [InlineData(ConversationProjectionStoreOptions.ConfigurationKey, null)]
    [InlineData(ConversationProjectionStoreOptions.ConfigurationKey, " ")]
    [InlineData("EventStore:DataProtection:StateStoreName", null)]
    [InlineData("EventStore:DataProtection:StateStoreName", " ")]
    [InlineData("EventStore:DataProtection:PersistToStateStore", null)]
    [InlineData("EventStore:DataProtection:PersistToStateStore", " ")]
    public void MissingEnrolledStorageInputsFailBeforeAnyStoreUse(string key, string? value)
    {
        IConfiguration configuration = Configuration("enrolled-state", key, value);
        InMemoryReadModelStore store = new();
        ServiceCollection services = new();
        services.AddSingleton<IReadModelStore>(store);
        Should.Throw<InvalidOperationException>(() => services.AddConversationQueries(configuration)).Message.ShouldContain(key);
        store.Count.ShouldBe(0);
    }

    /// <summary>Executes bounded SDK key-ring persistence and cursor purpose isolation over an isolated Dapr fake.</summary>
    /// <returns>A task for key-ring and cursor operations; this is no live restart qualification.</returns>
    [Fact]
    public async Task EnrolledDataProtectionUsesSdkPersistenceAndStableApplication()
    {
        const string component = "enrolled-key-ring";
        const string alternate = "alternate-key-ring";
        string stateKey = "dataprotection-keys-" + ConversationQueryServiceCollectionExtensions.DataProtectionApplicationName;
        IConfiguration configuration = Configuration("enrolled-state", "EventStore:DataProtection:PersistToStateStore", "true");
        configuration["EventStore:DataProtection:OperationTimeout"] = "00:00:02";
        using DaprClient dapr = new InMemoryDaprStateClient();
        (await dapr.TrySaveStateAsync(alternate, stateKey, new List<string> { "<retained-alternate />" }, string.Empty,
            new StateOptions { Concurrency = ConcurrencyMode.FirstWrite }, cancellationToken: Token)).ShouldBeTrue();
        var alternateBefore = await dapr.GetByteStateAndETagAsync(alternate, stateKey, cancellationToken: Token);
        using ServiceProvider provider = KeyRingProvider(dapr, configuration);
        provider.GetRequiredService<IOptions<EventStoreDataProtectionOptions>>().Value.StateStoreName.ShouldBe(component);
        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository!.GetType().Assembly
            .ShouldBe(typeof(EventStoreDataProtectionOptions).Assembly);
        provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator
            .ShouldBe(ConversationQueryServiceCollectionExtensions.DataProtectionApplicationName);
        provider.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        IQueryCursorCodec codec = provider.GetRequiredService<IQueryCursorCodec>();
        string scope = QueryCursorScope.Create().Add("tenant", "configured-tenant").Build();
        string cursor = await Task.Run(() => codec.Encode("configured-list", scope, "position-7"), Token)
            .WaitAsync(TimeSpan.FromSeconds(5), Token);
        codec.TryDecode(cursor, "configured-list", scope, out string? position, out string? failure).ShouldBeTrue();
        position.ShouldBe("position-7");
        failure.ShouldBeNull();
        IDataProtectionProvider protection = provider.GetRequiredService<IDataProtectionProvider>();
        string payload = protection.CreateProtector(ConversationQueryServiceCollectionExtensions.CursorCodecPurpose).Unprotect(cursor);
        using (JsonDocument json = JsonDocument.Parse(payload)) json.RootElement.GetProperty("position").GetString().ShouldBe("position-7");
        Should.Throw<CryptographicException>(() => protection.CreateProtector("Other.Domain.Cursor.v1").Unprotect(cursor));
        List<string> persistedKeys = await dapr.GetStateAsync<List<string>>(component, stateKey, cancellationToken: Token);
        persistedKeys.ShouldContain(xml => xml.Contains("<key", StringComparison.Ordinal));
        using ServiceProvider second = KeyRingProvider(dapr, configuration);
        second.GetRequiredService<IQueryCursorCodec>().TryDecode(cursor, "configured-list", scope, out string? reloaded, out _).ShouldBeTrue();
        reloaded.ShouldBe("position-7");
        (await dapr.GetStateAsync<List<string>>(new EventStoreDataProtectionOptions().StateStoreName, stateKey, cancellationToken: Token)).ShouldBeNull();
        var alternateAfter = await dapr.GetByteStateAndETagAsync(alternate, stateKey, cancellationToken: Token);
        alternateAfter.Item1.ToArray().ShouldBe(alternateBefore.Item1.ToArray());
        alternateAfter.Item2.ShouldBe(alternateBefore.Item2);
        ConversationQueryServiceCollectionExtensions.CursorCodecPurpose.ShouldBe("Hexalith.Conversations.QueryCursor.v1");
    }

    /// <summary>Idempotent Conversations proof registration coexists with an unrelated SDK owner registration.</summary>
    [Fact]
    public void NativeProofRegistrationPreservesOtherOwners()
    {
        ServiceCollection services = new();
        DomainProjectionOwnerProofRegistration other = new("other-domain", "other-projection",
            new(1, 1, 1, "full-replay-v1", "projection-owner-proof/v1", null, null), new("other-state", "selection"));
        services.AddSingleton(other);
        services.AddSingleton(new ConversationProjectionStoreOptions("enrolled-state"));
        services.AddConversationResolutionNativeProof();
        services.AddConversationResolutionNativeProof();
        using ServiceProvider provider = services.BuildServiceProvider();
        DomainProjectionOwnerProofRegistration[] owners = [.. provider.GetServices<DomainProjectionOwnerProofRegistration>()];
        owners.Length.ShouldBe(2);
        owners.ShouldContain(other);
        owners.Single(item => item.Domain == ConversationProjectionHandler.ConversationDomain).Binding.StoreName.ShouldBe("enrolled-state");
    }

    /// <summary>Existing direct constructors continue to share the SDK default without affecting enrolled providers.</summary>
    /// <returns>A task for the compatibility assertions.</returns>
    [Fact]
    public async Task LegacyDirectConstructionRetainsSdkDefaultCompatibility()
    {
        InMemoryReadModelStore store = new();
        ConversationProjectionReadModelWriter writer = new(store, null);
        ConversationAsyncProjectionHandler handler = new(new(), writer, store,
            new ResolutionTestTimeProvider(ResolutionMetadataFixture.Started.AddSeconds(10)));
        (await handler.ProjectAsync(ResolutionMetadataFixture.Request(), "legacy-constructor", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var reader = new ConversationProjectionReadStore(store);
        (await reader.ReadAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token)).ShouldNotBeNull();
        store.Snapshot<ConversationProjectionIndexReadModel>(new EventStoreProcessOptions().StateStoreName,
            ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant)).ShouldNotBeNull();
    }

    private static async Task AssertConfiguredAdvisoryAsync(InMemoryReadModelStore store, ConversationProjectionStoreOptions binding, string label)
    {
        QueryEnvelope query = ResolutionMetadataFixture.Query() with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType };
        IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        List<Claim> claims =
        [
            new(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "eventstore"),
            new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
            .. QueryCallerAuthorityBinding.Bind(query).Select(pair => new Claim(pair.Key, pair.Value)),
        ];
        accessor.HttpContext!.User = new(new ClaimsIdentity(claims, "Workload"));
        accessor.HttpContext.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName] = "verified-owner-hop";
        using ServiceProvider services = new ServiceCollection().AddSingleton(accessor)
            .AddSingleton<IDomainQueryHandler>(new GetConversationAdvisoryMetadataDomainQueryHandler(new ResolutionMetadataAccess(), accessor, store, binding))
            .BuildServiceProvider();
        QueryResult result = await DomainQueryDispatcher.ExecuteAsync(services, query, Token);
        result.Success.ShouldBeTrue();
        result.GetPayload().GetProperty("metadata").GetProperty("safeLabel").GetString().ShouldBe(label);
    }

    private static void AssertUntouched(InMemoryReadModelStore store, string component, IReadOnlyDictionary<string, byte[]> before)
    {
        store.SnapshotRawEntries(component).Keys.ShouldBe(before.Keys, ignoreOrder: true);
        foreach ((string key, byte[] value) in before) store.SnapshotRaw(component, key).ShouldBe(value);
    }

    private static ServiceProvider KeyRingProvider(DaprClient dapr, IConfiguration configuration)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(dapr);
        services.AddConversationQueries(configuration);
        return services.BuildServiceProvider();
    }

    private static ServiceProvider Provider(InMemoryReadModelStore store, string name)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IReadModelStore>(store);
        services.AddSingleton<TimeProvider>(new ResolutionTestTimeProvider(ResolutionMetadataFixture.Started.AddSeconds(10)));
        services.AddConversationQueries(Configuration(name));
        services.AddSingleton<ConversationAsyncProjectionHandler>();
        services.AddConversationResolutionNativeProof();
        return services.BuildServiceProvider();
    }

    private static IConfiguration Configuration(string store, string? overrideKey = null, string? overrideValue = null)
    {
        Dictionary<string, string?> values = new()
        {
            [ConversationProjectionStoreOptions.ConfigurationKey] = store,
            ["EventStore:DataProtection:StateStoreName"] = "enrolled-key-ring",
            ["EventStore:DataProtection:PersistToStateStore"] = "false",
        };
        if (overrideKey is not null) values[overrideKey] = overrideValue;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
