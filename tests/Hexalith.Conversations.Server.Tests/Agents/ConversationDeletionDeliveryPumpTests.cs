using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Client;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Worker orchestration against actual aggregate transitions/serialized replay and synthetic authenticated receiver ports.</summary>
public sealed class ConversationDeletionDeliveryPumpTests
{
    internal static readonly PartyId ServiceParty = new("conversations-dedicated-worker");
    private static async Task<ConversationDeletionDeliveryPumpFixture> Approved()
    {
        var f = new F(); var before = await f.ReplayAsync();
        f.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"),
            F.Conversation, "independent-approval", before.SourceRevision, F.At.AddMinutes(1),
            F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
        var stream = (await f.ReadAsync(new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value), TestContext.Current.CancellationToken)).Stream!;
        return new(f, new(1, new ConversationDeletionPublicationProjector().Project(stream, TestContext.Current.CancellationToken).Single()));
    }

    /// <summary>A response lost after receiver persistence is resolved by exact lookup without another dispatch or new signal.</summary>
    [Fact]
    public async Task LostReceiverAcknowledgementRecoversAcrossPumpRestart()
    {
        var f = await Approved(); f.LoseReceiverResponse = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable);
        var pending = (await f.Source.ReplayAsync()).DeletionSource;
        pending.LastAttemptId.ShouldNotBeNull(); pending.AcknowledgedSourceRevision.ShouldBe(0);
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
        var committed = (await f.Source.ReplayAsync()).DeletionSource;
        committed.AcknowledgedSourceRevision.ShouldBe(f.Entry.Publication.SourceRevision);
        f.Submissions.ShouldBe(1); f.AttemptCommands.Count.ShouldBe(1);
        f.AttemptCommands.Single().Metadata.ActorPartyId.ShouldBe(ServiceParty);
        committed.Signal!.ConversationDeletionSignalId.ShouldBe(f.Entry.Publication.PublicationId);
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
        f.Submissions.ShouldBe(1);
    }

    /// <summary>Gateway admission cannot authorize dispatch before the attempt is independently visible in source replay.</summary>
    [Fact]
    public async Task UnpersistedAttemptDoesNotDispatchAndRetryReusesLogicalIdentity()
    {
        var f = await Approved(); f.PersistCommands = false;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable);
        f.Submissions.ShouldBe(0); (await f.Source.ReplayAsync()).DeletionSource.LastAttemptId.ShouldBeNull();
        string original = f.AttemptCommands.Single().DeliveryAttemptId;
        f.PersistCommands = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
        f.AttemptCommands.Select(c => c.DeliveryAttemptId).Distinct().Single().ShouldBe(original); f.Submissions.ShouldBe(1);
    }

    /// <summary>Typed refusal/target rollover changes only a durable attempt after authoritative old-target absence.</summary>
    [Fact]
    public async Task TargetRolloverRetainsOriginalSignalAndSourceRevision()
    {
        var f = await Approved(); f.RefuseSubmission = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable);
        f.Target = "receiver-v2"; f.RefuseSubmission = false;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
        f.AttemptCommands.Count.ShouldBe(2);
        f.AttemptCommands.Select(c => c.Signal).Distinct().Count().ShouldBe(1);
        f.AttemptCommands.Select(c => c.DeliveryAttemptId).Distinct().Count().ShouldBe(2);
        (await f.Source.ReplayAsync()).DeletionSource.TargetVersion.ShouldBe("receiver-v2");
    }

    /// <summary>Unknown old-target lookup neither dispatches nor creates a rollover attempt.</summary>
    [Fact]
    public async Task UnknownLookupRetainsPendingEntryWithoutRetry()
    {
        var f = await Approved(); f.RefuseSubmission = true;
        await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken);
        f.Target = "receiver-v2"; f.UnknownLookup = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable);
        f.Submissions.ShouldBe(1); f.AttemptCommands.Count.ShouldBe(1);
        (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
    }

    /// <summary>A changed authenticated receipt is durably quarantined and cannot advance source acknowledgement.</summary>
    [Fact]
    public async Task ChangedReceiptQuarantinesWithoutAcknowledgement()
    {
        var f = await Approved(); f.ChangedReceipt = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Quarantined);
        var state = (await f.Source.ReplayAsync()).DeletionSource;
        state.Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined); state.AcknowledgedSourceRevision.ShouldBe(0);
        state.Signal!.ConversationDeletionSignalId.ShouldBe(f.Entry.Publication.PublicationId);
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Quarantined);
        f.Submissions.ShouldBe(1);
    }

    /// <summary>An accepted but unpersisted quarantine is unavailable, never a durable poison result.</summary>
    [Fact]
    public async Task UnpersistedQuarantineCannotClaimDurablePoison()
    {
        var f = await Approved(); f.RefuseSubmission = true;
        await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken);
        f.RefuseSubmission = false; f.ChangedReceipt = true; f.PersistCommands = false;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable);
        var state = (await f.Source.ReplayAsync()).DeletionSource;
        state.Outcome.ShouldBe(ConversationAgentsOutcome.Available); state.PoisonCode.ShouldBeNull(); state.AcknowledgedSourceRevision.ShouldBe(0);
    }

    /// <summary>Configuration cannot replace independent current tenant/Party/principal authorization.</summary>
    [Theory]
    [InlineData("tenant")]
    [InlineData("party")]
    [InlineData("principal")]
    [InlineData("organization")]
    [InlineData("missing")]
    public async Task InvalidWorkerBindingStopsBeforeSourceOrReceiver(string vector)
    {
        var f = await Approved(); f.BadBinding = vector;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Denied);
        f.SourceReads.ShouldBe(0); f.ReceiverReads.ShouldBe(0); f.Submissions.ShouldBe(0);
    }

    /// <summary>Revocation after persisted attempt is rechecked before dispatch; no approving-human fallback exists.</summary>
    [Fact]
    public async Task RevocationBeforeDispatchRetainsAttemptWithoutRelease()
    {
        var f = await Approved(); f.RevokeAfterAttempt = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Denied);
        f.Submissions.ShouldBe(0); (await f.Source.ReplayAsync()).DeletionSource.LastAttemptId.ShouldNotBeNull();
        f.AttemptCommands.Single().Metadata.ActorPartyId.ShouldBe(ServiceParty);
    }

    /// <summary>Caller cancellation during receiver lookup preserves its original token and performs no dispatch.</summary>
    [Fact]
    public async Task CallerCancellationDuringReceiverLookupPreservesOriginalToken()
    {
        var f = await Approved(); f.BlockLookup = true; using var caller = new CancellationTokenSource();
        var delivery = f.Pump().DeliverAsync(f.Entry, caller.Token);
        await f.LookupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken); caller.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => delivery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        exception.CancellationToken.ShouldBe(caller.Token); f.Submissions.ShouldBe(0); f.LookupPending.TrySetResult(new(ConversationAgentsOutcome.Absent));
    }

}
