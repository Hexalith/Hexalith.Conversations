using System.Text.Json;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionNativeProvenanceTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NativeFixturePagesMoreThanOneHundredEventsWithoutTruncatingProof()
    {
        var initial = ResolutionMetadataFixture.Request();
        var original = System.Text.Json.JsonSerializer.Deserialize<Hexalith.Conversations.Contracts.Events.ConversationCreated>(initial.Events[0].Payload, JsonSerializerOptions.Web)!;
        var events = initial.Events.ToList();
        for (int sequence = 2; sequence <= 101; sequence++)
        {
            var updated = new Hexalith.Conversations.Contracts.Events.ConversationMetadataUpdated(original.Metadata with
            { EventId = "metadata-" + sequence, EventType = Hexalith.Conversations.Contracts.Events.ConversationEventType.ConversationMetadataUpdated,
                OccurredAt = ResolutionMetadataFixture.Started.AddSeconds(sequence) }, Attributes: new Dictionary<string, string> { ["category"] = "stable" });
            events.Add(new(nameof(Hexalith.Conversations.Contracts.Events.ConversationMetadataUpdated),
                JsonSerializer.SerializeToUtf8Bytes(updated, JsonSerializerOptions.Web), "json", sequence,
                updated.Metadata.OccurredAt, "correlation", "source-message-" + sequence));
        }
        var request = await ResolutionMetadataFixture.AdmitNativeAsync(initial with { Events = events.ToArray() });
        request.VerifiedNativeSourceProof.ShouldNotBeNull().HeadSequence.ShouldBe(101);
        var fixture = new ResolutionMetadataFixture();
        fixture.Time.Now = ResolutionMetadataFixture.Started.AddSeconds(200);
        (await fixture.Handler().ProjectAsync(request, "native-101", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.NativeSourceProof.ShouldBe(request.VerifiedNativeSourceProof);
        (await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token))!
            .NativeSourceProof.ShouldBe(request.VerifiedNativeSourceProof);
    }

    [Fact]
    public async Task OriginalVerifiedProofIsCommittedWithSubsetGenerationAndSelectionAndSurvivesRetry()
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest request = ResolutionMetadataFixture.Request();
        var native = request.VerifiedNativeSourceProof.ShouldNotBeNull();
        (await fixture.Handler().ProjectAsync(request, "native-original", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.NativeSourceProof.ShouldBe(native);
        fixture.Proof.Current!.NativeSourceProof.ShouldBe(native);
        var selected = fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation))!;
        selected.NativeSourceProof.ShouldBe(native);
        selected.ProjectionOwner.OwnerGeneration.ShouldBe(fixture.Metadata.Digest());
        byte[] before = JsonSerializer.SerializeToUtf8Bytes(fixture.Metadata);
        fixture.Time.Now = fixture.Time.Now.AddDays(1);
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(), "native-original", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        JsonSerializer.SerializeToUtf8Bytes(fixture.Metadata).ShouldBe(before);
        var guard = new ResolutionGuardedStore(fixture.Store) { ForbidLegacyReads = true, ForbidWrites = true };
        (await fixture.Reader(guard).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant,
            ResolutionMetadataFixture.Conversation, Token))!.NativeSourceProof.ShouldBe(native);
        guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StableDispatchCannotGainLoseOrReplaceItsOriginalNativeEvidence(bool initiallyProven)
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest native = ResolutionMetadataFixture.Request();
        ProjectionRequest legacy = JsonSerializer.Deserialize<ProjectionRequest>(JsonSerializer.Serialize(native with { NativeSourceProof = null }))!;
        ProjectionRequest first = initiallyProven ? native : legacy;
        (await fixture.Handler().ProjectAsync(first, "stable-dispatch", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        byte[] originalMetadata = JsonSerializer.SerializeToUtf8Bytes(fixture.Metadata);
        byte[] originalGeneration = JsonSerializer.SerializeToUtf8Bytes(fixture.Proof);
        (await fixture.Handler().ProjectAsync(initiallyProven ? legacy : native, "stable-dispatch", Token)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        ProjectionRequest replacement = await ResolutionMetadataFixture.AdmitNativeAsync(native, "01ARZ3NDEKTSV4RRFFQ69G5FAW");
        (await fixture.Handler().ProjectAsync(replacement, "stable-dispatch", Token)).Status.ShouldBe(ProjectionDispatchStatus.Failed);
        JsonSerializer.SerializeToUtf8Bytes(fixture.Metadata).ShouldBe(originalMetadata);
        JsonSerializer.SerializeToUtf8Bytes(fixture.Proof).ShouldBe(originalGeneration);
        if (!initiallyProven)
        {
            fixture.Metadata.NativeSourceProof.ShouldBeNull();
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
            (await fixture.DispatchAsync()).GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task UntrustedWireProofDoesNotCertifyLegacyGenerationAndReadsCannotRepairIt()
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest native = ResolutionMetadataFixture.Request();
        ProjectionRequest transport = JsonSerializer.Deserialize<ProjectionRequest>(JsonSerializer.Serialize(native))!;
        transport.NativeSourceProof.ShouldNotBeNull();
        transport.VerifiedNativeSourceProof.ShouldBeNull();
        (await fixture.Handler().ProjectAsync(transport, "legacy-wire", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.NativeSourceProof.ShouldBeNull();
        var guard = new ResolutionGuardedStore(fixture.Store) { ForbidLegacyReads = true, ForbidWrites = true };
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader(guard).ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task InterruptedNativeMetadataBatchRecoversOnlyItsOriginalVerifiedEvidence()
    {
        ResolutionMetadataFixture fixture = new();
        ProjectionRequest request = ResolutionMetadataFixture.Request();
        bool interrupted = false;
        fixture.Store.BatchFaultHook = (phase, ordinal, _) =>
        {
            if (!interrupted && phase == Hexalith.EventStore.Client.Projections.ReadModelBatchPhase.AfterInstallOperation && ordinal == 1)
            {
                interrupted = true;
                throw new InvalidOperationException("interrupted original native metadata batch");
            }
            return Task.CompletedTask;
        };
        (await fixture.Handler().ProjectAsync(request, "native-interrupted", Token)).Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        interrupted.ShouldBeTrue();
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        fixture.Store.BatchFaultHook = null;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.Handler().ProjectAsync(request, "native-interrupted", Token)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        fixture.Metadata.NativeSourceProof.ShouldBe(request.VerifiedNativeSourceProof);
        fixture.Proof.Current!.NativeSourceProof.ShouldBe(request.VerifiedNativeSourceProof);
        fixture.Proof.Current.IsPending.ShouldBeFalse();
        (await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant,
            ResolutionMetadataFixture.Conversation, Token))!.NativeSourceProof.ShouldBe(request.VerifiedNativeSourceProof);
    }

    [Theory]
    [InlineData("incarnation")]
    [InlineData("prefix")]
    [InlineData("head")]
    [InlineData("retention")]
    [InlineData("erased")]
    public async Task AlteredPersistedNativeEvidenceCannotBeDisclosed(string field)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        var proof = original.NativeSourceProof!;
        proof = field switch
        {
            "incarnation" => proof with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" },
            "prefix" => proof with { CumulativeDigest = new('a', 64) },
            "head" => proof with { HeadMessageId = "replacement" },
            "retention" => proof with { RetainedFloor = 2 },
            _ => proof with { Erased = true },
        };
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionMetadataKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), original with { NativeSourceProof = proof });
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        (await fixture.DispatchAsync()).GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
        fixture.Proof.Current!.NativeSourceProof.ShouldBe(original.NativeSourceProof);
    }
}
