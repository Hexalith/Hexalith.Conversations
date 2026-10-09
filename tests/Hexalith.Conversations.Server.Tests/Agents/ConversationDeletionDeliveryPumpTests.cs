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
using Hexalith.EventStore.Client.Streams;
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


    /// <summary>Read-only owner acknowledgement lookup proves actual serialized Pending/Acknowledged state through restart and fails closed for unknown, quarantined or withdrawn authority.</summary>
    [Theory]
    [InlineData("ack")][InlineData("unknown")][InlineData("quarantine")][InlineData("withdrawn")]
    public async Task ConcretePublicationAdapterReadsActualSourceAcknowledgementAndCurrentAuthority(string vector)
    {
        var f = await Approved(); var adapter = new ConversationDeletionPublicationDelivery(f.Pump());
        (await adapter.LookupAcknowledgementAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Pending);
        f.Submissions.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty();
        if (vector == "quarantine") { f.ChangedReceipt = true; (await adapter.DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Quarantined); }
        else if (vector == "unknown") { f.Source.CorruptHead = true; }
        else { (await adapter.DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Acknowledged); }
        if (vector == "withdrawn") { f.WithdrawAuthority = true; }
        var restarted = new ConversationDeletionPublicationDelivery(f.Pump()); int effects = f.Submissions, commands = f.AttemptCommands.Count;
        var expected = vector == "ack" ? SourcePublicationDeliveryStatus.Acknowledged : vector == "quarantine" ? SourcePublicationDeliveryStatus.Quarantined : SourcePublicationDeliveryStatus.Unavailable;
        (await restarted.LookupAcknowledgementAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(expected);
        f.Submissions.ShouldBe(effects); f.AttemptCommands.Count.ShouldBe(commands);
        if (vector == "ack") { (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(f.Entry.Publication.SourceRevision); }
    }
    /// <summary>Delivery-only revocation after durable attempt never borrows still-current source-read permission for a receiver effect.</summary>
    [Fact]
    public async Task DeliveryPermissionWithdrawalAfterAttemptMakesZeroSubmissions()
    {
        var f = await Approved(); f.RevokeDeliveryAfterAttempt = true;
        (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Denied);
        f.Submissions.ShouldBe(0); f.AttemptCommands.Count.ShouldBe(1);
        var original = (await f.Source.ReplayAsync()).DeletionSource; original.LastAttemptId.ShouldBe(f.AttemptCommands.Single().DeliveryAttemptId);
        original.Acknowledgement.ShouldBeNull(); original.AcknowledgedSourceRevision.ShouldBe(0);
        (await f.AuthorizeAsync("conversations-worker", F.Tenant, F.Conversation, "DeletionSource", TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Available);
    }
    /// <summary>The actually suspended second target lookup is followed by fresh source and delivery admission, preserving the persisted original attempt.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task AuthorityWithdrawalDuringFinalTargetLookupMakesZeroSubmissions(bool deliveryOnly)
    {
        var f = await Approved(); f.BlockTargetCall = 2;
        var result = f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken);
        await f.TargetEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        f.AttemptCommands.Count.ShouldBe(1); f.Submissions.ShouldBe(0);
        if (deliveryOnly) { f.RevokeDeliveryAfterAttempt = true; } else { f.WithdrawAuthority = true; }
        f.TargetPending.SetResult(f.Target);
        (await result.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Denied);
        f.Submissions.ShouldBe(0); f.AttemptCommands.Count.ShouldBe(1);
        var original = (await f.Source.ReplayAsync()).DeletionSource; original.LastAttemptId.ShouldBe(f.AttemptCommands.Single().DeliveryAttemptId);
        original.Acknowledgement.ShouldBeNull(); original.AcknowledgedSourceRevision.ShouldBe(0);
    }

    /// <summary>Withdrawal during the final actual serialized source read cannot release acknowledgement/quarantine success, while durable original receipts remain intact.</summary>
    [Theory]
    [InlineData("existing-ack")][InlineData("existing-quarantine")][InlineData("persisted-ack")][InlineData("persisted-quarantine")][InlineData("attempt-ack")]
    public async Task TerminalSourceReadRequiresSameCurrentWorkerAuthority(string vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        var f = await Approved();
        if (vector.StartsWith("existing", StringComparison.Ordinal))
        {
            f.ChangedReceipt = vector == "existing-quarantine";
            (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(f.ChangedReceipt ? ConversationAgentsOutcome.Quarantined : ConversationAgentsOutcome.Available);
            f.BlockSourceCall = f.SourceReads + 1;
        }
        else { f.ChangedReceipt = vector == "persisted-quarantine"; f.BlockSourceCall = vector == "attempt-ack" ? 2 : 3; }
        var pending = f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken);
        await f.SourceEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        // Exercise the attempt-read shortcut with an original acknowledgement already committed by another independently admitted worker.
        if (vector == "attempt-ack")
        {
            f.BlockSourceCall = 0;
            (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
        }
        var retained = (await f.Source.ReplayAsync()).DeletionSource;
        string before = System.Text.Json.JsonSerializer.Serialize(f.Source.PersistedEvents);
        f.WithdrawAuthority = true; f.SourcePending.TrySetResult();
        (await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken)).ShouldBe(vector == "attempt-ack" ? ConversationAgentsOutcome.Unavailable : ConversationAgentsOutcome.Denied);
        System.Text.Json.JsonSerializer.Serialize(f.Source.PersistedEvents).ShouldBe(before);
        var after = (await f.Source.ReplayAsync()).DeletionSource;
        after.Acknowledgement.ShouldBe(retained.Acknowledgement); after.LastAttemptId.ShouldBe(retained.LastAttemptId); after.PoisonCode.ShouldBe(retained.PoisonCode);
        after.AcknowledgedSourceRevision.ShouldBe(retained.AcknowledgedSourceRevision);
    }
    /// <summary>Standalone pump entries bound actual dependency Tasks and synchronous invocations; late results resume no later protocol phase.</summary>
    [Theory]
    [InlineData("authority", false, false, false)]
    [InlineData("authority", false, false, true)]
    [InlineData("authority", false, true, false)]
    [InlineData("authority", false, true, true)]
    [InlineData("source", false, false, false)]
    [InlineData("source", false, false, true)]
    [InlineData("source", false, true, false)]
    [InlineData("source", false, true, true)]
    [InlineData("target", false, false, false)]
    [InlineData("target", false, false, true)]
    [InlineData("target", false, true, false)]
    [InlineData("target", false, true, true)]
    [InlineData("lookup", false, false, false)]
    [InlineData("lookup", false, false, true)]
    [InlineData("lookup", false, true, false)]
    [InlineData("lookup", false, true, true)]
    [InlineData("submit", false, false, false)]
    [InlineData("submit", false, false, true)]
    [InlineData("submit", false, true, false)]
    [InlineData("submit", false, true, true)]
    [InlineData("record:Attempt", false, false, false)]
    [InlineData("record:Attempt", false, false, true)]
    [InlineData("record:Attempt", false, true, false)]
    [InlineData("record:Attempt", false, true, true)]
    [InlineData("authority", true, false, false)]
    [InlineData("authority", true, false, true)]
    [InlineData("authority", true, true, false)]
    [InlineData("authority", true, true, true)]
    [InlineData("source", true, false, false)]
    [InlineData("source", true, false, true)]
    [InlineData("source", true, true, false)]
    [InlineData("source", true, true, true)]
    public async Task StandalonePumpBudgetBoundsEveryDependency(string stage, bool lookupOnly, bool invocation, bool cancellation)
    {
        var f = await Approved(); var clock = new PumpClock(); using var caller = new CancellationTokenSource(); using var unblock = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pauses = 0;
        f.OperationHook = async operation =>
        {
            if (operation != stage || Interlocked.Increment(ref pauses) != 1) { return; }
            entered.TrySetResult(); if (invocation) { unblock.Wait(); } else { await released.Task; }
        };
        f.OperationFinished = operation => { if (operation == stage) { finished.TrySetResult(); } };
        var pump = f.Pump(clock);
        var pending = Task.Run(async () => lookupOnly ? (int)await pump.LookupAcknowledgementAsync(f.Entry, caller.Token) : (int)await pump.DeliverAsync(f.Entry, caller.Token), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            if (cancellation) { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            else { clock.Advance(TimeSpan.FromSeconds(30)); (await pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).ShouldBe(lookupOnly ? (int)SourcePublicationDeliveryStatus.Unavailable : (int)ConversationAgentsOutcome.Unavailable); }
            (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
            int attempts = f.AttemptCommands.Count; int submissions = f.Submissions;
            unblock.Set(); released.TrySetResult(); await finished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            f.OperationHook = null;
            if (stage == "submit")
            {
                SpinWait.SpinUntil(() => f.Receipts.ContainsKey(f.Target), TimeSpan.FromSeconds(2)).ShouldBeTrue();
                (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
                f.Submissions.ShouldBe(1); f.AttemptCommands.Count.ShouldBe(1);
                (await new ConversationDeletionPublicationDelivery(f.Pump()).LookupAcknowledgementAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Acknowledged);
            }
            else
            {
                if (stage == "record:Attempt") { SpinWait.SpinUntil(() => f.AttemptCommands.Count == 1, TimeSpan.FromSeconds(2)).ShouldBeTrue(); }
                else { f.AttemptCommands.Count.ShouldBe(attempts); }
                f.Submissions.ShouldBe(submissions); (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
            }
        }
        finally { unblock.Set(); released.TrySetResult(); }
    }
}
