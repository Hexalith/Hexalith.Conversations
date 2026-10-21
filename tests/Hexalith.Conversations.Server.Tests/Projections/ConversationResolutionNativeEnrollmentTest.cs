using System.Text.Json;

using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.Server.Tests.Projections;

public sealed class ConversationResolutionNativeEnrollmentTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LegacyLedgerWithoutAdmissionEvidenceCannotAcquireProofAfterVerifierRecovery()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var request = fixture.Dispatch(catalog.CatalogFingerprint!);
        fixture.Gateway.Unavailable = true;
        string proofKey = ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        fixture.Backend.BeforeWrite = key => { if (key == proofKey) throw new InvalidOperationException("Interrupted before generation proof."); };
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request)).Outcomes[0].Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        fixture.Backend.BeforeWrite = null;
        string ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(request.DispatchId);
        var prior = (await fixture.Store.GetAsync<ConversationProjectionDispatchLedger>("statestore", ledgerKey, Token)).Value!;
        var legacy = prior with { NativeAdmission = null };
        JsonSerializer.Serialize(legacy, JsonSerializerOptions.Web).ShouldNotContain("nativeAdmission");
        await fixture.Store.SaveAsync("statestore", ledgerKey, legacy, Token);
        fixture.Gateway.Unavailable = false;
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        metadata.NativeAdmission.ShouldBeNull();
        metadata.NativeSourceProof.ShouldBeNull();
        JsonSerializer.Serialize(metadata, JsonSerializerOptions.Web).ShouldNotContain("nativeAdmission");
    }

    [Theory]
    [InlineData("null-empty")]
    [InlineData("timestamp-offset")]
    [InlineData("segment-boundary")]
    public async Task VerifierOutageCannotHideExactHintedMetadataMutation(string mutation)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        fixture.Gateway.Unavailable = true;
        ProjectionRequest original = fixture.Input;
        if (mutation == "segment-boundary") original = original with
            { Events = [original.Events[0] with { MessageId = "x\u001fy", CorrelationId = "z" }] };
        var first = fixture.Dispatch(catalog.CatalogFingerprint!, input: original);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", first)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProjectionEventDto changed = mutation switch
        {
            "null-empty" => original.Events[0] with { UserId = string.Empty },
            "timestamp-offset" => original.Events[0] with { Timestamp = original.Events[0].Timestamp.ToOffset(TimeSpan.FromHours(1)) },
            _ => original.Events[0] with { MessageId = "x", CorrelationId = "y\u001fz" },
        };
        var retry = first with { Request = original with { Events = [changed] } };
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", retry)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Failed);
        (await fixture.MetadataAsync()).NativeSourceProof.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task OriginalAdmissionProofOrAbsenceSurvivesVerifierOutageAndRecovery(bool initiallyAvailable, bool interrupted, bool loseLedger)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var request = fixture.Dispatch(catalog.CatalogFingerprint!);
        fixture.Gateway.Unavailable = !initiallyAvailable;
        string detailKey = ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        if (interrupted) fixture.Backend.BeforeWrite = key =>
        {
            if (key == detailKey) throw new InvalidOperationException("Interrupted first generation after admission.");
        };
        var first = await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request);
        (first.Outcomes[0].Status == ProjectionDispatchStatus.Completed).ShouldBe(!interrupted);
        var ledgerKey = ConversationProjectionReadModelKeys.DispatchLedgerKey(request.DispatchId);
        var original = await fixture.Store.GetAsync<ConversationProjectionDispatchLedger>("statestore", ledgerKey, Token);
        var decision = original.Value.ShouldNotBeNull().NativeAdmission.ShouldNotBeNull();
        decision.ProducerHint.ShouldBe(fixture.Input.NativeSourceProof);
        decision.AdmittedProof.ShouldBe(initiallyAvailable ? fixture.Input.NativeSourceProof : null);
        fixture.Backend.BeforeWrite = null;
        if (loseLedger) (await fixture.Store.TryEraseAsync("statestore", ledgerKey, original.ETag!, Token)).ShouldBeTrue();
        fixture.Gateway.Unavailable = initiallyAvailable;
        fixture.Time.Now = fixture.Time.Now.AddHours(1);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        metadata.NativeAdmission.ShouldBe(decision);
        metadata.NativeSourceProof.ShouldBe(decision.AdmittedProof);
        metadata.Freshness.ProjectionGeneratedAt.ShouldBe(original.Value.ProjectionGeneratedAt);
        var finalLedger = (await fixture.Store.GetAsync<ConversationProjectionDispatchLedger>("statestore", ledgerKey, Token)).Value!;
        finalLedger.NativeAdmission.ShouldBe(decision);
        finalLedger.RequestFingerprint.ShouldBe(original.Value.RequestFingerprint);
        fixture.Gateway.Unavailable = true;
        var altered = request with { Request = request.Request with
            { Events = [request.Request.Events[0] with { MessageId = "different-native-event" }] } };
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", altered)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Failed);
        var lostHint = request with { Request = request.Request with { NativeSourceProof = null } };
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", lostHint)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Failed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        if (!initiallyAvailable)
        {
            await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
                await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(
                    ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
        }
    }

    [Fact]
    public async Task ExplicitEnrollmentNegotiatesAndReadsActualPersistedSdkOwnerProofWithoutContentOrWrites()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var ordinary = await fixture.CatalogAsync(guarded: false);
        var guarded = await fixture.CatalogAsync();
        guarded.CatalogFingerprint.ShouldNotBe(ordinary.CatalogFingerprint);
        var domain = guarded.Domains.ShouldHaveSingleItem();
        domain.SourceGuardCapabilities.ShouldNotBeNull()[ResolutionEnrollmentFixture.Route].IsSupported.ShouldBeTrue();
        domain.ProjectionNames.ShouldBe(ordinary.Domains[0].ProjectionNames);
        domain.NamedProjectionTypes.ShouldBe(ordinary.Domains[0].NamedProjectionTypes);
        domain.OrdinaryProjectionTypes.ShouldBe(ordinary.Domains[0].OrdinaryProjectionTypes);
        fixture.App.Services.GetServices<DomainProjectionOwnerProofRegistration>().ShouldHaveSingleItem();
        var response = await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2",
            fixture.Dispatch(guarded.CatalogFingerprint!));
        response.Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        metadata.NativeSourceProof.ShouldBe(fixture.Input.NativeSourceProof);
        int writes = fixture.Backend.WrittenKeys.Count;
        fixture.Backend.ReadKeys.Clear();
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Read attempted mutation.");
        fixture.Backend.BeforeRead = key =>
        {
            key.ShouldNotBe(ConversationProjectionReadModelKeys.ConversationKey(
                ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation));
            key.ShouldNotBe(ConversationProjectionReadModelKeys.TenantIndexKey(ResolutionMetadataFixture.Tenant));
        };
        var proof = await fixture.OwnerAsync(guarded.CatalogFingerprint!);
        proof.Proof.ShouldNotBeNull().Selection.OwnerGeneration.ShouldBe(metadata.Digest());
        proof.Proof.Selection.AppliedSequence.ShouldBe(metadata.NativeSourceProof!.HeadSequence);
        proof.Proof.Selection.MessageId.ShouldBe(metadata.NativeSourceProof.HeadMessageId);
        proof.MetadataKeyReads.ShouldBe(2);
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token)).ShouldBe(metadata);
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
        (await fixture.OwnerAsync(ordinary.CatalogFingerprint!)).Proof.ShouldBeNull();
        (await fixture.OwnerAsync(guarded.CatalogFingerprint!, tenant: "other-tenant")).Proof.ShouldBeNull();
        (await fixture.OwnerAsync(guarded.CatalogFingerprint!, aggregate: "other-conversation")).Proof.ShouldBeNull();
        (await fixture.OwnerAsync(guarded.CatalogFingerprint!, route: "other-route")).Proof.ShouldBeNull();
    }

    [Fact]
    public async Task DefaultLegacyAndCompatibilityDeliveryPreserveRoutesWithoutGuardCertificate()
    {
        await using var fixture = new ResolutionEnrollmentFixture(enroll: false);
        var ordinary = await fixture.CatalogAsync(guarded: false);
        var requested = await fixture.CatalogAsync();
        requested.CatalogFingerprint.ShouldBe(ordinary.CatalogFingerprint);
        requested.Domains[0].SourceGuardCapabilities.ShouldBeNull();
        int writes = fixture.Backend.WrittenKeys.Count;
        var legacy = await fixture.PostAsync<ProjectionResponse>("/project", fixture.Input with { NativeSourceProof = null });
        legacy.ProjectionType.ShouldBe(ConversationProjectionHandler.ConversationProjectionType);
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
        using (IServiceScope scope = fixture.App.Services.CreateScope())
        {
            var handler = new LegacyDomainProjectionHandlerAdapter(
                scope.ServiceProvider.GetServices<IDomainProjectionHandler>().ShouldHaveSingleItem(),
                fixture.Input.Domain, ConversationProjectionHandler.ConversationProjectionType);
            (await handler.ProjectAsync(fixture.Input with { NativeSourceProof = null }, "compatibility", Token))
                .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        }
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", fixture.Dispatch(ordinary.CatalogFingerprint!)))
            .Outcomes.ShouldHaveSingleItem().Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.OwnerAsync(ordinary.CatalogFingerprint!)).Proof.ShouldBeNull();
    }

    [Theory]
    [InlineData("event")]
    [InlineData("proof")]
    [InlineData("replacement")]
    public async Task GuardedOriginalInputTamperingRejectsBeforePersistedEffects(string change)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var altered = change switch
        {
            "event" => fixture.Input with { Events = [fixture.Input.Events[0] with { UserId = "altered" }] },
            "proof" => fixture.Input with { NativeSourceProof = fixture.Input.NativeSourceProof! with { CumulativeDigest = new string('a', 64) } },
            _ => fixture.Input with { NativeSourceProof = fixture.Input.NativeSourceProof! with { SourceIncarnation = "01ARZ3NDEKTSV4RRFFQ69G5FAW" } },
        };
        int writes = fixture.Backend.WrittenKeys.Count;
        // The endpoint maps the typed admission failure to 400 before a handler can write.
        var refusal = await fixture.PostAsync<JsonElement>("/project/v2",
            fixture.Dispatch(catalog.CatalogFingerprint!, input: altered), expectedStatus: 400);
        refusal.GetString().ShouldBe(ProjectionDispatchReasonCodes.DeliveryIdentityConflict);
        fixture.Backend.WrittenKeys.Count.ShouldBe(writes);
    }

    [Fact]
    public async Task ReconciliationPreservesOriginalGenerationAndSelectionTamperingRefusesProof()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var request = fixture.Dispatch(catalog.CatalogFingerprint!);
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request)).Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.MetadataAsync()).ShouldBe(metadata);
        var key = ConversationProjectionReadModelKeys.ResolutionSelectionKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        var selected = await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore", key, Token);
        await fixture.Store.SaveAsync("statestore", key, selected.Value! with
            { ProjectionOwner = selected.Value!.ProjectionOwner with { OwnerGeneration = "altered" } }, Token);
        (await fixture.OwnerAsync(catalog.CatalogFingerprint!)).Proof.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DirectRichWriterInvalidatesEnrolledMetadataBeforeEitherPublicEntryPoint(bool markPending)
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", fixture.Dispatch(catalog.CatalogFingerprint!)))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var models = (await fixture.Store.GetAsync<ConversationProjectedReadModels>("statestore",
            ConversationProjectionReadModelKeys.ConversationKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), Token)).Value!;
        var writer = new ConversationProjectionReadModelWriter(fixture.Store);
        if (markPending) await writer.MarkPendingAsync(models, Token);
        else await writer.PersistAsync(models, Token);
        await Should.ThrowAsync<ConversationProjectionConsistencyException>(async () =>
            await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(
                ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token));
    }

    [Fact]
    public async Task InterruptedEnrolledDeliveryRecoversTheSameOriginalEvidenceThroughReconciliation()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var request = fixture.Dispatch(catalog.CatalogFingerprint!);
        string selectionKey = ConversationProjectionReadModelKeys.ResolutionSelectionKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        fixture.Backend.BeforeWrite = key =>
        {
            if (key == selectionKey) throw new InvalidOperationException("Interrupted selection write.");
        };
        var interrupted = await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2", request);
        interrupted.Outcomes[0].Status.ShouldNotBe(ProjectionDispatchStatus.Completed);
        (await fixture.OwnerAsync(catalog.CatalogFingerprint!)).Proof.ShouldBeNull();
        fixture.Backend.BeforeWrite = null;
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/v2/reconcile", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        metadata.NativeSourceProof.ShouldBe(fixture.Input.NativeSourceProof);
        (await fixture.OwnerAsync(catalog.CatalogFingerprint!)).Proof.ShouldNotBeNull().Selection.OwnerGeneration.ShouldBe(metadata.Digest());
        (await new ConversationProjectionReadStore(fixture.Store).ReadResolutionMetadataAsync(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation, Token)).ShouldBe(metadata);
    }

    [Fact]
    public async Task GuardedRebuildRetainsOriginalEvidenceAndKeepsContentFreePromotionGated()
    {
        await using var fixture = new ResolutionEnrollmentFixture();
        var catalog = await fixture.CatalogAsync();
        var request = fixture.Dispatch(catalog.CatalogFingerprint!, fixture.Input.Events[^1].MessageId!,
            fixture.Input with { RebuildSourceHeadSequence = fixture.Input.Events[^1].SequenceNumber });
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/stage/v1", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        (await fixture.OwnerAsync(catalog.CatalogFingerprint!)).Proof.ShouldBeNull();
        (await fixture.PostAsync<ProjectionDispatchResponse>("/project/rebuild/commit/v1", request))
            .Outcomes[0].Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var metadata = await fixture.MetadataAsync();
        metadata.NativeSourceProof.ShouldBe(fixture.Input.NativeSourceProof);
        var selected = (await fixture.Store.GetAsync<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), Token)).Value!;
        selected.NativeSourceProof.ShouldBe(fixture.Input.NativeSourceProof);
        selected.ProjectionOwner.ReceiptProjectionName.ShouldBe("rebuild");
        (await fixture.OwnerAsync(catalog.CatalogFingerprint!)).Proof.ShouldNotBeNull().Selection.ShouldBe(selected.ProjectionOwner);
        // An SDK receipt alone does not qualify content-free post-rebuild promotion. The existing
        // owner query refuses the coordinated generation before invoking delegated transport.
        fixture.Backend.ReadKeys.Clear();
        fixture.Backend.BeforeRead = key => key.ShouldBe(ConversationProjectionReadModelKeys.ResolutionSelectionKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation));
        fixture.Backend.BeforeWrite = _ => throw new Xunit.Sdk.XunitException("Rebuild read attempted mutation.");
        (await new ConversationResolutionNativeEvidence(null!, fixture.Store).ReadProjectionAsync(metadata, Token)).ShouldBeNull();
        fixture.Backend.ReadKeys.ShouldHaveSingleItem();
    }
}
