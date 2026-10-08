using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Real catalogue adapter over serialized lifecycle sources; current namespace authority remains synthetic.</summary>
public sealed class EventStoreConversationTenantCatalogueTests : TimeProvider, ISourcePublicationNamespaceSource, IAuthoritativeEventStreamReader
{
    private readonly SourcePublicationScope _scope = new(F.Tenant.Value, "conversation", "catalogue", "installation-1");
    private readonly Dictionary<AggregateIdentity, AuthoritativeEventStream> _sources = [];
    private bool _complete = true;
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
            _now.AddMinutes(1), _sources.Select(s => new SourcePublicationHead(s.Key, s.Value.Head)).ToArray(), _complete));
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
}
