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

/// <summary>Synthetic worker ports retain actual serialized aggregate events across pump instances; no production qualification.</summary>
internal sealed class ConversationDeletionDeliveryPumpFixture(F source, SourcePublicationIndexEntry entry) : IConversationClient, IConversationAgentAuthority, IConversationDeletionReceiver
    {
        internal Func<string, Task>? OperationHook;
        internal Action<string>? OperationFinished;
        internal F Source => source;
        internal SourcePublicationIndexEntry Entry => entry;
        internal string Target = "receiver-v1";
        internal bool LoseReceiverResponse, RefuseSubmission, UnknownLookup, ChangedReceipt, RevokeAfterAttempt, BlockLookup;
        internal bool PersistCommands = true;
        internal bool WithdrawAuthority, BlockTarget, RevokeDeliveryAfterAttempt;
        internal int BlockTargetCall, TargetCalls;
        internal TaskCompletionSource TargetEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<string?> TargetPending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal string? BadBinding;
        internal int Submissions, SourceReads, ReceiverReads;
        internal int BlockSourceCall;
        internal TaskCompletionSource SourceEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource SourcePending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal List<RecordConversationDeletionDeliveryCommand> AttemptCommands = [];
        internal Dictionary<string, ConversationDeletionAcknowledgement> Receipts = [];
        internal TaskCompletionSource LookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<ConversationDeletionReceiverResult> LookupPending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ConversationDeletionDeliveryPump Pump(TimeProvider? clock = null) => new(this, new(BadBinding == "missing" ? null :
            new ConversationDeletionWorkerRegistration(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), this), this, clock);
        public async Task<ConversationAgentAuthorization> AuthorizeAsync(string principal, TenantId tenant, ConversationId? conversation, string operation, CancellationToken token)
        {
            try
            {
            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("authority") ?? Task.CompletedTask); operation.ShouldBeOneOf("DeletionSource", "RecordConversationDeletionDelivery"); conversation.ShouldBe(source.CurrentConversation);
            return new ConversationAgentAuthorization(WithdrawAuthority || RevokeAfterAttempt && AttemptCommands.Count > 0 || RevokeDeliveryAfterAttempt && AttemptCommands.Count > 0 && operation == "RecordConversationDeletionDelivery"
                ? ConversationAgentsOutcome.Denied : ConversationAgentsOutcome.Available,
                BadBinding == "tenant" ? new TenantId("other-tenant") : tenant,
                BadBinding == "principal" ? "wrong-principal" : principal,
                BadBinding == "party" ? F.Agent : ConversationDeletionDeliveryPumpTests.ServiceParty, "current-independent-authority", BadBinding != "organization");
            }
            finally { OperationFinished?.Invoke("authority"); }
        }
        public async Task<ConversationDeletionSourceResult> GetConversationDeletionSourceAsync(ConversationDeletionSourceQuery request, CancellationToken token)
        {
            try
            {
            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("source") ?? Task.CompletedTask); SourceReads++;
            var state = await source.ReplayAsync();
            if (SourceReads == BlockSourceCall) { SourceEntered.TrySetResult(); await SourcePending.Task; state = await source.ReplayAsync(); }
            return state.DeletionSource;
            }
            finally { OperationFinished?.Invoke("source"); }
        }
        public async Task<ConversationAgentCommandResult> RecordConversationDeletionDeliveryAsync(RecordConversationDeletionDeliveryCommand command, CancellationToken token)
        {
            try
            {
            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("record:" + command.Action) ?? Task.CompletedTask); command.Metadata.ActorPartyId.ShouldBe(ConversationDeletionDeliveryPumpTests.ServiceParty);
            if (command.Action == ConversationDeletionDeliveryAction.Attempt) { AttemptCommands.Add(command); }
            if (PersistCommands) { source.Persist(ConversationAggregate.Handle(new RecordConversationDeletionDelivery(command,
                command.Metadata.IdempotencyKey!), await source.ReplayAsync())); }
            return new(ConversationAgentsOutcome.Available);
            }
            finally { OperationFinished?.Invoke("record:" + command.Action); }
        }
        public async Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken token) {
            try
            { token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("target") ?? Task.CompletedTask); ReceiverReads++; TargetCalls++; if (BlockTarget || BlockTargetCall == TargetCalls) { TargetEntered.TrySetResult(); return await TargetPending.Task; } return Target;
            }
            finally { OperationFinished?.Invoke("target"); }
        }
        public async Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target, CancellationToken token)
        {
            try
            {
            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("lookup") ?? Task.CompletedTask); ReceiverReads++;
            if (BlockLookup) { LookupEntered.TrySetResult(); return await LookupPending.Task; }
            if (UnknownLookup) { return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Unavailable); }
            return Receipts.TryGetValue(target, out var receipt) ? new(ConversationAgentsOutcome.Available, receipt)
                : new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Absent);
            }
            finally { OperationFinished?.Invoke("lookup"); }
        }
        public async Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target, CancellationToken token)
        {
            try
            {
            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("submit") ?? Task.CompletedTask); Submissions++;
            source.PersistedEvents.OfType<ConversationDeletionDeliveryRecordedDomainEvent>()
                .Any(e => e.Action == ConversationDeletionDeliveryAction.Attempt && e.DeliveryAttemptId == attemptId && e.TargetVersion == target).ShouldBeTrue();
            if (RefuseSubmission) { return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Denied); }
            var receipt = new ConversationDeletionAcknowledgement(ChangedReceipt ? "changed-signal" : signal.ConversationDeletionSignalId,
                signal.SourceRevision, 19, target, "synthetic-authenticated-receiver-receipt"); Receipts[target] = receipt;
            if (LoseReceiverResponse) { LoseReceiverResponse = false; throw new HttpRequestException("Controlled lost receiver response."); }
            return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt);
            }
            finally { OperationFinished?.Invoke("submit"); }
        }
        public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(CreateConversationCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(AppendMessageCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(ReassignConversationProjectCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(GetConversationQuery query, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(ListConversationsQuery query, CancellationToken token) => throw new NotSupportedException();
    }
