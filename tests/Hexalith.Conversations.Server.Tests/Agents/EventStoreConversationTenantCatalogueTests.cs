using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Real catalogue adapter over serialized lifecycle sources; current namespace authority remains synthetic.</summary>
public sealed class EventStoreConversationTenantCatalogueTests : TimeProvider, ISourcePublicationNamespaceSource, IAuthoritativeEventStreamReader
{
    private readonly SourcePublicationScope _scope = new(F.Tenant.Value, "conversation", "catalogue", "installation-1");
    private readonly Dictionary<AggregateIdentity, AuthoritativeEventStream> _sources = [];
    private bool _complete = true;
    private long? _certifiedHead;
    private int _namespaceReads;
    private DateTimeOffset _now = DateTimeOffset.UtcNow;
    private bool _expireAfterFinalCapture;

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <inheritdoc/>
    public override long GetTimestamp()
    {
        if (_expireAfterFinalCapture && _namespaceReads >= 2) { _now = _now.AddMinutes(2); }
        return base.GetTimestamp();
    }

    /// <inheritdoc/>
    public Task<SourcePublicationCut?> ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken = default)
    {
        _namespaceReads++;
        return Task.FromResult<SourcePublicationCut?>(new(scope, "authority", _now.AddSeconds(-1),
            _now.AddMinutes(1), _sources.Select(s => new SourcePublicationHead(s.Key, _certifiedHead ?? s.Value.Head)).ToArray(), _complete));
    }

    /// <inheritdoc/>
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthoritativeStreamReadResult(_sources.GetValueOrDefault(identity), null));

    private EventStoreConversationTenantCatalogue Catalogue()
        => new(new(_scope), new(this, this, this), this);

    private async Task AddAsync(string id, string lifecycle = "Open")
    {
        var fixture = new F();
        var source = (await fixture.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value))).Stream!;
        var identity = new AggregateIdentity(F.Tenant.Value, "conversation", id);
        // Rename the persisted creation identity and leave all original metadata/replay fields intact.
        var creation = (ConversationCreatedDomainEvent)fixture.PersistedEvents[0];
        creation = new(creation.Metadata with { ConversationId = new(id) });
        var events = new List<StreamReadEvent> { source.Events[0] with { Payload = JsonSerializer.SerializeToUtf8Bytes(creation, F.Options) } };
        if (lifecycle != "Open")
        {
            var changed = new ConversationClosed(fixture.Metadata(ConversationEventType.ConversationClosed,
                F.Human, F.At.AddMinutes(1)) with { ConversationId = new(id) });
            events.Add(new(2, typeof(ConversationClosed).FullName!, JsonSerializer.SerializeToUtf8Bytes(changed, F.Options),
                "json", 1, "lifecycle-event", null, null, F.At.AddMinutes(1), null));
        }
        _sources[identity] = source with { Identity = identity, Head = events.Count, Events = events };
    }

    /// <summary>Count uses persisted creation and current lifecycle, including Conversations without any AI membership or call.</summary>
    [Fact]
    public async Task CompleteSourceCatalogueIncludesZeroCallSourcesAndExcludesClosed()
    {
        await AddAsync("open-zero-calls"); await AddAsync("closed-zero-calls", "Closed");
        var result = await Catalogue().ReadAsync(new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Available); result.Complete.ShouldBeTrue();
        result.Entries!.Count.ShouldBe(2); result.Entries.Single(e => e.ConversationId.Value == "open-zero-calls").Active.ShouldBeTrue();
        result.Entries.Single(e => e.ConversationId.Value == "closed-zero-calls").Active.ShouldBeFalse();
        var fixture = new F(); var query = new ConversationActiveCountQuery(F.Tenant, F.At, F.At.AddMinutes(1));
        var service = new ConversationAgentQueryService(fixture, new(fixture), Catalogue());
        (await service.CountAsync("agents-service", query, TestContext.Current.CancellationToken)).Count.ShouldBe(1);
        (await service.CountAsync("agents-service", query with { CreatedFromInclusive = F.At.AddTicks(1) },
            TestContext.Current.CancellationToken)).Count.ShouldBe(0);
    }

    /// <summary>Partial coverage and missing source prefixes cannot become successful zero denominators.</summary>
    [Fact]
    public async Task PartialCoverageAndCorruptSourceDenyWholeCatalogue()
    {
        await AddAsync("source-a"); _complete = false;
        (await Catalogue().ReadAsync(new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        _complete = true;
        var identity = _sources.Keys.Single(); _sources[identity] = _sources[identity] with { Events = [] };
        (await Catalogue().ReadAsync(new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
    }

    /// <summary>Wrong tenant registration rejects before even consulting namespace authority.</summary>
    [Fact]
    public async Task ForeignTenantCannotReadNamespace()
    {
        var result = await Catalogue().ReadAsync(new(new("tenant-foreign"), F.At, F.At.AddDays(1)), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable); _namespaceReads.ShouldBe(0);
    }

    /// <summary>Namespace authority that expires after its final captured cut cannot survive adapter replay/serialization release.</summary>
    [Fact]
    public async Task ExpiryAfterSourceSnapshotDeniesCatalogueRelease()
    {
        await AddAsync("source-a"); _expireAfterFinalCapture = true;
        var result = await Catalogue().ReadAsync(new(F.Tenant, F.At, F.At.AddDays(1)), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable); result.Entries.ShouldBeNull(); result.Checkpoint.ShouldBeNull();
        _namespaceReads.ShouldBe(2);
    }

    /// <summary>A current stream may have later lifecycle bytes while the exact certified cut folds only its consecutive creation prefix; a prefix gap still denies.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task CertifiedCatalogueCutFoldsOriginalPrefixAfterCurrentHeadAdvances(bool gap)
    {
        await AddAsync("advanced-conversation", "Closed"); _certifiedHead = 1;
        if (gap) { var identity = _sources.Keys.Single(); var source = _sources[identity]; _sources[identity] = source with { Events = [source.Events[0] with { SequenceNumber = 2 }, source.Events[1]] }; }
        var result = await Catalogue().ReadAsync(new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(gap ? ConversationAgentsOutcome.Unavailable : ConversationAgentsOutcome.Available);
        if (!gap) { result.Complete.ShouldBeTrue(); result.Entries!.Single().Active.ShouldBeTrue(); }
    }
    /// <summary>A serialized approved deletion leaves lifecycle Open but is excluded from the complete accepted active denominator.</summary>
    [Fact]
    public async Task DeletedOpenConversationIsExcludedFromCompleteActiveCount()
    {
        var fixture = new F(); var before = await fixture.ReplayAsync();
        fixture.Persist(Hexalith.Conversations.Aggregates.ConversationAggregate.Handle(new Hexalith.Conversations.Commands.ApproveConversationDeletion(
            new(fixture.CommandMetadata(F.Human, "catalogue-deletion"), F.Conversation, "independent-approved-deletion", before.SourceRevision,
                F.At.AddMinutes(1), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "catalogue-deletion-event"), before));
        var identity = new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value);
        var source = (await fixture.ReadAsync(identity, TestContext.Current.CancellationToken)).Stream!;
        byte[] saved = JsonSerializer.SerializeToUtf8Bytes(source, F.Options);
        _sources[identity] = JsonSerializer.Deserialize<AuthoritativeEventStream>(saved, F.Options)!;
        var reconstructed = ConversationAgentSourceReader.ReplaySource(_sources[identity], identity, TestContext.Current.CancellationToken)!.Value.State;
        reconstructed.Lifecycle.ToString().ShouldBe("Open"); reconstructed.IsDeleted.ShouldBeTrue();
        await AddAsync("ordinary-open-zero-calls");
        var catalogue = await Catalogue().ReadAsync(new(F.Tenant, F.At, F.At.AddDays(1)), TestContext.Current.CancellationToken);
        catalogue.Outcome.ShouldBe(ConversationAgentsOutcome.Available); catalogue.Complete.ShouldBeTrue();
        catalogue.Entries!.Single(entry => entry.ConversationId == F.Conversation).Active.ShouldBeFalse();
        catalogue.Entries!.Single(entry => entry.ConversationId.Value == "ordinary-open-zero-calls").Active.ShouldBeTrue();
        var service = new ConversationAgentQueryService(fixture, new(fixture), Catalogue());
        var count = await service.CountAsync("agents-service", new(F.Tenant, F.At, F.At.AddMinutes(1)), TestContext.Current.CancellationToken);
        count.Outcome.ShouldBe(ConversationAgentsOutcome.Available); count.CatalogCheckpoint.ShouldNotBeNullOrWhiteSpace(); count.Count.ShouldBe(1);
        JsonSerializer.SerializeToUtf8Bytes(_sources[identity], F.Options).ShouldBe(saved);
    }

    /// <summary>Both real registration methods resolve the complete source catalogue through the scoped query service.</summary>
    [Fact]
    public async Task RegisteredCatalogueSuppliesCompleteScopedCount()
    {
        await AddAsync("registered-open-zero-calls");
        var services = new ServiceCollection();
        services.AddSingleton<IConversationAgentAuthority>(new F());
        services.AddSingleton<ISourcePublicationNamespaceSource>(this);
        services.AddSingleton<IAuthoritativeEventStreamReader>(this);
        services.AddSingleton<TimeProvider>(this);
        services.AddConversationAgentServices();
        services.AddConversationTenantCatalogue(new(_scope));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var catalogue = scope.ServiceProvider.GetRequiredService<IConversationTenantCatalogue>();
        catalogue.ShouldBeOfType<EventStoreConversationTenantCatalogue>();
        var query = scope.ServiceProvider.GetRequiredService<ConversationAgentQueryService>();
        var result = await query.CountAsync("agents-service", new(F.Tenant, F.At, F.At.AddMinutes(1)), TestContext.Current.CancellationToken);
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        result.Count.ShouldBe(1);
        result.CatalogCheckpoint.ShouldNotBeNullOrWhiteSpace();
    }

}
