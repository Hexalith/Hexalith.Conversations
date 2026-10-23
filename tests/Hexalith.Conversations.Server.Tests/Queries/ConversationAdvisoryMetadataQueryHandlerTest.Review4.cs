using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Tests.ResolutionMetadata;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Projections;

namespace Hexalith.Conversations.Server.Tests.Queries;

public sealed partial class ConversationAdvisoryMetadataQueryHandlerTest
{
    [Theory]
    [InlineData("requirement")]
    [InlineData("tenant")]
    [InlineData("actor")]
    [InlineData("retryable")]
    public async Task FinalOrdinaryReadDecisionStillRequiresExactIdentityAndNonretryableSuccess(string mutation)
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        f.Access.OnCheck = n =>
        {
            if (n != 2) return;
            if (mutation == "retryable") { f.Access.RetryableDenial = true; return; }
            f.Access.MutateDecision = decision => mutation switch
            {
                "requirement" => decision with { Requirement = (ConversationTenantAccessRequirement)999 },
                "tenant" => decision with { TenantId = new("foreign") },
                _ => decision with { CallerPrincipalId = "other" },
            };
        };
        var result = await f.DispatchAdvisoryAsync();
        if (mutation == "retryable") { Payload(result).Metadata.ShouldBeNull(); Payload(result).Incomplete.ShouldBeTrue(); }
        else result.ErrorMessage.ShouldBe("safe-denial");
        f.Access.Checks.ShouldBe(2);
    }

    [Theory]
    [InlineData("origin-context")]
    [InlineData("origin-kind")]
    [InlineData("actor")]
    [InlineData("delegation")]
    [InlineData("scopes")]
    [InlineData("array")]
    [InlineData("nonempty")]
    [InlineData("null-payload")]
    [InlineData("oversize")]
    [InlineData("entity")]
    [InlineData("paging")]
    [InlineData("admin")]
    [InlineData("domain-case")]
    public async Task UnsupportedAdvisoryAuthorityDeniesBeforeAccessOrStorage(string mutation)
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        var query = ResolutionMetadataFixture.Query() with { QueryType = GetConversationAdvisoryMetadataQuery.QueryType };
        var origin = query.CallerAuthority!.OriginatingContext!;
        query = mutation switch
        {
            "origin-context" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = null } },
            "origin-kind" => query with { CallerAuthority = query.CallerAuthority with { Origin = "authenticated-caller", OriginatingContext = null } },
            "actor" => query with { OriginalActorId = "other" },
            "delegation" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = origin with { IsDelegated = true, DelegationId = null } } },
            "scopes" => query with { CallerAuthority = query.CallerAuthority with { OriginatingContext = origin with { Scopes = ["projects.write"] } } },
            "array" => query with { Payload = "[]"u8.ToArray() },
            "nonempty" => query with { Payload = "{\"purpose\":\"inspect\"}"u8.ToArray() },
            "null-payload" => query with { Payload = null! },
            "oversize" => query with { Payload = "{}"u8.ToArray().Concat(Enumerable.Repeat((byte)' ', 8191)).ToArray() },
            "entity" => query with { EntityId = "other" },
            "paging" => query with { Paging = new(10) },
            "admin" => query with { IsGlobalAdmin = true },
            _ => query with { Domain = "Conversations" },
        };
        (await f.DispatchAdvisoryAsync(query)).ErrorMessage.ShouldBe("safe-denial");
        f.Access.Checks.ShouldBe(0); f.Guard.Reads.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Unclassified label", false)]
    public async Task AbsenceOfBothLabelsIsCompleteWhileUnclassifiedTextIsIncomplete(string? label, bool complete)
    {
        var f = new ResolutionMetadataFixture();
        (await f.Handler().ProjectAsync(ResolutionMetadataFixture.Request(label: label, classifyLabel: false), "label-absence", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        f.Metadata.IsLabelComplete.ShouldBe(complete); f.Metadata.Metadata.SafeLabel.ShouldBeNull();
        f.Metadata.LabelPurpose.ShouldBe(complete ? ConversationResolutionScoringPolicy.Purpose : null);
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldNotBeNull(); result.Metadata.SafeLabel.ShouldBeNull(); result.Incomplete.ShouldBe(!complete);
    }

    [Fact]
    public async Task LaggingCommittedOwnerMetadataRemainsACompleteObservation()
    {
        var f = new ResolutionMetadataFixture(); await f.ProjectAsync();
        string key = ConversationProjectionReadModelKeys.ResolutionMetadataKey(ResolutionMetadataFixture.Tenant, ResolutionMetadataFixture.Conversation);
        var model = f.Metadata; var old = model.Freshness;
        f.Store.SeedRaw("statestore", key, model with { Freshness = new(old.ProjectionContractSchemaVersion, old.ProjectionCursor,
            old.LastAppliedEventPosition, old.LastAppliedEventTimestamp, old.ProjectionGeneratedAt, TimeSpan.FromMinutes(1), true,
            ProjectionTrustState.Stale, ProjectionFreshnessReasonCode.StaleThresholdExceeded) });
        var result = Payload(await f.DispatchAdvisoryAsync());
        result.Metadata.ShouldNotBeNull(); result.Incomplete.ShouldBeFalse(); f.Access.Checks.ShouldBe(2);
    }
}
