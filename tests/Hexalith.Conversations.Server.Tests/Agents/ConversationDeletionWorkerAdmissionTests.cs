using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Actual client serialization reaches concrete server admission with synthetic authenticated authority/source/receiver ports.</summary>
public sealed class ConversationDeletionWorkerAdmissionTests
{
    private static async Task<ConversationDeletionDeliveryPumpFixture> Approved()
    {
        var source = new F(); var before = await source.ReplayAsync();
        source.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(source.CommandMetadata(F.Human, "approval"), F.Conversation,
            "independent-approval", before.SourceRevision, F.At.AddMinutes(1), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
        var stream = (await source.ReadAsync(new Hexalith.EventStore.Contracts.Identity.AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken)).Stream!;
        return new(source, new SourcePublicationIndexEntry(1, new ConversationDeletionPublicationProjector().Project(stream, TestContext.Current.CancellationToken).Single()));
    }

    /// <summary>Dedicated Party metadata is admitted, while wrong Party, principal, tenant and receiver target fail at the actual server boundary.</summary>
    [Theory]
    [InlineData("valid")]
    [InlineData("party")]
    [InlineData("principal")]
    [InlineData("tenant")]
    [InlineData("target")]
    public async Task ClientSerializationUsesDedicatedWorkerAdmission(string vector)
    {
        var f = await Approved(); var state = await f.Source.ReplayAsync();
        var metadata = f.Source.CommandMetadata(vector == "party" ? F.Human : ConversationDeletionDeliveryPumpTests.ServiceParty, "worker-attempt");
        var command = new RecordConversationDeletionDeliveryCommand(metadata, F.Conversation, state.DeletionSource.Signal!,
            ConversationDeletionDeliveryAction.Attempt, "attempt-1", vector == "target" ? "wrong-target" : f.Target, 0);
        var worker = new ConfiguredConversationDeletionWorker(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f);
        var admission = new ConversationAgentAdmissionStage(f, f.Source, new ConfiguredConversationDeletionReceiptVerifier(worker, f), f.Source);
        bool? accepted = null;
        using var handler = new ConversationDeletionWorkerBoundaryHandler(async (request, token) =>
        {
            var wire = await request.Content!.ReadFromJsonAsync<SubmitCommandRequest>(F.Options, token);
            wire!.CommandType.ShouldBe("RecordConversationDeletionDelivery");
            var envelope = new CommandEnvelope(wire.MessageId, vector == "tenant" ? "wrong-tenant" : wire.Tenant, wire.Domain, wire.AggregateId,
                wire.CommandType, JsonSerializer.SerializeToUtf8Bytes(wire.Payload, F.Options), wire.CorrelationId!, null,
                vector == "principal" ? "wrong-authenticated-principal" : "conversations-worker", null);
            var current = await f.Source.CurrentStateAsync();
            var result = await admission.EvaluateAsync(new DomainServiceAdmissionContext(new(envelope, current)), token);
            accepted = result.IsAccepted;
            return new HttpResponseMessage(result.IsAccepted ? HttpStatusCode.Accepted : HttpStatusCode.Forbidden)
            { Content = JsonContent.Create(new SubmitCommandResponse(wire.CorrelationId!, MessageId: wire.MessageId), options: F.Options) };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://synthetic-gateway.invalid/") };
        var result = await new ConversationClient(http).RecordConversationDeletionDeliveryAsync(command, TestContext.Current.CancellationToken);
        accepted.ShouldBe(vector == "valid"); result.Outcome.ShouldBe(vector == "valid" ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Denied);
        result.Persisted.ShouldBeFalse();
    }

    /// <summary>A receipt supplied by a caller cannot pass acknowledgement admission without independent receiver exact lookup.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AcknowledgementRequiresIndependentExactReceiverReceipt(bool known)
    {
        var f = await Approved(); var state = await f.Source.ReplayAsync(); var signal = state.DeletionSource.Signal!;
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, f.Target, "synthetic-known-receipt");
        if (known) { f.Receipts[f.Target] = receipt; }
        var request = new RecordConversationDeletionDeliveryCommand(f.Source.CommandMetadata(ConversationDeletionDeliveryPumpTests.ServiceParty, "ack"),
            F.Conversation, signal, ConversationDeletionDeliveryAction.Acknowledge, "attempt-1", f.Target, 1, receipt);
        var verifier = new ConfiguredConversationDeletionReceiptVerifier(new(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f), f);
        (await verifier.VerifyAsync(f.Source.Envelope(new RecordConversationDeletionDelivery(request, "ack-event"), "conversations-worker"), request,
            TestContext.Current.CancellationToken)).ShouldBe(known ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Denied);
    }
}
