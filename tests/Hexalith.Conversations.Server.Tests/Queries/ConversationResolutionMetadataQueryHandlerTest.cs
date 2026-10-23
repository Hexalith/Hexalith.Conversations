using System.Text.Json;

using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.Conversations.Server.Tests.Queries;

public sealed class ConversationResolutionMetadataQueryHandlerTest
{
    [Fact]
    public async Task FutureNativeTenantEvidenceIsUnavailableBeforeMetadataReads()
    {
        var fixture = new ResolutionMetadataFixture(); await fixture.ProjectAsync();
        fixture.Native.TenantVerifiedAtOffset = TimeSpan.FromSeconds(1);
        var result = await fixture.DispatchAsync();
        result.Success.ShouldBeTrue(); result.GetPayload().GetProperty("metadata").ValueKind.ShouldBe(JsonValueKind.Null);
        fixture.Guard.Reads.ShouldBeEmpty();
    }


    [Theory]
    [InlineData(null)]
    [InlineData("inspection")]
    public async Task OlderImplicitClassificationOrAnotherPurposeCannotAuthorizeScoring(string? purpose)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var unsupported = fixture.Metadata with { LabelPurpose = purpose };
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionMetadataKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), unsupported);
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            new ConversationResolutionMetadataProof(ConversationResolutionGeneration.From(unsupported, false)));
        AssertUnavailable(await fixture.DispatchAsync());
    }

    [Theory]
    [InlineData("version")]
    [InlineData("watermark")]
    public async Task NonNullDecisionEvidenceMustIndependentlyMatchStableHealthySignalBeforeLookup(string mismatch)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        if (mismatch == "version") fixture.Access.DecisionVersionOverride = 8;
        else fixture.Access.DecisionWatermarkOverride = "other-native-watermark";
        AssertUnavailable(await fixture.DispatchAsync());
        fixture.Access.Checks.ShouldBe(1);
        fixture.Guard.ReadKeys.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("first-read")]
    [InlineData("final-auth")]
    [InlineData("confirmation-read")]
    public async Task CancellationDuringEvidenceAwaitsPropagatesWithoutUsableResponse(string boundary)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        using CancellationTokenSource cancellation = new();
        int proofReads = 0;
        fixture.Guard.AfterRead = (type, _) =>
        {
            if (type == typeof(ConversationResolutionMetadataProof))
            {
                proofReads++;
                if (boundary == "first-read" && proofReads == 1 || boundary == "confirmation-read" && proofReads == 4)
                    cancellation.Cancel();
            }
            return Task.CompletedTask;
        };
        fixture.Access.OnCheck = count => { if (boundary == "final-auth" && count == 2) cancellation.Cancel(); };
        await Should.ThrowAsync<OperationCanceledException>(() => fixture.DispatchAsync(cancellationToken: cancellation.Token));
        cancellation.IsCancellationRequested.ShouldBeTrue();
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("secret value")]
    [InlineData("https://private.example/document")]
    [InlineData("line\nbreak")]
    [InlineData("   ")]
    public async Task BoundedUnclassifiableLabelsWithholdCompleteScoringWithoutTruncation(string label)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync(label: label);
        fixture.Metadata.IsLabelComplete.ShouldBeFalse();
        fixture.Metadata.LabelClassification.ShouldBeNull();
        fixture.Metadata.Metadata.SafeLabel.ShouldBeNull();
        AssertUnavailable(await fixture.DispatchAsync());
    }

    [Fact]
    public async Task PersistedOwnerClassificationIsRequiredAndInspectionPurposeCannotBorrowReadAuthority()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Metadata.LabelClassification.ShouldBe(ConversationResolutionScoringPolicy.Classification);
        ConversationResolutionScoringPolicy.CanClassify("Scoring label", "inspection").ShouldBeFalse();
        var accessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var authorization = new Hexalith.Conversations.Server.Queries.ConversationResolutionMetadataAuthorization(
            fixture.Access, accessor, fixture.Access, fixture.Time);
        var denied = await authorization.CheckAsync(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation,
            "actor", "inspection", TestContext.Current.CancellationToken);
        denied.Denied.ShouldBeTrue();
        fixture.Access.Checks.ShouldBe(0);
        var unsupported = fixture.Metadata with { LabelClassification = "caller-asserted-classification" };
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionMetadataKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), unsupported);
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            new ConversationResolutionMetadataProof(ConversationResolutionGeneration.From(unsupported, false)));
        AssertUnavailable(await fixture.DispatchAsync());
    }

    [Fact]
    public async Task UnsupportedFreshnessSchemaIsWithheldEvenWhenProofAndMetadataAgree()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var freshness = fixture.Metadata.Freshness;
        var unsupported = fixture.Metadata with { Freshness = new(new(2), freshness.ProjectionCursor,
            freshness.LastAppliedEventPosition, freshness.LastAppliedEventTimestamp, freshness.ProjectionGeneratedAt,
            freshness.LagDuration, freshness.IsStale, freshness.FreshnessState, freshness.ReasonCode) };
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionMetadataKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation), unsupported);
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            new ConversationResolutionMetadataProof(ConversationResolutionGeneration.From(unsupported, false)));
        AssertUnavailable(await fixture.DispatchAsync());
    }

    [Fact]
    public async Task TwoIndividuallyCoherentDifferentCompletedGenerationsCannotBeCombinedAcrossOwnerChecks()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        var original = fixture.Metadata;
        fixture.Access.BeforeDecision = async count =>
        {
            if (count == 2)
            {
                await fixture.ProjectAsync("dispatch-new-completed", changedProject: true);
                fixture.Proof.Current!.IsPending.ShouldBeFalse();
                (await fixture.Reader().ReadResolutionMetadataAsync(ResolutionMetadataFixture.Tenant,
                    ResolutionMetadataFixture.Conversation, TestContext.Current.CancellationToken))!.Metadata.ProjectId.ShouldBe("project-new");
            }
        };
        AssertUnavailable(await fixture.DispatchAsync());
        fixture.Metadata.ShouldNotBe(original);
        Deserialize(await fixture.DispatchAsync()).Metadata!.ProjectId.ShouldBe("project-new");
    }

    [Fact]
    public async Task VerifiedCurrentReadReturnsOnlyClassifiedSubsetAndNativeEvidenceWithoutMutations()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync(label: " exact SCORING label ");
        ConversationResolutionMetadataReadModel stored = fixture.Metadata;
        int keys = fixture.Store.Count;
        QueryResult result = await fixture.DispatchAsync();
        result.Success.ShouldBeTrue();
        var value = Deserialize(result);
        value.Metadata.ShouldBe(stored.Metadata);
        value.Freshness.ShouldBe(stored.Freshness);
        value.ResourceVersion.ShouldBe(JsonSerializer.Serialize(fixture.Native.LastResource, JsonSerializerOptions.Web));
        value.PermissionVersion.ShouldBe(JsonSerializer.Serialize(fixture.Native.LastTenant, JsonSerializerOptions.Web));
        value.AuthorizedAt.ShouldBe(fixture.Time.Now);
        fixture.Access.Checks.ShouldBe(3);
        fixture.Store.Count.ShouldBe(keys);
        fixture.Metadata.ShouldBe(stored);
        fixture.Guard.Reads.ShouldAllBe(type => type == typeof(ConversationResolutionMetadataReadModel)
            || type == typeof(ConversationResolutionMetadataProof) || type == typeof(ConversationProjectionDispatchLedger));
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
        result.GetPayload().GetProperty("metadata").EnumerateObject().Select(property => property.Name)
            .ShouldBe(["conversationId", "projectId", "safeLabel", "lifecycleState"]);
    }

    [Fact]
    public async Task AuthorizedAtIsTheFinalOwnerCheckInstantAndNeverReplacesNativeGenerationTime()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        DateTimeOffset generation = fixture.Metadata.Freshness.ProjectionGeneratedAt;
        fixture.Access.OnCheck = count => { if (count == 2) fixture.Time.Now = fixture.Time.Now.AddMinutes(1); };
        var result = Deserialize(await fixture.DispatchAsync());
        result.AuthorizedAt.ShouldBe(fixture.Time.Now);
        result.AuthorizedAt.ShouldNotBe(generation);
        result.Freshness!.ProjectionGeneratedAt.ShouldBe(generation);
    }

    /// <summary>Withholds future native evidence independently at each authorization or initial lookup boundary.</summary>
    /// <param name="sample">The one-based native permission sample to date in the future.</param>
    /// <returns>A task representing the metadata disclosure check.</returns>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task FutureNativeVerificationIsWithheldAtEachAuthorizationAndInitialLookupBoundary(int sample)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        fixture.Native.TenantVerificationTime = (read, now) => read == sample ? now.AddTicks(1) : now;

        AssertUnavailable(await fixture.DispatchAsync());

        fixture.Native.TenantReads.ShouldBe(sample);
        if (sample <= 2) fixture.Guard.Reads.ShouldBeEmpty();
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    /// <summary>Validates the native verification instant sampled after the final resource read.</summary>
    /// <param name="offsetTicks">The final verification offset from the owner clock.</param>
    /// <param name="available">Whether current metadata may be disclosed.</param>
    /// <returns>A task representing the final sample check.</returns>
    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task FinalNativeVerificationMustNotBeInTheFutureAfterTheLastProjectionRead(int offsetTicks, bool available)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ConversationResolutionMetadataReadModel stored = fixture.Metadata;
        int keys = fixture.Store.Count;
        DateTimeOffset finalVerification = fixture.Time.Now.AddTicks(offsetTicks);
        fixture.Native.OnProjection = read =>
        {
            if (read == 2) fixture.Native.TenantVerificationTime = (_, _) => finalVerification;
        };

        QueryResult result = await fixture.DispatchAsync();

        fixture.Native.ProjectionReads.ShouldBe(2);
        if (available)
        {
            var value = Deserialize(result);
            value.Metadata.ShouldBe(stored.Metadata);
            value.Freshness.ShouldBe(stored.Freshness);
            value.PermissionVersion.ShouldBe(JsonSerializer.Serialize(fixture.Native.LastTenant, JsonSerializerOptions.Web));
            value.AuthorizedAt.ShouldBe(finalVerification);
        }
        else
        {
            AssertUnavailable(result);
        }
        fixture.Store.Count.ShouldBe(keys);
        fixture.Metadata.ShouldBe(stored);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    /// <summary>Allows a current final sample after the clock advances during resource I/O.</summary>
    /// <returns>A task representing the current-clock and immutable-generation checks.</returns>
    [Fact]
    public async Task FinalNativeVerificationUsesTheClockAfterResourceIoWithoutChangingProjectionGenerationTime()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        DateTimeOffset generation = fixture.Metadata.Freshness.ProjectionGeneratedAt;
        DateTimeOffset started = fixture.Time.Now;
        fixture.Native.OnProjection = read =>
        {
            if (read == 2) fixture.Time.Now = fixture.Time.Now.AddMinutes(1);
        };

        var value = Deserialize(await fixture.DispatchAsync());

        value.Metadata.ShouldBe(fixture.Metadata.Metadata);
        value.AuthorizedAt.ShouldBe(started.AddMinutes(1));
        value.Freshness!.ProjectionGeneratedAt.ShouldBe(generation);
    }

    [Fact]
    public async Task SupportedAggregateLength256IsPreservedExactly()
    {
        ResolutionMetadataFixture fixture = new();
        var conversation = new Hexalith.Conversations.Contracts.Identifiers.ConversationId(new string('x', 256));
        (await fixture.Handler().ProjectAsync(ResolutionMetadataFixture.Request(conversationId: conversation),
            "dispatch-long", TestContext.Current!.CancellationToken)).Status.ShouldBe(Hexalith.EventStore.Contracts.Projections.ProjectionDispatchStatus.Completed);
        QueryEnvelope query = ResolutionMetadataFixture.Query() with { AggregateId = conversation.Value, EntityId = conversation.Value };
        Deserialize(await fixture.DispatchAsync(query)).Metadata!.ConversationId.ShouldBe(conversation.Value);
    }

    [Fact]
    public async Task NativeEvidenceChangingInsideThePolicyCheckWithholdsBeforeLookup()
    {
        ResolutionMetadataFixture fixture = new();
        fixture.Access.OnCheck = _ => fixture.Access.Health = new(Version: 8, Watermark: "changed-during-check");
        AssertUnavailable(await fixture.DispatchAsync());
        fixture.Guard.Reads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("unverified")]
    [InlineData("origin-context")]
    [InlineData("origin-kind")]
    [InlineData("workload")]
    [InlineData("actor")]
    [InlineData("delegation")]
    [InlineData("scopes")]
    [InlineData("audience")]
    [InlineData("payload")]
    [InlineData("purpose")]
    [InlineData("null-payload")]
    [InlineData("entity")]
    [InlineData("paging")]
    [InlineData("admin-override")]
    [InlineData("route-case")]
    public async Task UnsupportedAuthorityAndPurposeDenyBeforeLookup(string scenario)
    {
        ResolutionMetadataFixture fixture = new();
        QueryEnvelope query = ResolutionMetadataFixture.Query();
        var origin = query.CallerAuthority!.OriginatingContext!;
        query = scenario switch
        {
            "origin-context" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = null } },
            "origin-kind" => query with { CallerAuthority = query.CallerAuthority with { Origin = "authenticated-caller", OriginatingContext = null } },
            "workload" => query with { AuthenticatedWorkloadId = "other" },
            "actor" => query with { OriginalActorId = "another" },
            "delegation" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = origin with { IsDelegated = true, DelegationId = null } } },
            "scopes" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = origin with { Scopes = ["projects.write"] } } },
            "audience" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = origin with { Audience = ["other"] } } },
            "payload" => query with { Payload = "[]"u8.ToArray() },
            "purpose" => query with { Payload = "{\"purpose\":\"inspect\"}"u8.ToArray() },
            "null-payload" => query with { Payload = null! },
            "entity" => query with { EntityId = "other" },
            "paging" => query with { Paging = new(10) },
            "admin-override" => query with { IsGlobalAdmin = true },
            "route-case" => query with { Domain = "Conversations" },
            _ => query,
        };
        QueryResult result = await fixture.DispatchAsync(query, verified: scenario != "unverified");
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        fixture.Guard.Reads.ShouldBeEmpty();
        fixture.Access.Checks.ShouldBe(0);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OmittedCollectionsAndValidOriginalDelegationAreCompatible(bool empty)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        QueryEnvelope query = ResolutionMetadataFixture.Query();
        query = query with { EntityId = null, CallerAuthority = query.CallerAuthority! with
        { OriginatingContext = query.CallerAuthority.OriginatingContext! with
            { IsDelegated = true, DelegationId = "original-delegation", AuthenticatedWorkloadId = empty ? null : "original-client",
                Scopes = empty ? [] : null, Audience = empty ? [] : null } } };
        Deserialize(await fixture.DispatchAsync(query)).Metadata.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("denied")]
    [InlineData("absent")]
    [InlineData("cross-tenant")]
    public async Task DeniedAbsentAndCrossTenantAreIndistinguishable(string scenario)
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        QueryEnvelope query = ResolutionMetadataFixture.Query();
        if (scenario == "denied") fixture.Access.Denied = true;
        if (scenario == "absent") query = query with { AggregateId = "missing", EntityId = "missing" };
        if (scenario == "cross-tenant") query = query with { TenantId = "other-tenant" };
        QueryResult result = await fixture.DispatchAsync(query);
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("safe-denial");
        if (scenario == "denied") fixture.Guard.Reads.ShouldBeEmpty();
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("missing-signal")]
    [InlineData("missing-decision")]
    [InlineData("stale")]
    [InlineData("gap")]
    [InlineData("rollback")]
    [InlineData("poison")]
    [InlineData("malformed")]
    public async Task NonCurrentNativePermissionEvidenceWithholdsBeforeLookup(string scenario)
    {
        ResolutionMetadataFixture fixture = new();
        fixture.Access.Health = scenario switch
        {
            "missing-signal" => ConversationTenantProjectionHealth.Healthy,
            "stale" => fixture.Access.Health with { IsStale = true },
            "gap" => fixture.Access.Health with { HasGap = true },
            "rollback" => fixture.Access.Health with { HasRollback = true },
            "poison" => fixture.Access.Health with { IsPoisoned = true },
            "malformed" => fixture.Access.Health with { Watermark = "" },
            _ => fixture.Access.Health,
        };
        fixture.Access.MissingDecisionEvidence = scenario == "missing-decision";
        AssertUnavailable(await fixture.DispatchAsync());
        fixture.Guard.Reads.ShouldBeEmpty();
    }

    [Fact]
    public async Task RealAccessServicePreservesOnlyCheckedHealthAndStillRequiresNativeEvidence()
    {
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        ResolutionTenantStore tenantStore = new();
        var defaultSignal = new DefaultConversationTenantProjectionSignal(tenantStore, NullLogger<DefaultConversationTenantProjectionSignal>.Instance);
        var realAccess = new ConversationTenantAccessService(tenantStore, defaultSignal, NullLogger<ConversationTenantAccessService>.Instance);
        var unsupported = await realAccess.CheckAccessAsync(ConversationTenantAccessRequirement.Read,
            ResolutionMetadataFixture.Tenant, "actor", cancellationToken: TestContext.Current.CancellationToken);
        unsupported.IsAllowed.ShouldBeTrue();
        unsupported.ProjectionVersion.ShouldBeNull(); unsupported.ProjectionWatermark.ShouldBeNull();
        AssertUnavailable(await fixture.DispatchAsync(access: realAccess, signal: defaultSignal));
        fixture.Guard.Reads.ShouldBeEmpty();

        // A real decision preserves the exact health checked by its evaluator; it cannot manufacture native proof.
        var currentSignalAccess = new ConversationTenantAccessService(tenantStore, fixture.Access, NullLogger<ConversationTenantAccessService>.Instance);
        var allowed = await currentSignalAccess.CheckAccessAsync(ConversationTenantAccessRequirement.Read,
            ResolutionMetadataFixture.Tenant, "actor", cancellationToken: TestContext.Current.CancellationToken);
        allowed.IsAllowed.ShouldBeTrue();
        allowed.ProjectionVersion.ShouldBe(fixture.Access.Health.Version);
        allowed.ProjectionWatermark.ShouldBe(fixture.Access.Health.Watermark);
        fixture.Native.Available = false;
        AssertUnavailable(await fixture.DispatchAsync(access: currentSignalAccess));
        fixture.Guard.Reads.ShouldBeEmpty();
        fixture.Native.Available = true;
        var current = Deserialize(await fixture.DispatchAsync(access: currentSignalAccess));
        current.Metadata.ShouldBe(fixture.Metadata.Metadata);
        current.ResourceVersion.ShouldBe(JsonSerializer.Serialize(fixture.Native.LastResource, JsonSerializerOptions.Web));
        current.PermissionVersion.ShouldBe(JsonSerializer.Serialize(fixture.Native.LastTenant, JsonSerializerOptions.Web));
    }

    [Fact]
    public async Task LocalHealthWithPreservedPermissionEvidenceCannotReplaceNativeAuthorization()
    {
        ResolutionMetadataFixture fixture = new(); await fixture.ProjectAsync();
        var decision = await fixture.Access.CheckAccessAsync(ConversationTenantAccessRequirement.Read,
            ResolutionMetadataFixture.Tenant, "actor", ResolutionMetadataFixture.Tenant,
            aggregateTenantId: ResolutionMetadataFixture.Tenant, projectionTenantId: ResolutionMetadataFixture.Tenant,
            cancellationToken: TestContext.Current.CancellationToken);
        decision.IsAllowed.ShouldBeTrue(); decision.ProjectionVersion.ShouldNotBeNull(); decision.ProjectionWatermark.ShouldNotBeNull();
        AssertUnavailable(await fixture.DispatchAsync(nativeAuthorization: false));
        fixture.Guard.Reads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("revision-first-read")]
    [InlineData("revocation-first-read")]
    [InlineData("revision-final-read")]
    [InlineData("resource-final-read")]
    public async Task AuthorityAndGenerationMustRemainStableThroughEveryLookup(string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ResolutionMetadataFixture fixture = new();
        await fixture.ProjectAsync();
        int metadataReads = 0;
        fixture.Guard.AfterRead = (type, _) =>
        {
            if (type != typeof(ConversationResolutionMetadataReadModel)) return Task.CompletedTask;
            metadataReads++;
            if ((scenario.EndsWith("first-read", StringComparison.Ordinal) && metadataReads == 1)
                || (scenario.EndsWith("final-read", StringComparison.Ordinal) && metadataReads == 2))
            {
                if (scenario.StartsWith("revocation", StringComparison.Ordinal)) fixture.Access.Denied = true;
                else if (scenario.StartsWith("revision", StringComparison.Ordinal))
                    fixture.Access.Health = new(Version: 8, Watermark: "native-watermark-8");
                else fixture.Store.SeedRaw("statestore",
                    ConversationProjectionReadModelKeys.ResolutionProofKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
                    fixture.Proof with { Current = fixture.Proof.Current! with { IsPending = true } });
            }

            return Task.CompletedTask;
        };
        QueryResult result = await fixture.DispatchAsync();
        if (scenario.StartsWith("revocation", StringComparison.Ordinal)) result.ErrorMessage.ShouldBe("safe-denial");
        else AssertUnavailable(result);
        fixture.Guard.ForbiddenAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(160, true)]
    [InlineData(161, false)]
    public async Task LabelAbsenceAndExactBoundArePreservedWhileOverBoundScoringIsUnavailable(int? length, bool complete)
    {
        ResolutionMetadataFixture fixture = new();
        string? label = length.HasValue ? new('x', length.Value) : null;
        await fixture.ProjectAsync(label: label);
        fixture.Metadata.IsLabelComplete.ShouldBe(complete);
        fixture.Metadata.Metadata.SafeLabel.ShouldBe(complete ? label : null);
        QueryResult result = await fixture.DispatchAsync();
        if (complete) Deserialize(result).Metadata!.SafeLabel.ShouldBe(label);
        else AssertUnavailable(result);
    }

    [Fact]
    public async Task StaleProjectionAndPendingProofCannotDiscloseMembershipOrLabel()
    {
        ResolutionMetadataFixture fixture = new();
        fixture.Time.Now = ResolutionMetadataFixture.Started.AddMinutes(6);
        await fixture.ProjectAsync();
        AssertUnavailable(await fixture.DispatchAsync());
        fixture.Store.SeedRaw("statestore", ConversationProjectionReadModelKeys.ResolutionProofKey(
            ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation),
            fixture.Proof with { Current = fixture.Proof.Current! with { IsPending = true } });
        AssertUnavailable(await fixture.DispatchAsync());
    }

    [Fact]
    public async Task CancellationPropagates()
    {
        ResolutionMetadataFixture fixture = new();
        using CancellationTokenSource source = new();
        source.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => fixture.DispatchAsync(cancellationToken: source.Token));
    }

    private static ConversationResolutionMetadataResultV1 Deserialize(QueryResult result)
    {
        result.Success.ShouldBeTrue();
        return result.GetPayload().Deserialize<ConversationResolutionMetadataResultV1>(JsonSerializerOptions.Web)!;
    }

    private static void AssertUnavailable(QueryResult result)
        => Deserialize(result).ShouldBe(new ConversationResolutionMetadataResultV1(null, null, null, null, null));
}
