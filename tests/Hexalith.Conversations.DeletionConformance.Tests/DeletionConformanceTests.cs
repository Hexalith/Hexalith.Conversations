using System.Security.Claims;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.DeletionConformance;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Microsoft.Extensions.Options;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Persisted receipt folding, exact retries, isolation and fail-closed qualification component checks.</summary>
public sealed class DeletionConformanceTests
{
    private static DomainServiceAdmissionContext Context(CommandEnvelope envelope) => new(new DomainServiceRequest(envelope, null));

    /// <summary>Processing is not persistence: only complete independent receipt reads can return an acknowledgement.</summary>
    [Fact]
    public async Task GatewayAdmissionAndUnpersistedEventCannotAcknowledge()
    {
        var f = new ConformanceFixture(); f.Authenticate();
        var result = DeletionConformanceAggregate.Handle(f.Command, null);
        result.IsSuccess.ShouldBeTrue();
        var before = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        before.Outcome.ShouldBe(ConversationAgentsOutcome.Absent); before.Acknowledgement.ShouldBeNull();
        f.Persist(result.Events.Single());
        var after = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        after.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        after.Acknowledgement!.ProtectedDeletionRevision.ShouldBe(1);
        f.Stored.Values.Single().Events.Single().EventTypeName.ShouldBe(typeof(ConformanceDeletionAccepted).FullName);
        after.Acknowledgement.ShouldBe(JsonSerializer.Deserialize<ConformanceDeletionAccepted>(f.Stored.Values.Single().Events.Single().Payload, ConformanceFixture.Json)!.Acknowledgement);
    }

