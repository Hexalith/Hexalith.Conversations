// <copyright file="ConformanceConversationDeletionReceiverTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using S = Hexalith.Conversations.Server.Tests.Agents.DeletionRuntimeTestSettings;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Receiver adapter matrix against a stub sidecar, a fake lookup issuer and a recording gateway; no installed receiver.</summary>
public sealed class ConformanceConversationDeletionReceiverTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly RecordingGateway _gateway = new();
    private readonly FakeLookupIssuer _issuer = new();
    private readonly RecordingLogger<ConformanceConversationDeletionReceiver> _logger = new();
    private Func<QueryEnvelope, HttpResponseMessage> _sidecar = static _ => new HttpResponseMessage(HttpStatusCode.NotFound);
    private StubHttpHandler? _handler;

    /// <summary>A gateway 202 is admission only: submission never returns Available and sends the exact mirrored command.</summary>
    [Fact]
    public async Task SubmissionIsAdmissionOnlyAndSendsTheExactConformanceCommand()
    {
        var signal = S.Signal();
        ConformanceConversationDeletionReceiver receiver = Create();

        ConversationDeletionReceiverResult result = await receiver.SubmitAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken);

        result.ShouldBe(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Unavailable));
        var command = _gateway.Commands.ShouldHaveSingleItem();
        command.Tenant.ShouldBe(signal.TenantId.Value);
        command.Domain.ShouldBe("conversation-deletion-conformance");
        command.AggregateId.ShouldBe(signal.ConversationDeletionSignalId);
        command.CommandType.ShouldBe("Hexalith.Conversations.DeletionConformance.AcceptConformanceDeletion");
        command.IdempotencyKey.ShouldBe(command.MessageId);
        command.Payload.EnumerateObject().Select(static property => property.Name).ShouldBe(["signal", "deliveryAttemptId", "targetVersion"]);
        command.Payload.Deserialize<ToolConformanceDeletion>(Web).ShouldBe(new ToolConformanceDeletion(signal, "attempt-1", S.Target));

        _ = await receiver.SubmitAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken);
        _gateway.Commands[1].MessageId.ShouldBe(command.MessageId, "an exact retry reuses one logical submission identity");
        _handler!.Requests.ShouldBeEmpty("submission never reads the receiver");
    }

    /// <summary>Gateway transport failures are unavailable, never an acknowledgement or conflict.</summary>
    [Fact]
    public async Task SubmissionTransportFailureIsUnavailable()
    {
        foreach (Exception failure in new Exception[] { new HttpRequestException("lost"), new EventStoreGatewayException(503, "Unavailable"),
            new InvalidOperationException("token") })
        {
            _gateway.Failure = failure;
            (await Create().SubmitAsync(S.Signal(), "attempt-1", S.Target, TestContext.Current.CancellationToken)).Outcome
                .ShouldBe(ConversationAgentsOutcome.Unavailable);
        }
    }

    /// <summary>Unaccepted targets, foreign tenants and malformed attempts never reach the gateway or the sidecar.</summary>
    [Fact]
    public async Task InadmissibleRequestsAreUnavailableWithoutTransport()
    {
        ConformanceConversationDeletionReceiver receiver = Create();
        var foreign = S.Signal() with { TenantId = new TenantId("tenant-beta") };
        (await receiver.SubmitAsync(S.Signal(), "attempt-1", new string('a', 40), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.SubmitAsync(foreign, "attempt-1", S.Target, TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.LookupAsync(S.Signal(), " ", S.Target, TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.LookupAsync(S.Signal(), "attempt-1", S.Target.ToUpperInvariant(), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.CurrentTargetAsync(new TenantId("tenant-beta"), TestContext.Current.CancellationToken)).ShouldBeNull();
        _gateway.Commands.ShouldBeEmpty();
        _handler!.Requests.ShouldBeEmpty();
    }

    /// <summary>An exact independently looked-up acknowledgement is the only Available result, sent through this host's sidecar.</summary>
    [Fact]
    public async Task ExactLookupAcknowledgementIsAvailableThroughTheSidecar()
    {
        var signal = S.Signal();
        var receipt = Receipt(signal, S.Target);
        _sidecar = _ => Result(new { outcome = 0, acknowledgement = receipt });

        ConversationDeletionReceiverResult result = await Create().LookupAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken);

        result.ShouldBe(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt));
        RecordedRequest request = _handler!.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ShouldBe(new Uri(S.Sidecar + "/v1.0/invoke/" + S.Receiver + "/method/query"));
        request.SidecarToken.ShouldBe(S.SidecarToken);
        request.Authorization.ShouldBeNull("the lookup never carries a public Bearer or the worker identity");
        request.Assertion.ShouldNotBeNullOrWhiteSpace();
        QueryEnvelope envelope = JsonSerializer.Deserialize<QueryEnvelope>(request.Body, Web)!;
        envelope.TenantId.ShouldBe(signal.TenantId.Value);
        envelope.Domain.ShouldBe("conversation-deletion-conformance");
        envelope.AggregateId.ShouldBe(signal.ConversationDeletionSignalId);
        envelope.QueryType.ShouldBe("deletion-conformance-receipt");
        envelope.UserId.ShouldBe(S.Lookup);
        envelope.IsGlobalAdmin.ShouldBeFalse();
        envelope.IsDelegated.ShouldBeFalse();
        envelope.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        JsonSerializer.Deserialize<ToolConformanceDeletion>(envelope.Payload, Web).ShouldBe(new ToolConformanceDeletion(signal, "attempt-1", S.Target));
        WorkloadAssertionRequest issued = _issuer.Requests.ShouldHaveSingleItem();
        issued.Audience.ShouldBe(S.Receiver);
        issued.Operation.ShouldBe(EventStoreWorkloadOperations.DomainServiceQuery);
        issued.Bindings.ShouldNotBeNull().ShouldBe(new Dictionary<string, string>
        {
            [EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] = signal.TenantId.Value,
            [EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] = "conversation-deletion-conformance",
        }, ignoreOrder: true);
    }

    /// <summary>The receiver outcome may be an enum name as well as a number.</summary>
    [Fact]
    public async Task OutcomeNameIsAccepted()
    {
        var signal = S.Signal();
        var receipt = Receipt(signal, S.Target);
        _sidecar = _ => Result(new { outcome = "Available", acknowledgement = receipt });
        (await Create().LookupAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken))
            .ShouldBe(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt));
    }

    /// <summary>Authoritative absence and conflict pass through without an acknowledgement.</summary>
    [Theory]
    [InlineData(1, ConversationAgentsOutcome.Absent)]
    [InlineData(6, ConversationAgentsOutcome.Conflict)]
    [InlineData(4, ConversationAgentsOutcome.Unavailable)]
    [InlineData(5, ConversationAgentsOutcome.Unavailable)]
    [InlineData(7, ConversationAgentsOutcome.Unavailable)]
    public async Task NegativeOutcomesAreClassified(int outcome, ConversationAgentsOutcome expected)
    {
        _sidecar = _ => Result(new { outcome, acknowledgement = (ConversationDeletionAcknowledgement?)null });
        (await Create().LookupAsync(S.Signal(), "attempt-1", S.Target, TestContext.Current.CancellationToken))
            .ShouldBe(new ConversationDeletionReceiverResult(expected));
    }

    /// <summary>Malformed, failed, unknown-outcome and ambiguous receiver answers are Unavailable.</summary>
    [Theory]
    [InlineData("failed")]
    [InlineData("not-json")]
    [InlineData("empty-payload")]
    [InlineData("unknown-number")]
    [InlineData("unknown-name")]
    [InlineData("numeric-name")]
    [InlineData("duplicate-outcome")]
    [InlineData("case-variant-outcome")]
    [InlineData("extra-field")]
    [InlineData("available-without-receipt")]
    [InlineData("absent-with-receipt")]
    [InlineData("conflict-with-receipt")]
    [InlineData("receipt-extra-field")]
    [InlineData("receipt-missing-field")]
    [InlineData("receipt-text-revision")]
    [InlineData("duplicate-receipt-field")]
    [InlineData("http-500")]
    [InlineData("http-302")]
    [InlineData("http-202")]
    [InlineData("oversized")]
    public async Task UntrustworthyLookupAnswersAreUnavailable(string fault)
    {
        var signal = S.Signal();
        var receipt = Receipt(signal, S.Target);
        string receiptJson = JsonSerializer.Serialize(receipt, Web);
        _sidecar = _ => fault switch
        {
            "failed" => Raw(JsonSerializer.Serialize(QueryResult.Failure("Denied"), Web)),
            "not-json" => Raw("{"),
            "empty-payload" => Raw("{\"success\":true,\"payloadBytes\":null}"),
            "unknown-number" => Result(new { outcome = 99 }),
            "unknown-name" => Result(new { outcome = "Delivered" }),
            "numeric-name" => Result(new { outcome = "0", acknowledgement = receipt }),
            "duplicate-outcome" => Payload("{\"outcome\":1,\"outcome\":0,\"acknowledgement\":" + receiptJson + "}"),
            "case-variant-outcome" => Payload("{\"outcome\":0,\"Outcome\":1,\"acknowledgement\":" + receiptJson + "}"),
            "extra-field" => Payload("{\"outcome\":0,\"acknowledgement\":" + receiptJson + ",\"note\":\"x\"}"),
            "available-without-receipt" => Result(new { outcome = 0 }),
            "absent-with-receipt" => Result(new { outcome = 1, acknowledgement = receipt }),
            "conflict-with-receipt" => Result(new { outcome = 6, acknowledgement = receipt }),
            "receipt-extra-field" => Payload("{\"outcome\":0,\"acknowledgement\":" + receiptJson[..^1] + ",\"note\":\"x\"}}"),
            "receipt-missing-field" => Payload("{\"outcome\":0,\"acknowledgement\":{\"signalId\":\"" + signal.ConversationDeletionSignalId
                + "\",\"sourceRevision\":3,\"targetVersion\":\"" + S.Target + "\",\"evidence\":\"" + receipt.Evidence + "\"}}"),
            "receipt-text-revision" => Payload("{\"outcome\":0,\"acknowledgement\":" + receiptJson.Replace("\"protectedDeletionRevision\":19",
                "\"protectedDeletionRevision\":\"19\"", StringComparison.Ordinal) + "}"),
            "duplicate-receipt-field" => Payload("{\"outcome\":0,\"acknowledgement\":" + receiptJson[..^1] + ",\"SignalId\":\"x\"}}"),
            "http-500" => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            "http-302" => new HttpResponseMessage(HttpStatusCode.Found) { Headers = { Location = new Uri("https://elsewhere.test/query") } },
            "http-202" => Result(new { outcome = 0, acknowledgement = receipt }, HttpStatusCode.Accepted),
            "oversized" => Result(new { outcome = 0, acknowledgement = receipt with { Evidence = new string('a', 70_000) } }),
            _ => throw new ArgumentOutOfRangeException(nameof(fault)),
        };

        (await Create().LookupAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable, fault);
    }

    /// <summary>A well-formed authenticated receipt whose same-id fields changed is a conflict, so the pump quarantines it.</summary>
    [Theory]
    [InlineData("other-signal")]
    [InlineData("other-revision")]
    [InlineData("other-target")]
    [InlineData("unpersisted-receipt")]
    [InlineData("foreign-evidence")]
    public async Task WellFormedMismatchedReceiptIsConflict(string mismatch)
    {
        var signal = S.Signal();
        var receipt = Receipt(signal, S.Target);
        _sidecar = _ => Result(new
        {
            outcome = 0,
            acknowledgement = mismatch switch
            {
                "other-signal" => receipt with { SignalId = "signal-beta" },
                "other-revision" => receipt with { SourceRevision = 4 },
                "other-target" => receipt with { TargetVersion = S.RolloverTarget },
                "unpersisted-receipt" => receipt with { ProtectedDeletionRevision = 0 },
                "foreign-evidence" => receipt with { Evidence = "synthetic-receipt" },
                _ => throw new ArgumentOutOfRangeException(nameof(mismatch)),
            },
        });

        (await Create().LookupAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken))
            .ShouldBe(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Conflict), mismatch);
        _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Error && entry.Message.EndsWith("receipt-conflict", StringComparison.Ordinal));
    }

    /// <summary>Invalid UTF-8 text in a receiver answer is malformed, never an escaping exception.</summary>
    [Fact]
    public async Task InvalidUtf8TextIsMalformed()
    {
        byte[] receipt = [.. "{\"outcome\":\""u8, 0xC3, 0x28, .. "\"}"u8];
        byte[] identity = [.. "{\"schemaVersion\":1,\"receiverKind\":\""u8, 0xC3, 0x28, .. ("\",\"qualificationScope\":\"OQ-5.4-1\","
            + "\"evidenceLevel\":4,\"agentsRuntimeEnabled\":false,\"tenantId\":\"tenant-alpha\",\"targetVersion\":\"" + S.Target
            + "\",\"storageIdentity\":\"s\",\"receiverPrincipalId\":\"" + S.Receiver + "\",\"lookupPrincipalId\":\"" + S.Lookup + "\"}")
            .Select(static character => (byte)character)];
        _sidecar = envelope => Raw(JsonSerializer.Serialize(new QueryResult(true,
            envelope.QueryType == "deletion-conformance-identity" ? identity : receipt), Web));
        ConformanceConversationDeletionReceiver receiver = Create();

        (await receiver.LookupAsync(S.Signal(), "attempt-1", S.Target, TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.CurrentTargetAsync(S.Signal().TenantId, TestContext.Current.CancellationToken)).ShouldBeNull();
        _logger.Entries.Count(static entry => entry.Message.EndsWith("payload-malformed", StringComparison.Ordinal)).ShouldBe(2);
    }

    /// <summary>A remote sidecar is accepted only over HTTPS.</summary>
    [Fact]
    public async Task RemoteHttpsSidecarIsAccepted()
    {
        _sidecar = _ => Identity();
        var settings = new Dictionary<string, string?> { ["DAPR_HTTP_ENDPOINT"] = "https://sidecar.example", ["DAPR_API_TOKEN"] = S.SidecarToken };
        (await Create(settings).CurrentTargetAsync(S.Signal().TenantId, TestContext.Current.CancellationToken)).ShouldBe(S.Target);
        _handler!.Requests.ShouldHaveSingleItem().Uri.ShouldBe(new Uri("https://sidecar.example/v1.0/invoke/" + S.Receiver + "/method/query"));
    }

    /// <summary>Lookups require a binding-capable separate issuer, exact bound claims and the sidecar channel credential.</summary>
    [Theory]
    [InlineData("cannot-bind", "issuer-cannot-bind")]
    [InlineData("no-assertion", "assertion-unavailable")]
    [InlineData("unbound-assertion", "assertion-unbound")]
    [InlineData("other-workload", "assertion-unbound")]
    [InlineData("no-sidecar-token", "sidecar-token-missing")]
    [InlineData("no-sidecar", "sidecar-unconfigured")]
    [InlineData("remote-http-sidecar", "sidecar-unconfigured")]
    [InlineData("sidecar-path", "sidecar-unconfigured")]
    [InlineData("sidecar-query", "sidecar-unconfigured")]
    public async Task LookupCredentialsComeOnlyFromTheSeparateIssuerAndSidecar(string missing, string reason)
    {
        var signal = S.Signal();
        _sidecar = _ => Result(new { outcome = 0, acknowledgement = Receipt(signal, S.Target) });
        var settings = new Dictionary<string, string?> { ["DAPR_HTTP_ENDPOINT"] = S.Sidecar, ["DAPR_API_TOKEN"] = S.SidecarToken };
        FakeLookupIssuer issuer = missing == "other-workload" ? new FakeLookupIssuer(S.Worker) : _issuer;
        switch (missing)
        {
            case "cannot-bind": issuer.CanBindResources = false; break;
            case "no-assertion": issuer.ReturnNothing = true; break;
            case "unbound-assertion": issuer.OmitBindings = true; break;
            case "no-sidecar-token": settings.Remove("DAPR_API_TOKEN"); break;
            case "no-sidecar": settings.Remove("DAPR_HTTP_ENDPOINT"); break;
            case "remote-http-sidecar": settings["DAPR_HTTP_ENDPOINT"] = "http://sidecar.example:3500"; break;
            case "sidecar-path": settings["DAPR_HTTP_ENDPOINT"] = S.Sidecar + "/prefix"; break;
            case "sidecar-query": settings["DAPR_HTTP_ENDPOINT"] = S.Sidecar + "/?target=x"; break;
        }

        ConformanceConversationDeletionReceiver receiver = Create(settings, issuer);
        (await receiver.LookupAsync(signal, "attempt-1", S.Target, TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        (await receiver.CurrentTargetAsync(signal.TenantId, TestContext.Current.CancellationToken)).ShouldBeNull();
        _handler!.Requests.ShouldBeEmpty();
        _logger.Entries.Count(entry => entry.Message.EndsWith(": " + reason, StringComparison.Ordinal)).ShouldBe(2, missing);
        _logger.Entries.ShouldAllBe(entry => !entry.Message.Contains(S.SidecarToken) && !entry.Message.Contains("eyJ")
            && !entry.Message.Contains("sidecar.example") && !entry.Message.Contains("127.0.0.1"));
    }

    /// <summary>The sidecar port is accepted when no explicit endpoint is configured.</summary>
    [Fact]
    public async Task SidecarPortSelectsTheLocalSidecar()
    {
        _sidecar = _ => Identity();
        var settings = new Dictionary<string, string?> { ["DAPR_HTTP_PORT"] = "3500", ["DAPR_API_TOKEN"] = S.SidecarToken };
        (await Create(settings).CurrentTargetAsync(S.Signal().TenantId, TestContext.Current.CancellationToken)).ShouldBe(S.Target);
        _handler!.Requests.ShouldHaveSingleItem().Uri.ShouldBe(new Uri(S.Sidecar + "/v1.0/invoke/" + S.Receiver + "/method/query"));
    }

    /// <summary>The current target is an accepted full target reported with the fixed conformance scope and installed principals.</summary>
    [Fact]
    public async Task CurrentTargetRequiresTheFixedConformanceIdentity()
    {
        _sidecar = _ => Identity(target: S.RolloverTarget);
        (await Create().CurrentTargetAsync(S.Signal().TenantId, TestContext.Current.CancellationToken)).ShouldBe(S.RolloverTarget);
        QueryEnvelope envelope = JsonSerializer.Deserialize<QueryEnvelope>(_handler!.Requests.Single().Body, Web)!;
        envelope.AggregateId.ShouldBe("identity");
        envelope.QueryType.ShouldBe("deletion-conformance-identity");
        envelope.Payload.ShouldBeEmpty();
        envelope.UserId.ShouldBe(S.Lookup);
    }

    /// <summary>Any deviation from the fixed scope, principals, tenant or accepted target disables delivery.</summary>
    [Theory]
    [InlineData("receiverKind")]
    [InlineData("qualificationScope")]
    [InlineData("evidenceLevel")]
    [InlineData("agentsRuntimeEnabled")]
    [InlineData("agentsRuntimeEnabledText")]
    [InlineData("schemaVersion")]
    [InlineData("tenantId")]
    [InlineData("receiverPrincipalId")]
    [InlineData("lookupPrincipalId")]
    [InlineData("unaccepted-target")]
    [InlineData("short-target")]
    [InlineData("storageIdentity")]
    [InlineData("extra-field")]
    [InlineData("failed")]
    public async Task UnexpectedReceiverIdentityDisablesDelivery(string deviation)
    {
        _sidecar = _ => deviation switch
        {
            "receiverKind" => Identity(receiverKind: "AgentsRuntime"),
            "qualificationScope" => Identity(scope: "RQ-1"),
            "evidenceLevel" => Identity(level: 5),
            "agentsRuntimeEnabled" => Identity(agents: true),
            "agentsRuntimeEnabledText" => Identity(agents: "false"),
            "schemaVersion" => Identity(schema: 2),
            "tenantId" => Identity(tenant: "tenant-beta"),
            "receiverPrincipalId" => Identity(receiverPrincipal: S.Lookup),
            "lookupPrincipalId" => Identity(lookupPrincipal: S.Worker),
            "unaccepted-target" => Identity(target: new string('a', 40)),
            "short-target" => Identity(target: S.Target[..12]),
            "storageIdentity" => Identity(storage: " "),
            "extra-field" => Payload("{\"schemaVersion\":1,\"receiverKind\":\"ContractConformance\",\"qualificationScope\":\"OQ-5.4-1\","
                + "\"evidenceLevel\":4,\"agentsRuntimeEnabled\":false,\"tenantId\":\"tenant-alpha\",\"targetVersion\":\"" + S.Target
                + "\",\"storageIdentity\":\"s\",\"receiverPrincipalId\":\"" + S.Receiver + "\",\"lookupPrincipalId\":\"" + S.Lookup
                + "\",\"expectedTarget\":\"" + S.Target + "\"}"),
            "failed" => Raw(JsonSerializer.Serialize(QueryResult.Failure("Denied"), Web)),
            _ => throw new ArgumentOutOfRangeException(nameof(deviation)),
        };

        (await Create().CurrentTargetAsync(S.Signal().TenantId, TestContext.Current.CancellationToken)).ShouldBeNull(deviation);
    }

    private static ConversationDeletionAcknowledgement Receipt(ConversationDeletionSignal signal, string target)
        => new(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, target, S.Evidence(signal, target));

    private static HttpResponseMessage Raw(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Payload(string payloadJson)
        => Raw(JsonSerializer.Serialize(new QueryResult(true, Encoding.UTF8.GetBytes(payloadJson)), Web));

    private static HttpResponseMessage Result(object payload, HttpStatusCode status = HttpStatusCode.OK)
        => Raw(JsonSerializer.Serialize(QueryResult.FromPayload(JsonSerializer.SerializeToElement(payload, Web)), Web), status);

    private static HttpResponseMessage Identity(string receiverKind = "ContractConformance", string scope = "OQ-5.4-1", int level = 4,
        object? agents = null, int schema = 1, string tenant = "tenant-alpha", string receiverPrincipal = S.Receiver,
        string lookupPrincipal = S.Lookup, string target = S.Target, string storage = "receiver-storage")
        => Result(new
        {
            schemaVersion = schema,
            receiverKind,
            qualificationScope = scope,
            evidenceLevel = level,
            agentsRuntimeEnabled = agents ?? false,
            tenantId = tenant,
            targetVersion = target,
            storageIdentity = storage,
            receiverPrincipalId = receiverPrincipal,
            lookupPrincipalId = lookupPrincipal,
        });

    private ConformanceConversationDeletionReceiver Create(Dictionary<string, string?>? settings = null, FakeLookupIssuer? issuer = null)
    {
        _handler = new StubHttpHandler((request, body) =>
            Task.FromResult(_sidecar(JsonSerializer.Deserialize<QueryEnvelope>(body, Web)!)));
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings
            ?? new Dictionary<string, string?> { ["DAPR_HTTP_ENDPOINT"] = S.Sidecar, ["DAPR_API_TOKEN"] = S.SidecarToken }).Build();
        return new ConformanceConversationDeletionReceiver(_gateway, new StubHttpClientFactory(_handler), issuer ?? _issuer, S.Options(),
            configuration, _logger);
    }
}
