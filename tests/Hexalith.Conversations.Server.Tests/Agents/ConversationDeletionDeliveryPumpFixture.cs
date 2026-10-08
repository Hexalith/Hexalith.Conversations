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
        internal F Source => source;
        internal SourcePublicationIndexEntry Entry => entry;
        internal string Target = "receiver-v1";
        internal bool LoseReceiverResponse, RefuseSubmission, UnknownLookup, ChangedReceipt, RevokeAfterAttempt, BlockLookup;
        internal bool PersistCommands = true;
        internal string? BadBinding;
        internal int Submissions, SourceReads, ReceiverReads;
        internal List<RecordConversationDeletionDeliveryCommand> AttemptCommands = [];
        internal Dictionary<string, ConversationDeletionAcknowledgement> Receipts = [];
        internal TaskCompletionSource LookupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource<ConversationDeletionReceiverResult> LookupPending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ConversationDeletionDeliveryPump Pump() => new(this, new(BadBinding == "missing" ? null :
            new ConversationDeletionWorkerRegistration(F.Tenant, "conversations-worker", ConversationDeletionDeliveryPumpTests.ServiceParty), this), this);
        public Task<ConversationAgentAuthorization> AuthorizeAsync(string principal, TenantId tenant, ConversationId? conversation, string operation, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); operation.ShouldBeOneOf("DeletionSource", "RecordConversationDeletionDelivery"); conversation.ShouldBe(F.Conversation);
            return Task.FromResult(new ConversationAgentAuthorization(RevokeAfterAttempt && AttemptCommands.Count > 0
                ? ConversationAgentsOutcome.Denied : ConversationAgentsOutcome.Available,
                BadBinding == "tenant" ? new TenantId("other-tenant") : tenant,
                BadBinding == "principal" ? "wrong-principal" : principal,
                BadBinding == "party" ? F.Agent : ConversationDeletionDeliveryPumpTests.ServiceParty, "current-independent-authority", BadBinding != "organization"));
        }
        public async Task<ConversationDeletionSourceResult> GetConversationDeletionSourceAsync(ConversationDeletionSourceQuery request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); SourceReads++;
            var state = await source.ReplayAsync(); return state.DeletionSource;
        }
        public async Task<ConversationAgentCommandResult> RecordConversationDeletionDeliveryAsync(RecordConversationDeletionDeliveryCommand command, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); command.Metadata.ActorPartyId.ShouldBe(ConversationDeletionDeliveryPumpTests.ServiceParty);
            if (command.Action == ConversationDeletionDeliveryAction.Attempt) { AttemptCommands.Add(command); }
            if (PersistCommands) { source.Persist(ConversationAggregate.Handle(new RecordConversationDeletionDelivery(command,
                command.Metadata.IdempotencyKey!), await source.ReplayAsync())); }
            return new(ConversationAgentsOutcome.Available);
        }
        public Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken token) { token.ThrowIfCancellationRequested(); ReceiverReads++; return Task.FromResult<string?>(Target); }
        public Task<ConversationDeletionReceiverResult> LookupAsync(ConversationDeletionSignal signal, string attemptId, string target, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); ReceiverReads++;
            if (BlockLookup) { LookupEntered.TrySetResult(); return LookupPending.Task; }
            if (UnknownLookup) { return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Unavailable)); }
            return Task.FromResult(Receipts.TryGetValue(target, out var receipt) ? new(ConversationAgentsOutcome.Available, receipt)
                : new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Absent));
        }
        public Task<ConversationDeletionReceiverResult> SubmitAsync(ConversationDeletionSignal signal, string attemptId, string target, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Submissions++;
            source.PersistedEvents.OfType<ConversationDeletionDeliveryRecordedDomainEvent>()
                .Any(e => e.Action == ConversationDeletionDeliveryAction.Attempt && e.DeliveryAttemptId == attemptId && e.TargetVersion == target).ShouldBeTrue();
            if (RefuseSubmission) { return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Denied)); }
            var receipt = new ConversationDeletionAcknowledgement(ChangedReceipt ? "changed-signal" : signal.ConversationDeletionSignalId,
                signal.SourceRevision, 19, target, "synthetic-authenticated-receiver-receipt"); Receipts[target] = receipt;
            if (LoseReceiverResponse) { LoseReceiverResponse = false; return Task.FromException<ConversationDeletionReceiverResult>(new HttpRequestException("Controlled lost receiver response.")); }
            return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt));
        }
        public Task<ConversationClientResult<ConversationCreatedResult>> CreateConversationAsync(CreateConversationCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> AppendMessageAsync(AppendMessageCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationCommandAcceptedResult>> ReassignConversationProjectAsync(ReassignConversationProjectCommand command, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationDetailResult>> GetConversationAsync(GetConversationQuery query, CancellationToken token) => throw new NotSupportedException();
        public Task<ConversationClientResult<ConversationListResult>> ListConversationsAsync(ListConversationsQuery query, CancellationToken token) => throw new NotSupportedException();
    }