    /// <summary>A new attempt and a fresh service instance reuse the original serialized receipt.</summary>
    [Fact]
    public async Task RetryAndRestartPreserveOriginalReceiptAndSourceBinding()
    {
        var f = new ConformanceFixture(); f.Authenticate();
        f.Persist(DeletionConformanceAggregate.Handle(f.Command, null).Events.Single());
        var first = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        var state = await new DeletionConformanceStreams(f).ReadReceiptAsync(f.Query().AggregateIdentity, TestContext.Current.CancellationToken);
        DeletionConformanceAggregate.Handle(f.Command with { DeliveryAttemptId = "attempt-two" }, state).IsNoOp.ShouldBeTrue();
        var snapshot = JsonSerializer.Deserialize<DeletionConformanceState>(JsonSerializer.SerializeToUtf8Bytes(state, ConformanceFixture.Json), ConformanceFixture.Json)!;
        snapshot.Acceptance.ShouldBe(state!.Acceptance); snapshot.Revision.ShouldBe(1);
        (await new DeletionConformanceAggregate().ProcessAsync(f.Envelope(f.Command with { DeliveryAttemptId = "attempt-two" }),
            JsonSerializer.SerializeToElement(snapshot, ConformanceFixture.Json), TestContext.Current.CancellationToken)).IsNoOp.ShouldBeTrue();
        var retry = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(attempt: "attempt-two"), TestContext.Current.CancellationToken));
        retry.Acknowledgement.ShouldBe(first.Acknowledgement);
        state!.Acceptance!.Signal.ShouldBe(f.Signal);
        f.Stored.Values.Single().Events.Count.ShouldBe(1);
    }

    /// <summary>Rollover preserves the exact original receipt at an explicitly retained target.</summary>
    [Fact]
    public async Task RetainedTargetLookupAfterRolloverPreservesOriginalReceipt()
    {
        var f = new ConformanceFixture(); f.Authenticate();
        string originalTarget = f.Options.TargetVersion;
        f.Persist(DeletionConformanceAggregate.Handle(f.Command, null).Events.Single());
        var original = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        original.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        f.Options.TargetVersion = new string('b', 40);
        f.Options.RetainedTargetVersions = [originalTarget];

        var retained = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(
            f.Query(attempt: "rollover-attempt", target: originalTarget), TestContext.Current.CancellationToken));
        retained.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        retained.Acknowledgement.ShouldBe(original.Acknowledgement);
        retained.Acknowledgement!.TargetVersion.ShouldBe(originalTarget);
        retained.Acknowledgement.SourceRevision.ShouldBe(f.Signal.SourceRevision);
        retained.Acknowledgement.ProtectedDeletionRevision.ShouldBe(original.Acknowledgement!.ProtectedDeletionRevision);
        retained.Acknowledgement.Evidence.ShouldBe(original.Acknowledgement.Evidence);
        var current = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        current.Outcome.ShouldBe(ConversationAgentsOutcome.Conflict);
        current.Acknowledgement.ShouldBeNull();
        f.Stored.Values.Single().Events.Count.ShouldBe(1);
    }

    /// <summary>Changing any immutable source or target field at the same logical id produces conflict.</summary>
    [Theory]
    [InlineData("conversation")]
    [InlineData("revision")]
    [InlineData("approval")]
    [InlineData("target")]
    public async Task SameSignalWithChangedFieldsConflictsWithoutReplacement(string field)
    {
        var f = new ConformanceFixture(); f.Authenticate();
        f.Persist(DeletionConformanceAggregate.Handle(f.Command, null).Events.Single());
        var state = await f.Streams.ReadReceiptAsync(f.Query().AggregateIdentity, TestContext.Current.CancellationToken);
        var altered = field switch
        {
            "conversation" => f.Command with { Signal = f.Signal with { ConversationId = new("conv-two"), SourceStream = "tenant-one:conversation:conv-two" } },
            "revision" => f.Command with { Signal = f.Signal with { SourceRevision = 2 } },
            "approval" => f.Command with { Signal = f.Signal with { ApprovalReference = "changed-approval" } },
            _ => f.Command with { TargetVersion = new string('b', 40) },
        };
        var conflict = DeletionConformanceAggregate.Handle(altered, state);
        ((ConformanceDeletionRejected)conflict.Events.Single()).Outcome.ShouldBe(ConversationAgentsOutcome.Conflict);
        f.Persist(conflict.Events.Single());
        var replay = await f.Streams.ReadReceiptAsync(f.Query().AggregateIdentity, TestContext.Current.CancellationToken);
        replay!.Revision.ShouldBe(2); replay.Acceptance.ShouldBe(state!.Acceptance);
        var original = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        original.Acknowledgement!.ProtectedDeletionRevision.ShouldBe(1);
        if (field != "target")
        {
            var result = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(altered.Signal), TestContext.Current.CancellationToken));
            result.Outcome.ShouldBe(ConversationAgentsOutcome.Conflict); result.Acknowledgement.ShouldBeNull();
        }
    }

    /// <summary>Incomplete, gapped, foreign, unknown or forged receipt histories never confirm acceptance.</summary>
    [Theory]
    [InlineData("gap")]
    [InlineData("head")]
    [InlineData("foreign")]
    [InlineData("revision")]
    [InlineData("evidence")]
    [InlineData("unknown")]
    public async Task UnconfirmedPersistenceNeverReturnsReceipt(string fault)
    {
        var f = new ConformanceFixture(); f.Authenticate();
        var accepted = (ConformanceDeletionAccepted)DeletionConformanceAggregate.Handle(f.Command, null).Events.Single();
        if (fault == "revision") accepted = accepted with { Acknowledgement = accepted.Acknowledgement with { ProtectedDeletionRevision = 9 } };
        if (fault == "evidence") accepted = accepted with { Acknowledgement = accepted.Acknowledgement with { Evidence = "forged" } };
        f.Persist(accepted);
        var key = f.Stored.Keys.Single(); var source = f.Stored[key]; var item = source.Events.Single();
        f.Stored[key] = fault switch
        {
            "gap" => source with { Events = [item with { SequenceNumber = 2 }] },
            "head" => source with { Head = 2 },
            "foreign" => source with { Identity = new("tenant-two", key.Domain, key.AggregateId) },
            "unknown" => source with { Events = [item with { EventTypeName = "UnknownReceipt" }] },
            _ => source,
        };
        var result = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable); result.Acknowledgement.ShouldBeNull();
    }

    /// <summary>Absent authentication, the production Agents workload, wrong operations and tenants disclose no receipt.</summary>
    [Theory]
    [InlineData("anonymous")]
    [InlineData("agents")]
    [InlineData("operation")]
    [InlineData("tenant")]
    [InlineData("target")]
    [InlineData("binding")]
    public async Task ReceiptIsolationDeniesBeforeReading(string vector)
    {
        var f = new ConformanceFixture();
        if (vector != "anonymous") f.Authenticate(vector == "agents" ? "agents" : null,
            vector == "operation" ? EventStoreWorkloadOperations.DomainServiceProcess : EventStoreWorkloadOperations.DomainServiceQuery,
            vector != "binding");
        var query = vector == "tenant" ? f.Query(f.Signal with { TenantId = new("tenant-two"), SourceStream = "tenant-two:conversation:conv-one" })
            : vector == "target" ? f.Query(target: new string('b', 40)) : f.Query();
        var result = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(query, TestContext.Current.CancellationToken));
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Denied); result.Acknowledgement.ShouldBeNull(); f.Reads.ShouldBe(0);
    }

    /// <summary>Only independently persisted exact source approval and latest attempt can pass command admission.</summary>
    [Fact]
    public async Task ExactAuthenticatedSourceAndAttemptAreRequired()
    {
        var f = new ConformanceFixture(); f.Authenticate(f.Options.GatewayWorkloadId, EventStoreWorkloadOperations.DomainServiceProcess);
        (await f.Admission.EvaluateAsync(Context(f.Envelope()), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        f.PersistSource();
        (await f.Admission.EvaluateAsync(Context(f.Envelope()), TestContext.Current.CancellationToken)).IsAccepted.ShouldBeTrue();
        (await f.Admission.EvaluateAsync(Context(f.Envelope(f.Command with { DeliveryAttemptId = "different-attempt" })), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        var changed = f.Command with { Signal = f.Signal with { ApprovalReference = "substituted" } };
        var denied = await f.Admission.EvaluateAsync(Context(f.Envelope(changed)), TestContext.Current.CancellationToken);
        ((ConformanceDeletionRejected)denied.RejectionEvents.Single()).Outcome.ShouldBe(ConversationAgentsOutcome.Conflict);
        f.PersistSource(ConversationDeletionDeliveryAction.Quarantine);
        (await f.Admission.EvaluateAsync(Context(f.Envelope()), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        f.Stored.Keys.ShouldNotContain(f.Query().AggregateIdentity);
    }

    /// <summary>Command admission refuses authentication, actor, domain, operation, tenant and target substitution.</summary>
    [Theory]
    [InlineData("anonymous")]
    [InlineData("agents")]
    [InlineData("operation")]
    [InlineData("actor")]
    [InlineData("domain")]
    [InlineData("target")]
    public async Task CommandIsolationDeniesBeforeSourceRead(string vector)
    {
        var f = new ConformanceFixture();
        if (vector != "anonymous") f.Authenticate(vector == "agents" ? "agents" : f.Options.GatewayWorkloadId,
            vector == "operation" ? EventStoreWorkloadOperations.DomainServiceQuery : EventStoreWorkloadOperations.DomainServiceProcess);
        var envelope = vector switch
        {
            "actor" => f.Envelope() with { UserId = "imposter" },
            "domain" => f.Envelope() with { Domain = "agents" },
            "target" => f.Envelope(f.Command with { TargetVersion = new string('b', 40) }),
            _ => f.Envelope(),
        };
        var result = await f.Admission.EvaluateAsync(Context(envelope), TestContext.Current.CancellationToken);
        result.IsRejected.ShouldBeTrue(); ((ConformanceDeletionRejected)result.RejectionEvents.Single()).Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        f.Reads.ShouldBe(0); f.Stored.ShouldBeEmpty();
    }

    /// <summary>Qualification scope is fixed by the receiver and never taken from query payload or caller expectations.</summary>
    [Fact]
    public async Task AuthenticatedIdentityHasFixedLevelFourScope()
    {
        var f = new ConformanceFixture(); f.Authenticate();
        var handler = new DeletionConformanceIdentityQuery(Options.Create(f.Options), f.Authorization);
        var query = new QueryEnvelope(f.Options.TenantId, handler.Domain, "identity", handler.QueryType, [], "identity-correlation", f.Options.LookupPrincipalId);
        var result = await handler.ExecuteAsync(query, TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(result.PayloadBytes!);
        json.RootElement.GetProperty("receiverKind").GetString().ShouldBe("ContractConformance");
        json.RootElement.GetProperty("qualificationScope").GetString().ShouldBe("OQ-5.4-1");
        json.RootElement.GetProperty("evidenceLevel").GetInt32().ShouldBe(4);
        json.RootElement.GetProperty("agentsRuntimeEnabled").GetBoolean().ShouldBeFalse();
        (await handler.ExecuteAsync(query with { Payload = "{}"u8.ToArray() }, TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
        f.Authenticate("agents");
        (await handler.ExecuteAsync(query, TestContext.Current.CancellationToken)).Success.ShouldBeFalse();
    }

    /// <summary>Missing enrollment and shared identities fail closed even if a caller forges a matching envelope.</summary>
    [Fact]
    public async Task MissingOrSharedEnrollmentNeverEnablesService()
    {
        new DeletionConformanceOptions().IsConfigured().ShouldBeFalse();
        var f = new ConformanceFixture(); f.Options.IsConfigured().ShouldBeTrue();
        f.Options.ReceiverPrincipalId = f.Options.LookupPrincipalId;
        f.Options.IsConfigured().ShouldBeFalse(); f.Authenticate();
        var result = ConformanceFixture.Result(await f.Lookup.ExecuteAsync(f.Query(), TestContext.Current.CancellationToken));
        result.Outcome.ShouldBe(ConversationAgentsOutcome.Denied); f.Reads.ShouldBe(0);
    }

    /// <summary>Cancelled lookups preserve caller cancellation and release no receipt.</summary>
    [Fact]
    public async Task CallerCancellationPreventsReceiptRelease()
    {
        var f = new ConformanceFixture(); f.Authenticate(); using var caller = new CancellationTokenSource(); caller.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => f.Lookup.ExecuteAsync(f.Query(), caller.Token));
        exception.CancellationToken.ShouldBe(caller.Token); f.Reads.ShouldBe(0);
    }
}
