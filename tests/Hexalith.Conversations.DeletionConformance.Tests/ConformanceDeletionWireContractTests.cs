// <copyright file="ConformanceDeletionWireContractTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

extern alias server;

using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Srv = server::Hexalith.Conversations.Server.Agents;

namespace Hexalith.Conversations.DeletionConformance.Tests;

/// <summary>Binds the Server's mirrored receiver wire contract to this receiver's actual types; Server never references the tool.</summary>
public sealed class ConformanceDeletionWireContractTests
{
    private const string Tenant = "tenant-alpha";
    private const string Target = "0123456789abcdef0123456789abcdef01234567";
    private const string Receiver = "conversations-deletion-conformance";
    private const string Lookup = "conversations-deletion-lookup";
    private const string Worker = "conversations-worker";
    private const string Sidecar = "http://127.0.0.1:3500";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private static ConversationDeletionSignal Signal => new("signal-alpha", new TenantId(Tenant), new ConversationId("conversation-alpha"),
        new AggregateIdentity(Tenant, "conversation", "conversation-alpha").ActorId, 3, "independent-approval");

    /// <summary>The command type, domain, query types, scope and evidence prefix equal the receiver's own.</summary>
    [Fact]
    public void NamesMatchTheReceiver()
    {
        Srv.ConformanceDeletionWire.CommandType.ShouldBe(typeof(AcceptConformanceDeletion).FullName);
        Srv.ConversationDeletionRuntimeOptions.ReceiverDomain.ShouldBe(DeletionConformanceContract.Domain);
        Srv.ConformanceDeletionWire.QualificationScope.ShouldBe(DeletionConformanceContract.QualificationScope);
        Srv.ConformanceDeletionWire.ReceiptQueryType.ShouldBe(QueryType<DeletionConformanceReceiptQuery>());
        Srv.ConformanceDeletionWire.IdentityQueryType.ShouldBe(QueryType<DeletionConformanceIdentityQuery>());
        DeletionConformanceContract.Evidence(Signal, Target, 19).ShouldStartWith(Srv.ConformanceDeletionWire.EvidencePrefix);
    }

    /// <summary>The submitted and looked-up request is exactly the receiver's command and lookup shape.</summary>
    [Fact]
    public void RequestShapeMatchesTheReceiver()
    {
        var request = new Srv.ConformanceDeletionRequest(Signal, "attempt-1", Target);
        JsonElement sent = JsonSerializer.SerializeToElement(request, Web);

        Names(sent).ShouldBe(Srv.ConformanceDeletionWire.RequestFields);
        Names(JsonSerializer.SerializeToElement(new AcceptConformanceDeletion(Signal, "attempt-1", Target), Web))
            .ShouldBe(Srv.ConformanceDeletionWire.RequestFields);
        Names(JsonSerializer.SerializeToElement(new DeletionConformanceLookup(Signal, "attempt-1", Target), Web))
            .ShouldBe(Srv.ConformanceDeletionWire.RequestFields);
        sent.Deserialize<AcceptConformanceDeletion>(Web).ShouldBe(new AcceptConformanceDeletion(Signal, "attempt-1", Target));
        sent.Deserialize<DeletionConformanceLookup>(Web).ShouldBe(new DeletionConformanceLookup(Signal, "attempt-1", Target));
    }

