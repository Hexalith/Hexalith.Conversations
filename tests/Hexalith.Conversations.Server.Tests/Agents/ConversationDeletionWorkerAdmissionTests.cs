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
    /// <summary>Current machine/Party withdrawal during either final receiver await prevents command admission.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task WorkerWithdrawalDuringFinalReceiverAwaitDenies(bool acknowledgement)
    {
        var f = await Approved(); var signal = (await f.Source.ReplayAsync()).DeletionSource.Signal!;
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, f.Target, "independent-receipt");
        f.BlockLookup = acknowledgement; f.BlockTarget = !acknowledgement;
        var request = new RecordConversationDeletionDeliveryCommand(f.Source.CommandMetadata(ConversationDeletionDeliveryPumpTests.ServiceParty, "withdrawal"), F.Conversation, signal,
            acknowledgement ? ConversationDeletionDeliveryAction.Acknowledge : ConversationDeletionDeliveryAction.Attempt, "original-attempt", f.Target, acknowledgement ? 1 : 0,
            acknowledgement ? receipt : null);
        var verifier = new ConfiguredConversationDeletionReceiptVerifier(new(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f), f);
        var verifying = verifier.VerifyAsync(f.Source.Envelope(new RecordConversationDeletionDelivery(request, "event"), "conversations-worker"), request, TestContext.Current.CancellationToken);
        await (acknowledgement ? f.LookupEntered.Task : f.TargetEntered.Task).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        f.WithdrawAuthority = true;
        if (acknowledgement) { f.LookupPending.TrySetResult(new(ConversationAgentsOutcome.Available, receipt)); } else { f.TargetPending.TrySetResult(f.Target); }
        (await verifying).ShouldBe(ConversationAgentsOutcome.Denied);
        (await f.Source.ReplayAsync()).DeletionSource.DeliveryRevision.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty();
    }

    /// <summary>The standalone exact source and delivery worker bounds an actually synchronous authority invocation with original cancellation.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task WorkerInvocationCannotOutliveCanceledCaller(bool delivery)
    {
        var f = await Approved(); using var caller = new CancellationTokenSource(); using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.OperationHook = _ => { entered.TrySetResult(); release.Wait(); return Task.CompletedTask; };
        f.OperationFinished = _ => finished.TrySetResult();
        var worker = new ConfiguredConversationDeletionWorker(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f);
        var pending = Task.Run(() => delivery ? worker.AuthorizeDeliveryAsync(F.Tenant, F.Conversation, caller.Token)
            : worker.AuthorizeAsync(F.Tenant, F.Conversation, caller.Token), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        try
        {
            caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));
            error.CancellationToken.ShouldBe(caller.Token);
        }
        finally { release.Set(); }
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        f.Submissions.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty(); (await f.Source.ReplayAsync()).DeletionSource.DeliveryRevision.ShouldBe(0);
    }

    /// <summary>Both standalone operations bound invocation and returned tasks with cancellation and uncancelled deadline expiry.</summary>
    [Theory]
    [InlineData(false, false, false)][InlineData(false, false, true)][InlineData(false, true, false)][InlineData(false, true, true)]
    [InlineData(true, false, false)][InlineData(true, false, true)][InlineData(true, true, false)][InlineData(true, true, true)]
    public async Task StandaloneWorkerHasOneBoundedAuthorityAcquisition(bool delivery, bool invocation, bool expires)
    {
        var f = await Approved(); var clock = new PumpClock(); using var caller = new CancellationTokenSource(); using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int calls = 0;
        f.OperationHook = _ => { Interlocked.Increment(ref calls); entered.TrySetResult(); if (invocation) { release.Wait(); return Task.CompletedTask; } return released.Task; };
        f.OperationFinished = _ => finished.TrySetResult();
        var worker = new ConfiguredConversationDeletionWorker(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f, clock);
        var pending = delivery ? worker.AuthorizeDeliveryAsync(F.Tenant, F.Conversation, expires ? CancellationToken.None : caller.Token)
            : worker.AuthorizeAsync(F.Tenant, F.Conversation, expires ? CancellationToken.None : caller.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        try
        {
            if (expires) { clock.Advance(TimeSpan.FromSeconds(30)); (await pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).ShouldBeNull(); }
            else { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            calls.ShouldBe(1);
        }
        finally { release.Set(); released.TrySetResult(); }
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        calls.ShouldBe(1); f.Submissions.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty();
    }

    /// <summary>The concrete receipt verifier bounds every external phase; the actual admission stage receives the repaired unavailable/canceled result.</summary>
    [Theory]
    [InlineData("initial", false, false, false)][InlineData("initial", false, true, false)][InlineData("initial", true, false, false)][InlineData("initial", true, true, false)]
    [InlineData("final", false, false, false)][InlineData("final", false, true, false)][InlineData("final", true, false, false)][InlineData("final", true, true, false)]
    [InlineData("lookup", false, false, false)][InlineData("lookup", false, true, false)][InlineData("lookup", true, false, false)][InlineData("lookup", true, true, false)]
    [InlineData("target", false, false, false)][InlineData("target", false, true, false)][InlineData("target", true, false, false)][InlineData("target", true, true, false)]
    [InlineData("initial", false, true, true)][InlineData("target", true, false, true)]
    public async Task StandaloneReceiptBudgetStopsEveryPhaseWithoutLateAdmission(string phase, bool invocation, bool expires, bool throughAdmission)
    {
        var f = await Approved(); var signal = (await f.Source.ReplayAsync()).DeletionSource.Signal!;
        bool acknowledge = phase == "lookup";
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, f.Target, "independent-receipt");
        f.Receipts[f.Target] = receipt;
        var command = new RecordConversationDeletionDeliveryCommand(f.Source.CommandMetadata(ConversationDeletionDeliveryPumpTests.ServiceParty, "bounded-receipt"), F.Conversation,
            signal, acknowledge ? ConversationDeletionDeliveryAction.Acknowledge : ConversationDeletionDeliveryAction.Attempt, "original-attempt", f.Target, acknowledge ? 1 : 0, acknowledge ? receipt : null);
        var envelope = f.Source.Envelope(new RecordConversationDeletionDelivery(command, "original-event"), "conversations-worker");
        byte[] original = JsonSerializer.SerializeToUtf8Bytes(f.Source.PersistedEvents, F.Options);
        var clock = new PumpClock(); using var caller = new CancellationTokenSource(); using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int authorities = 0; int calls = 0;
        string stopped = phase is "initial" or "final" ? "authority" : phase;
        f.OperationHook = label =>
        {
            Interlocked.Increment(ref calls); int authority = label == "authority" ? Interlocked.Increment(ref authorities) : 0;
            if (label != stopped || phase == "initial" && authority != 1 || phase == "final" && authority != 2) { return Task.CompletedTask; }
            entered.TrySetResult(); if (invocation) { release.Wait(); return Task.CompletedTask; } return released.Task;
        };
        f.OperationFinished = label => { if (label == stopped && entered.Task.IsCompleted) { finished.TrySetResult(); } };
        var worker = new ConfiguredConversationDeletionWorker(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f, clock);
        var verifier = new ConfiguredConversationDeletionReceiptVerifier(worker, f, clock);
        var token = expires ? CancellationToken.None : caller.Token;
        async Task<ConversationAgentsOutcome> Run()
        {
            if (!throughAdmission) { return await verifier.VerifyAsync(envelope, command, token); }
            var stage = new ConversationAgentAdmissionStage(f, f.Source, verifier, f.Source);
            var result = await stage.EvaluateAsync(new DomainServiceAdmissionContext(new(envelope, await f.Source.CurrentStateAsync())), token);
            return result.IsAccepted ? ConversationAgentsOutcome.Available : ConversationAgentsOutcome.Unavailable;
        }
        var pending = Run(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        int atTermination;
        try
        {
            if (expires) { clock.Advance(TimeSpan.FromSeconds(30)); (await pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Unavailable); }
            else { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            atTermination = Volatile.Read(ref calls);
        }
        finally { release.Set(); released.TrySetResult(); }
        await finished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Volatile.Read(ref calls).ShouldBe(atTermination); f.Submissions.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty();
        (await f.Source.ReplayAsync()).DeletionSource.DeliveryRevision.ShouldBe(0);
        JsonSerializer.SerializeToUtf8Bytes(f.Source.PersistedEvents, F.Options).ShouldBe(original);
    }

    /// <summary>New dependency calls cannot reset the verifier's elapsed budget or override final original caller cancellation.</summary>
    [Theory]
    [InlineData(false, false)][InlineData(true, false)][InlineData(false, true)][InlineData(true, true)]
    public async Task ReceiptBudgetIsCumulativeThroughFinalAuthorization(bool acknowledge, bool cancelFinal)
    {
        var f = await Approved(); var signal = (await f.Source.ReplayAsync()).DeletionSource.Signal!;
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, f.Target, "independent-receipt"); f.Receipts[f.Target] = receipt;
        var command = new RecordConversationDeletionDeliveryCommand(f.Source.CommandMetadata(ConversationDeletionDeliveryPumpTests.ServiceParty, "cumulative"), F.Conversation,
            signal, acknowledge ? ConversationDeletionDeliveryAction.Acknowledge : ConversationDeletionDeliveryAction.Attempt, "original-attempt", f.Target, acknowledge ? 1 : 0, acknowledge ? receipt : null);
        var clock = new PumpClock(); using var caller = new CancellationTokenSource(); int authorities = 0;
        f.OperationHook = label =>
        {
            if (label == "authority")
            {
                if (++authorities == 1) { clock.Advance(TimeSpan.FromSeconds(20)); }
                else if (cancelFinal) { caller.Cancel(); } else { clock.Advance(TimeSpan.FromSeconds(5)); }
            }
            else { clock.Advance(TimeSpan.FromSeconds(5)); }
            return Task.CompletedTask;
        };
        var verifier = new ConfiguredConversationDeletionReceiptVerifier(new(new(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), f, clock), f, clock);
        var pending = verifier.VerifyAsync(f.Source.Envelope(new RecordConversationDeletionDelivery(command, "original-event"), "conversations-worker"), command, caller.Token);
        if (cancelFinal) { var error = await Should.ThrowAsync<OperationCanceledException>(() => pending); error.CancellationToken.ShouldBe(caller.Token); }
        else { (await pending).ShouldBe(ConversationAgentsOutcome.Unavailable); }
        authorities.ShouldBe(2); f.Submissions.ShouldBe(0); f.AttemptCommands.ShouldBeEmpty();
    }

}
