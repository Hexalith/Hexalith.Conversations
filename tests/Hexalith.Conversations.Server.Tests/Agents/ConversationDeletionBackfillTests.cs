using System.Text.Json;
using System.Reflection;
using System.Security.Cryptography;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Server.Streams;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Concrete owner acknowledgement/backfill across actual serialized Conversation sources; actual anchored index actor retains independent modeled authority proofs; backend and enrollment remain synthetic.</summary>
public sealed class ConversationDeletionBackfillTests : TimeProvider, ISourcePublicationNamespaceSource, IAuthoritativeEventStreamReader, ISourcePublicationIndexStore, ISourcePublicationDelivery, ISourcePublicationOperationAuthority
{
    private readonly SourcePublicationScope _scope = new(F.Tenant.Value, "conversation", "approved-deletion-v1", "installed-fixture");
    private readonly Dictionary<AggregateIdentity, F> _sources = [];
    private readonly Dictionary<AggregateIdentity, ConversationDeletionDeliveryPumpFixture> _workers = [];
    private readonly InMemoryStateManager _indexBackend = new();
    private readonly Dictionary<string, string> _admissions = [];
    private readonly Dictionary<string, string> _journal = [];
    private readonly Dictionary<string, string> _progressProofs = [];
    private long _anchor;
    private string _anchorDigest = Hash((SourcePublicationIndexState?)null);
    private long _ticks;
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private TimeSpan _acknowledgementLatency;
    private string _deliveryTarget = "receiver-v1";
    private string _deliveryAuthority = "current-private-worker-authority";
    private bool _resumeAuthorized = true;
    /// <inheritdoc/>
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    /// <inheritdoc/>
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);
    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now.AddTicks(GetTimestamp());
    Task<bool> ISourcePublicationOperationAuthority.ReadIndexAsync(SourcePublicationScope scope) => Task.FromResult(scope == _scope);
    Task<bool> ISourcePublicationOperationAuthority.WriteIndexAsync(SourcePublicationIndexWrite write) => Task.FromResult(write.State.Scope == _scope);
    Task<bool> ISourcePublicationOperationAuthority.ReadNamespaceAsync(SourcePublicationScope scope) => Task.FromResult(false);
    Task<bool> ISourcePublicationOperationAuthority.InstallNamespaceAsync(SourcePublicationNamespaceState installation) => Task.FromResult(false);
    Task<bool> ISourcePublicationOperationAuthority.RegisterSourceAsync(SourcePublicationScope scope, long revision, AggregateIdentity identity) => Task.FromResult(false);
    Task<bool> ISourcePublicationOperationAuthority.ValidateIndexStateAsync(SourcePublicationScope scope, long revision, string digest)
        => Task.FromResult(scope == _scope && revision == _anchor && digest == _anchorDigest);
    Task<bool> IAnchoredStateTransitionAuthority.AdmitTransitionAsync(AnchoredStateTransition value, CancellationToken token)
    {
        string exact = JsonSerializer.Serialize(value);
        if (_admissions.TryGetValue(value.TargetDigest, out var prior)) { return Task.FromResult(prior == exact); }
        if (value.ScopeId != _scope.ActorId + "|source-publications-v1" || value.ExpectedRevision != _anchor || value.TargetRevision != _anchor + 1 || value.PredecessorDigest != _anchorDigest) { return Task.FromResult(false); }
        _admissions[value.TargetDigest] = exact; return Task.FromResult(true);
    }
    Task<bool> IAnchoredStateTransitionAuthority.RecordTransitionAsync(AnchoredStateTransition value, CancellationToken token)
    {
        string exact = JsonSerializer.Serialize(value);
        if (_journal.TryGetValue(value.TargetDigest, out var prior)) { return Task.FromResult(prior == exact); }
        if (!_admissions.TryGetValue(value.TargetDigest, out var admitted) || admitted != exact || value.ExpectedRevision != _anchor || value.PredecessorDigest != _anchorDigest) { return Task.FromResult(false); }
        _anchor = value.TargetRevision; _anchorDigest = value.TargetDigest; _journal[value.TargetDigest] = exact; return Task.FromResult(true);
    }
    Task<bool> IAnchoredStateTransitionAuthority.VerifyTransitionAsync(AnchoredStateTransition value, CancellationToken token)
        => Task.FromResult(_journal.TryGetValue(value.TargetDigest, out var proof) && proof == JsonSerializer.Serialize(value));
    Task<bool> IAnchoredStateTransitionAuthority.RecoverTransitionAsync(AnchoredStateTransition value, CancellationToken token)
        => _admissions.TryGetValue(value.TargetDigest, out var proof) && proof == JsonSerializer.Serialize(value)
            ? ((IAnchoredStateTransitionAuthority)this).RecordTransitionAsync(value, token) : Task.FromResult(false);
    Task<bool> ISourcePublicationOperationAuthority.VerifyDispatchProgressAsync(SourcePublicationCheckpoint cut, SourcePublicationDispatchProgress proof)
        => Task.FromResult(_resumeAuthorized && proof.Scope == _scope && cut.Scope == _scope && proof.SourceAuthorityRevision == "fixed-authenticated-cut"
            && proof.SourceAuthorityRevision == cut.AuthorityRevision && proof.SourcesDigest == Hash(cut.Sources) && proof.LastOffset == cut.LastOffset
            && proof.DeliveryTarget == _deliveryTarget && proof.DeliveryAuthorityRevision == _deliveryAuthority
            && _progressProofs.TryGetValue(proof.ReceiptId, out var original) && original == JsonSerializer.Serialize(proof));
    async Task<SourcePublicationDispatchProgress?> ISourcePublicationOperationAuthority.AuthorizeDispatchAdvanceAsync(SourcePublicationDispatchAdvance advance, SourcePublicationIndexState current)
    {
        if (!_resumeAuthorized) { return null; }
        foreach (var entry in advance.AcknowledgedEntries)
        {
            if (await new ConversationDeletionPublicationDelivery(_workers[entry.Publication.Identity].Pump()).LookupAcknowledgementAsync(entry, TestContext.Current.CancellationToken)
                != SourcePublicationDeliveryStatus.Acknowledged) { return null; }
        }
        long end = advance.AcknowledgedEntries[^1].Offset; long version = (current.DispatchProgress?.Version ?? 0) + 1;
        var proof = new SourcePublicationDispatchProgress(_scope, version, end, current.AuthorityRevision, Hash(current.Sources), current.Entries.Count,
            Hash(current.Entries.Take((int)end).ToArray()), _deliveryTarget, _deliveryAuthority, "independent-actual-ack-prefix-" + version);
        _progressProofs[proof.ReceiptId] = JsonSerializer.Serialize(proof); return proof;
    }
    private SourcePublicationIndexActor IndexActor()
    {
        var actor = new SourcePublicationIndexActor(ActorHost.CreateForTest<SourcePublicationIndexActor>(new ActorTestOptions { ActorId = new(_scope.ActorId) }), this);
        typeof(Dapr.Actors.Runtime.Actor).GetProperty("StateManager", BindingFlags.Instance | BindingFlags.Public)!.SetValue(actor, _indexBackend); return actor;
    }
    private static string Hash<T>(T value) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value)));
    Task<SourcePublicationCut?> ISourcePublicationNamespaceSource.ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken)
        => Task.FromResult<SourcePublicationCut?>(new(scope, "fixed-authenticated-cut", GetUtcNow().AddSeconds(-1), GetUtcNow().AddHours(1), _sources.Keys.Select(id => new SourcePublicationHead(id, 2)).ToArray(), true));
    /// <inheritdoc/>
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default) => _sources[identity].ReadAsync(identity, cancellationToken);
    Task<SourcePublicationIndexState?> ISourcePublicationIndexStore.ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken)
        => IndexActor().ReadAsync(scope);
    /// <inheritdoc/>
    public Task<bool> TryWriteAsync(SourcePublicationScope scope, long expectedRevision, SourcePublicationIndexState next, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(next); return IndexActor().TryWriteAsync(new(expectedRevision, next));
    }
    /// <inheritdoc/>
    public async Task<SourcePublicationDeliveryStatus> LookupAcknowledgementAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var status = await new ConversationDeletionPublicationDelivery(_workers[entry.Publication.Identity].Pump()).LookupAcknowledgementAsync(entry, cancellationToken);
        Interlocked.Add(ref _ticks, _acknowledgementLatency.Ticks); return status;
    }
    /// <inheritdoc/>
    public Task<SourcePublicationDispatchProgress?> ReadDispatchProgressAsync(SourcePublicationCheckpoint cut, CancellationToken cancellationToken = default)
    { ArgumentNullException.ThrowIfNull(cut); return IndexActor().ReadDispatchProgressAsync(cut); }
    /// <inheritdoc/>
    public Task<SourcePublicationDispatchProgress?> AdvanceDispatchProgressAsync(SourcePublicationDispatchAdvance advance, CancellationToken cancellationToken = default)
    { ArgumentNullException.ThrowIfNull(advance); return IndexActor().AdvanceDispatchProgressAsync(advance); }
    /// <inheritdoc/>
    public Task<SourcePublicationDeliveryStatus> DeliverAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
        => new ConversationDeletionPublicationDelivery(_workers[entry.Publication.Identity].Pump()).DeliverAsync(entry, cancellationToken);

    /// <summary>A bounded pass delivers the first hundred distinct source originals; restart re-verifies their actual durable acknowledgements and delivers originals 101–103 exactly once.</summary>
    [Fact]
    public async Task ActualSourceAcknowledgementsAdvanceBoundedDispatcherAfterRestart()
    {
        var scope = _scope; var workers = _workers; var projector = new ConversationDeletionPublicationProjector();
        await AddSourcesAsync(103);
        SourcePublicationDispatcher Dispatcher() => new(new SourcePublicationFeed(this, this, projector, this, this), this, this);
        var first = await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken); first.AcknowledgedPrefix.ShouldBe(100); first.IsComplete.ShouldBeFalse();
        workers.Values.Sum(f => f.Submissions).ShouldBe(100); await RestartIndexAsync();
        var second = await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken); second.AcknowledgedPrefix.ShouldBe(103); second.IsComplete.ShouldBeTrue();
        workers.Values.Sum(f => f.Submissions).ShouldBe(103); workers.Values.All(f => f.Submissions == 1 && f.AttemptCommands.Count == 1).ShouldBeTrue();
        foreach (var owner in workers.Values) { (await owner.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(2); }
        (await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken)).IsComplete.ShouldBeTrue(); workers.Values.Sum(f => f.Submissions).ShouldBe(103);
    }
    private async Task RestartIndexAsync()
    {
        foreach (var saved in _indexBackend.CommittedState.ToArray())
        { await _indexBackend.SetStateAsync(saved.Key, JsonSerializer.Deserialize<SourcePublicationIndexState>(JsonSerializer.Serialize(saved.Value))!, TestContext.Current.CancellationToken); }
        await _indexBackend.SaveStateAsync(TestContext.Current.CancellationToken); await _indexBackend.ClearCacheAsync(TestContext.Current.CancellationToken);
    }
    private async Task AddSourcesAsync(int count)
    {
        var projector = new ConversationDeletionPublicationProjector(); var sources = _sources; var workers = _workers;
        for (int n = 1; n <= count; n++)
        {
            var source = new F(new ConversationId("conversation-" + n.ToString("D3", global::System.Globalization.CultureInfo.InvariantCulture)));
            var before = await source.ReplayAsync();
            source.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(source.CommandMetadata(F.Human, "approval"), source.CurrentConversation,
                "independent-approval", before.SourceRevision, F.At.AddMinutes(1), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
            var identity = new AggregateIdentity(F.Tenant.Value, "conversation", source.CurrentConversation.Value);
            var stream = (await source.ReadAsync(identity, TestContext.Current.CancellationToken)).Stream!;
            var entry = new SourcePublicationIndexEntry(n, projector.Project(stream, TestContext.Current.CancellationToken).Single());
            sources.Add(identity, source); workers.Add(identity, new(source, entry));
        }
    }
    /// <summary>A finite multi-page actual acknowledged history exceeds one whole pass; independent anchored progress survives every dispatcher/index restart and eventually reaches a later original.</summary>
    [Fact]
    public async Task ActualSourceLongAcknowledgedPrefixMakesDurableDeadlineProgressAcrossRestart()
    {
        await AddSourcesAsync(202);
        foreach (var worker in _workers.Values.Take(201)) { (await worker.Pump().DeliverAsync(worker.Entry, TestContext.Current.CancellationToken)).ShouldBe(Hexalith.Conversations.Contracts.Agents.ConversationAgentsOutcome.Available); }
        var later = _workers.Values.Last(); later.UnknownLookup = true;
        _acknowledgementLatency = TimeSpan.FromSeconds(1);
        SourcePublicationDispatcher Dispatcher() => new(new SourcePublicationFeed(this, this, new ConversationDeletionPublicationProjector(), this, this), this, this);
        var first = await Dispatcher().DispatchAsync(_scope, 100, TestContext.Current.CancellationToken);
        first.IsComplete.ShouldBeFalse(); first.FailureReason.ShouldBe("publication-dispatch-time-bound-exceeded");
        var original = (await IndexActor().ReadAsync(_scope))!.DispatchProgress!; original.AcknowledgedPrefix.ShouldBeGreaterThan(0); original.AcknowledgedPrefix.ShouldBeLessThan(201);
        long prior = original.AcknowledgedPrefix; bool complete = false;
        for (int pass = 0; pass < 10 && prior < 201; pass++)
        {
            await RestartIndexAsync(); var result = await Dispatcher().DispatchAsync(_scope, 100, TestContext.Current.CancellationToken);
            var retained = (await IndexActor().ReadAsync(_scope))!.DispatchProgress!;
            retained.AcknowledgedPrefix.ShouldBeGreaterThan(prior); prior = retained.AcknowledgedPrefix; complete = result.IsComplete;
        }
        complete.ShouldBeFalse(); prior.ShouldBe(201); later.Submissions.ShouldBe(0); (await later.Source.ReplayAsync()).DeletionSource.Acknowledgement.ShouldBeNull();
        later.UnknownLookup = false; await RestartIndexAsync(); var final = await Dispatcher().DispatchAsync(_scope, 100, TestContext.Current.CancellationToken);
        final.IsComplete.ShouldBeTrue(); final.AcknowledgedPrefix.ShouldBe(202); _workers.Values.Sum(worker => worker.Submissions).ShouldBe(202);
        _workers.Values.All(worker => worker.Submissions == 1).ShouldBeTrue();
        (await _workers.Values.Last().Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(2);
    }
    /// <summary>A retained original checkpoint cannot cross a changed receiver or current worker authority; no unchecked process cursor can establish resume.</summary>
    [Theory]
    [InlineData("target")][InlineData("authority")][InlineData("unavailable")][InlineData("scope")][InlineData("installation")][InlineData("cut")][InlineData("unowned")]
    public async Task ActualDurableProgressIsDeniedForChangedCurrentDeliveryBinding(string changed)
    {
        await AddSourcesAsync(2);
        var feed = new SourcePublicationFeed(this, this, new ConversationDeletionPublicationProjector(), this, this);
        var first = await new SourcePublicationDispatcher(feed, this, this).DispatchAsync(_scope, 1, TestContext.Current.CancellationToken); first.AcknowledgedPrefix.ShouldBe(1);
        var state = (await IndexActor().ReadAsync(_scope))!; var cut = new SourcePublicationCheckpoint(_scope, state.Revision, state.AuthorityRevision, state.Sources, state.Entries.Count);
        await RestartIndexAsync();
        if (changed == "target") { _deliveryTarget = "receiver-v2"; }
        else if (changed == "authority") { _deliveryAuthority = "new-private-authority"; }
        else if (changed == "unavailable") { _resumeAuthorized = false; }
        else if (changed == "scope") { cut = cut with { Scope = new(_scope.Tenant, _scope.Domain, "other-feed", _scope.InstallationId) }; }
        else if (changed == "installation") { cut = cut with { Scope = new(_scope.Tenant, _scope.Domain, _scope.FeedName, "other-installation") }; }
        else if (changed == "cut") { cut = cut with { Sources = cut.Sources.Select(head => head with { Head = head.Head + 1 }).ToArray() }; }
        else
        {
            var forged = state.DispatchProgress! with { ReceiptId = "unowned-prefix-proof" };
            (await IndexActor().TryWriteAsync(new(state.Revision, state with { Revision = state.Revision + 1, DispatchProgress = forged }))).ShouldBeFalse();
            (await IndexActor().ReadAsync(_scope))!.Revision.ShouldBe(state.Revision);
            cut = cut with { AuthorityRevision = "unowned-cut" };
        }
        (await IndexActor().ReadDispatchProgressAsync(cut)).ShouldBeNull();
        JsonSerializer.Serialize((await IndexActor().ReadAsync(_scope))!.DispatchProgress).ShouldBe(JsonSerializer.Serialize(state.DispatchProgress));
    }
}