    /// <summary>This receiver's actual identity answer has the mirrored field set, and the Server adapter accepts it.</summary>
    [Fact]
    public async Task ReceiverIdentityAnswerIsTheMirroredShape()
    {
        var options = new DeletionConformanceOptions
        {
            Enabled = true, TenantId = Tenant, TargetVersion = Target, ReceiverPrincipalId = Receiver, LookupPrincipalId = Lookup,
            WorkerPrincipalId = Worker, GatewayWorkloadId = "eventstore", StorageIdentity = "receiver-storage",
            GatewayBaseAddress = "https://gateway.test/",
        };
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, Lookup),
                    new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreWorkloadOperations.DomainServiceQuery),
                    new Claim(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType, Tenant),
                    new Claim(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType, DeletionConformanceContract.Domain),
                ], EventStoreWorkloadAuthenticationDefaults.WorkloadScheme)),
            },
        };
        var query = new DeletionConformanceIdentityQuery(Microsoft.Extensions.Options.Options.Create(options),
            new DeletionConformanceAuthorization(http));

        QueryResult answer = await query.ExecuteAsync(new QueryEnvelope(Tenant, DeletionConformanceContract.Domain,
            Srv.ConformanceDeletionWire.IdentityAggregateId, Srv.ConformanceDeletionWire.IdentityQueryType, [], "contract", Lookup),
            TestContext.Current.CancellationToken);

        answer.Success.ShouldBeTrue(answer.ErrorMessage);
        Names(answer.GetPayload()).ShouldBe(Srv.ConformanceDeletionWire.IdentityFields, ignoreOrder: true);
        (await AdapterFor(answer, out IEventStoreGatewayClient gateway).CurrentTargetAsync(new TenantId(Tenant),
            TestContext.Current.CancellationToken)).ShouldBe(Target);
        gateway.ReceivedCalls().ShouldBeEmpty("identity lookups use only the sidecar");
    }

    /// <summary>This receiver's actual receipt result type and evidence are the mirrored shape, and the Server adapter accepts them.</summary>
    [Fact]
    public async Task ReceiverReceiptAnswerIsTheMirroredShape()
    {
        var receipt = new ConversationDeletionAcknowledgement(Signal.ConversationDeletionSignalId, Signal.SourceRevision, 19, Target,
            DeletionConformanceContract.Evidence(Signal, Target, 19));
        JsonElement payload = JsonSerializer.SerializeToElement(new DeletionConformanceResult(ConversationAgentsOutcome.Available, receipt), Web);

        Names(payload).ShouldBe(Srv.ConformanceDeletionWire.ReceiptFields);
        Names(payload.GetProperty("acknowledgement")).ShouldBe(Srv.ConformanceDeletionWire.AcknowledgementFields, ignoreOrder: true);
        (await AdapterFor(QueryResult.FromPayload(payload), out IEventStoreGatewayClient gateway)
            .LookupAsync(Signal, "attempt-1", Target, TestContext.Current.CancellationToken))
            .ShouldBe(new Srv.ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt));
        gateway.ReceivedCalls().ShouldBeEmpty("receipt lookups use only the sidecar");
    }

    private static string QueryType<T>() where T : IDomainQueryHandler
        => ((T)RuntimeHelpers.GetUninitializedObject(typeof(T))).QueryType;

    private static string[] Names(JsonElement element) => [.. element.EnumerateObject().Select(static property => property.Name)];

    private static Srv.ConformanceConversationDeletionReceiver AdapterFor(QueryResult answer, out IEventStoreGatewayClient gateway)
    {
        var options = new Srv.ConversationDeletionRuntimeOptions
        {
            Enabled = true, TenantId = Tenant, WorkerPrincipalId = Worker, ServicePartyId = "conversations-dedicated-worker",
            NamespaceInstallationId = "installation-a", StorageIdentity = "conversations-storage", GatewayBaseAddress = "https://gateway.test/",
            PlatformWorkloadId = "eventstore", ReceiverPrincipalId = Receiver, LookupPrincipalId = Lookup,
            IdentityReaderPrincipalId = "conversations-runtime-reader", AcceptedTargetVersions = [Target],
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = Sidecar,
            ["DAPR_API_TOKEN"] = "sidecar-api-token",
        }).Build();
        gateway = Substitute.For<IEventStoreGatewayClient>();
        return new Srv.ConformanceConversationDeletionReceiver(gateway, new ConformanceSidecarClients(answer), new BoundLookupIssuer(Lookup),
            options, configuration, NullLogger<Srv.ConformanceConversationDeletionReceiver>.Instance);
    }
}
