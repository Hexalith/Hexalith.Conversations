// <copyright file="ConversationAgentSixSeamTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.Conversations.Server.TenantAccess;
using Hexalith.Conversations.Contracts.Projections;
using Microsoft.AspNetCore.Builder;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.EventStore.DomainService;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>All six named owner seams and cross-tenant proof using serialized local events.</summary>
public sealed class ConversationAgentSixSeamTests
{
    /// <summary>Verifies membership retry removal and serialized replay.</summary>
    [Fact]
    public async Task MembershipRetryRemovalAndSerializedReplay()
    {
        var f = new F();
        var state = await f.ReplayAsync();
        var first = ConversationAggregate.Handle(f.Membership, state);
        first.Events.Count.ShouldBe(1);
        f.Persist(first);
        var replayed = await f.ReplayAsync();
        replayed.Participants.Count.ShouldBe(1);
        ConversationAggregate.Handle(f.Membership, replayed).Events.ShouldBeEmpty();
        var conflict = f.Membership with
        {
            PublicCommand = f.Membership.PublicCommand with
            {
                ParticipantRole = ParticipantRole.Facilitator
            }
        };
        ConversationAggregate.Handle(conflict, replayed).IsRejection.ShouldBeTrue();
        var remove = new RemoveAgentParticipant(new(f.CommandMetadata(), F.Conversation, F.Agent, F.At.AddMinutes(3)), "remove-event");
        f.Persist(ConversationAggregate.Handle(remove, replayed));
        var removed = await f.ReplayAsync();
        removed.Participants.ShouldBeEmpty();
        removed.WasAgentRemoved(F.Agent).ShouldBeTrue();
        ConversationAggregate.Handle(f.Membership, removed).IsRejection.ShouldBeTrue();
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation, ParticipantStateOnly: true), cancellationToken: TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.PrincipalRemovedFromConversation);
    }

    /// <summary>Verifies deterministic posting lost acknowledgement and concurrent intent.</summary>
    [Fact]
    public async Task DeterministicPostingLostAcknowledgementAndConcurrentIntent()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        var before = await f.ReplayAsync();
        var contender = f.Posting with
        {
            PublicCommand = f.Posting.PublicCommand with
            {
                Text = "Changed intent"
            }
        };
        var winner = ConversationAggregate.Handle(f.Posting, before);
        var candidate = ConversationAggregate.Handle(contender, before);
        winner.Events.Count.ShouldBe(1);
        candidate.Events.Count.ShouldBe(1);
        // Simulate the EventStore source compare winner; the losing intent must re-evaluate against that persisted winner.
        f.Persist(winner);
        var restarted = await f.ReplayAsync();
        ConversationAggregate.Handle(f.Posting, restarted).Events.ShouldBeEmpty();
        ConversationAggregate.Handle(contender, restarted).IsRejection.ShouldBeTrue();
        restarted.Messages.Count.ShouldBe(1);
        var changedProvenance = f.Posting with
        {
            PublicCommand = f.Posting.PublicCommand with
            {
                AgentProvenance = new("different-trace", true, false)
            }
        };
        ConversationAggregate.Handle(changedProvenance, restarted).IsRejection.ShouldBeTrue();
        var found = await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation, f.Posting.PublicCommand.MessageId), cancellationToken: TestContext.Current.CancellationToken);
        found.Outcome.ShouldBe(ConversationAgentsOutcome.Available);
        found.Messages!.Single().MessageId.ShouldBe(f.Posting.PublicCommand.MessageId);
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation, new MessageId("absent")), cancellationToken: TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Absent);
    }

    /// <summary>Verifies facilitator roster and current edited deleted redacted content.</summary>
    [Fact]
    public async Task FacilitatorRosterAndCurrentEditedDeletedRedactedContent()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        f.PersistedEvents.Add(new ParticipantAddedDomainEvent(f.Metadata(ConversationEventType.ParticipantAdded, F.Human, F.At.AddMinutes(1)),
            F.Human, ParticipantType.Human, ParticipantRole.Facilitator));
        f.Persist(ConversationAggregate.Handle(f.Posting, await f.ReplayAsync()));
        f.Persist(ConversationAggregate.Handle(new EditConversationMessageCommand(f.CommandMetadata(F.Human), F.Conversation,
            f.Posting.PublicCommand.MessageId, "Human edit", F.At.AddMinutes(3)), await f.ReplayAsync()));
        var read = await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken);
        read.Participants!.Single(p => p.ParticipantPartyId == F.Human).ParticipantRole.ShouldBe(ParticipantRole.Facilitator);
        var message = read.Messages!.Single();
        message.Text.ShouldBe("Human edit");
        message.Provenance!.HumanEdited.ShouldBeTrue();
        message.Provenance.EditedByPartyId.ShouldBe(F.Human);
        message.Provenance.AgentCallTraceReference.ShouldBe("agent-call-trace");
        ConversationAggregate.Handle(f.Posting, await f.ReplayAsync()).Events.ShouldBeEmpty();
        f.Persist(ConversationAggregate.Handle(new DeleteConversationMessageCommand(f.CommandMetadata(F.Human), F.Conversation,
            message.MessageId, F.At.AddMinutes(4)), await f.ReplayAsync()));
        var deleted = (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Messages!.Single();
        deleted.Deleted.ShouldBeTrue();
        deleted.Text.ShouldBeNull();
        // Governed redaction is covered by the existing aggregate suite; visibility must also honor its source intent.
        f.PersistedEvents.Add(new MessageContentRedactedDomainEvent(f.Metadata(ConversationEventType.MessageContentRedacted, F.Human, F.At.AddMinutes(5)),
            new(Contracts.Governance.GovernedTargetKind.Message, MessageId: message.MessageId),
            Contracts.Governance.RedactionCategory.ContentSuppression, "policy-1",
            "rationale", new Contracts.Governance.GovernanceAuditEvidenceReference(new Contracts.Governance.AuditEvidenceHandle("audit-1"), "policy-1", F.At.AddMinutes(5))));
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Messages!.Single().Redacted.ShouldBeTrue();
    }

    /// <summary>Verifies complete tenant active count includes zero agent calls and missing catalogue is unavailable.</summary>
    [Fact]
    public async Task CompleteTenantActiveCountIncludesZeroAgentCallsAndMissingCatalogueIsUnavailable()
    {
        var f = new F();
        var query = new ConversationActiveCountQuery(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1));
        var unavailable = await f.Queries.CountAsync("agents-service", query, cancellationToken: TestContext.Current.CancellationToken);
        unavailable.Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
        unavailable.Count.ShouldBeNull();
        f.Catalogue = new(ConversationAgentsOutcome.Available, [new(F.Tenant, F.Conversation, F.At, true),
            new(F.Tenant, new ConversationId("zero-agent-calls"), F.At, true), new(F.Tenant, new ConversationId("inactive"), F.At, false)],
            "complete-catalogue-checkpoint", F.At.AddHours(1), true);
        var count = await f.Queries.CountAsync("agents-service", query, cancellationToken: TestContext.Current.CancellationToken);
        count.Count.ShouldBe(2);
        f.Reads.ShouldBe(0);
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        f.Catalogue = f.Catalogue with
        {
            Complete = false
        };
        (await f.Queries.CountAsync("agents-service", query, cancellationToken: TestContext.Current.CancellationToken)).Count.ShouldBeNull();
        f.Catalogue = f.Catalogue with
        {
            Complete = true,
            Entries = [null!]
        };
        (await f.Queries.CountAsync("agents-service", query, TestContext.Current.CancellationToken)).Count.ShouldBeNull();
        f.Catalogue = f.Catalogue with
        {
            Entries = []
        };
        (await f.Queries.CountAsync("agents-service", query, TestContext.Current.CancellationToken)).Count.ShouldBe(0);
    }

    /// <summary>Verifies cross tenant unrelated party and revoked authority fail before disclosure.</summary>
    [Fact]
    public async Task CrossTenantUnrelatedPartyAndRevokedAuthorityFailBeforeDisclosure()
    {
        var f = new F();
        var denied = await f.Queries.ReadAsync("agents-service", new(new TenantId("foreign"), F.Conversation), cancellationToken: TestContext.Current.CancellationToken);
        denied.Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        denied.Messages.ShouldBeNull();
        f.Reads.ShouldBe(0);
        f.WrongOrganization = true;
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        f.Reads.ShouldBe(0);
        f.WrongOrganization = false;
        var wrong = f.Membership with
        {
            PublicCommand = f.Membership.PublicCommand with
            {
                ParticipantPartyId = F.Human
            }
        };
        (await f.Admission.EvaluateAsync(new(new(f.Envelope(wrong), null)), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        f.Reads.ShouldBe(0);
        f.RevokeAfterRead = true;
        var revoked = await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken);
        revoked.Outcome.ShouldBe(ConversationAgentsOutcome.Denied);
        revoked.Messages.ShouldBeNull();
    }

    /// <summary>Verifies approved deletion publication delivery backfill rollover and poison.</summary>
    [Fact]
    public async Task ApprovedDeletionPublicationDeliveryBackfillRolloverAndPoison()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        f.PersistedEvents.Add(new ConversationRejectedDomainEvent(Contracts.Errors.ConversationErrorCode.CommandValidationFailed, "prior-rejection"));
        var before = await f.ReplayAsync();
        before.SourceRevision.ShouldBe(3);
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"), F.Conversation,
            "independent-approval", before.SourceRevision, F.At.AddMinutes(3), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(3))), "approval-event");
        var missingAudit = approval with
        {
            PublicCommand = approval.PublicCommand with
            {
                AuditEvidence = null
            }
        };
        ConversationAggregate.Handle(missingAudit, before).IsRejection.ShouldBeTrue();
        f.Persist(ConversationAggregate.Handle(approval, before));
        var approved = await f.ReplayAsync();
        var signal = approved.DeletionSource.Signal!;
        signal.SourceRevision.ShouldBe(4);
        ConversationAggregate.Handle(approval, approved).Events.ShouldBeEmpty();
        var changedAudit = approval with
        {
            PublicCommand = approval.PublicCommand with
            {
                AuditEvidence = F.DeletionAudit(99, F.At.AddMinutes(3))
            }
        };
        ConversationAggregate.Handle(changedAudit, approved).IsRejection.ShouldBeTrue();
        (await f.Admission.EvaluateAsync(new(new(f.Envelope(changedAudit, "human"), await f.CurrentStateAsync())),
            TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.ConversationDeleted);
        for (int i = 1; i <= 2; i++)
        {
            var replay = await f.ReplayAsync();
            var attempt = new RecordConversationDeletionDelivery(new(f.CommandMetadata(F.Human, $"attempt-{i}"), F.Conversation, signal,
                ConversationDeletionDeliveryAction.Attempt, $"attempt-{i}", $"target-{i}", replay.DeletionSource.DeliveryRevision), $"attempt-event-{i}");
            f.Persist(ConversationAggregate.Handle(attempt, replay));
        }
        var pending = await f.Queries.DeletionSourceAsync("source-worker", new(F.Tenant, F.Conversation, signal.SourceRevision, signal.ConversationDeletionSignalId), cancellationToken: TestContext.Current.CancellationToken);
        pending.Signal.ShouldBe(signal);
        pending.AcknowledgedSourceRevision.ShouldBe(0);
        pending.TargetVersion.ShouldBe("target-2");
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 19, "target-2", "authenticated-local-receipt");
        var ack = new RecordConversationDeletionDelivery(new(f.CommandMetadata(F.Human, "ack"), F.Conversation, signal,
            ConversationDeletionDeliveryAction.Acknowledge, "attempt-2", "target-2", pending.DeliveryRevision, receipt), "ack-event");
        f.Persist(ConversationAggregate.Handle(ack, await f.ReplayAsync()));
        var confirmed = await f.Queries.DeletionSourceAsync("source-worker", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken);
        confirmed.AcknowledgedSourceRevision.ShouldBe(4);
        confirmed.Acknowledgement.ShouldBe(receipt);
        ConversationAggregate.Handle(ack, await f.ReplayAsync()).Events.ShouldBeEmpty();
        var poison = ack with
        {
            PublicCommand = ack.PublicCommand with
            {
                Acknowledgement = receipt with
                {
                    ProtectedDeletionRevision = 20
                }
            }
        };
        f.Persist(ConversationAggregate.Handle(poison, await f.ReplayAsync()));
        var quarantined = await f.ReplayAsync();
        quarantined.DeletionSource.Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined);
        quarantined.DeletionSource.AcknowledgedSourceRevision.ShouldBe(4);
        quarantined.DeletionSource.Acknowledgement.ShouldBe(receipt);
        quarantined.DeletionSource.PoisonCode.ShouldBe("changed-acknowledgement");
    }

    /// <summary>Verifies unauthenticated approval receipt snapshots and mismatched head fail closed.</summary>
    [Fact]
    public async Task UnauthenticatedApprovalReceiptSnapshotsAndMismatchedHeadFailClosed()
    {
        var f = new F();
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(), F.Conversation, "approval", 1, F.At.AddMinutes(1)), "event");
        (await f.Admission.EvaluateAsync(new(new(f.Envelope(approval), null)), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        f.Reads.ShouldBe(0);
        var command = f.Membership;
        var current = await f.CurrentStateAsync();
        var snapshot = current with
        {
            SnapshotState = JsonSerializer.SerializeToElement(await f.ReplayAsync()),
            LastSnapshotSequence = current.CurrentSequence,
            Events = []
        };
        (await f.Admission.EvaluateAsync(new(new(f.Envelope(command), snapshot)), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        var mismatch = current with
        {
            CurrentSequence = current.CurrentSequence + 1
        };
        (await f.Admission.EvaluateAsync(new(new(f.Envelope(command), mismatch)), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
        f.CorruptHead = true;
        (await f.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellationToken: TestContext.Current.CancellationToken)).Outcome.ShouldBe(ConversationAgentsOutcome.Unavailable);
    }

    /// <summary>Verifies current admission and sdk process query are runnable with local fixture.</summary>
    [Fact]
    public async Task CurrentAdmissionAndSdkProcessQueryAreRunnableWithLocalFixture()
    {
        var f = new F();
        var state = await f.CurrentStateAsync();
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(ConversationsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        var services = builder.Services;
        services.AddSingleton<IConversationCommandSourceVerifier>(f);
        services.AddSingleton<IConversationAgentAuthority>(f);
        services.AddSingleton<IConversationDeletionApprovalVerifier>(f);
        services.AddSingleton<IConversationDeletionReceiptVerifier>(f);
        services.AddSingleton<IConversationTenantCatalogue>(f);
        services.AddSingleton<Hexalith.EventStore.Client.Streams.IAuthoritativeEventStreamReader>(f);
        services.AddConversationTenantAccess();
        services.AddConversationQueries(options => options.MaxOffset = 100_000);
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredKeyedService<IDomainProcessor>("conversation").ShouldBeOfType<ConversationAggregate>();
        var result = await DomainServiceRequestRouter.ProcessAsync(scope.ServiceProvider, new(f.Envelope(f.Membership), state), cancellationToken: TestContext.Current.CancellationToken);
        result.Events.Count.ShouldBe(1);
        f.Reads.ShouldBe(1);
        // Only setup read; admission never re-enters source transport.
        var forged = f.Envelope(f.Membership, "human") with
        {
            CommandType = typeof(AddAgentParticipant).AssemblyQualifiedName!
        };
        var refused = await DomainServiceRequestRouter.ProcessAsync(scope.ServiceProvider, new(forged, state),
            cancellationToken: TestContext.Current.CancellationToken);
        refused.IsRejection.ShouldBeTrue();
        f.Reads.ShouldBe(1);
        var query = new QueryEnvelope(F.Tenant.Value, "conversation", F.Conversation.Value, "conversation-agent-read",
            JsonSerializer.SerializeToUtf8Bytes(new ConversationAgentReadQuery(F.Tenant, F.Conversation, ParticipantStateOnly: true), F.Options), "correlation", "agents-service");
        var queried = await DomainQueryDispatcher.ExecuteAsync(scope.ServiceProvider, query, TestContext.Current.CancellationToken);
        queried.Success.ShouldBeTrue();
        queried.GetPayload().Deserialize<ConversationAgentReadResult>(F.Options)!.Outcome.ShouldBe(ConversationAgentsOutcome.Absent);
    }

    /// <summary>Verifies assembly qualified restricted aliases cannot use general human grant.</summary>
    [Fact]
    public async Task AssemblyQualifiedRestrictedAliasesCannotUseGeneralHumanGrant()
    {
        var f = new F();
        foreach (var envelope in new[] {
            f.Envelope(f.Membership, "human") with { CommandType = typeof(AddAgentParticipant).AssemblyQualifiedName! },
            f.Envelope(f.Posting, "human") with { CommandType = typeof(AppendAgentMessage).AssemblyQualifiedName! } })
        {
            var result = await f.Admission.EvaluateAsync(new(new(envelope, null)), TestContext.Current.CancellationToken);
            result.IsRejected.ShouldBeTrue();
        }
        f.Reads.ShouldBe(0);
    }

    /// <summary>Verifies missing production providers and pre cancelled calls fail closed.</summary>
    [Fact]
    public async Task MissingProductionProvidersAndPreCancelledCallsFailClosed()
    {
        var f = new F();
        var service = new ConversationAgentQueryService(new UnavailableConversationAgentAuthority(), new ConversationAgentSourceReader(f), f);
        (await service.ReadAsync("agents-service", new(F.Tenant, F.Conversation), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        f.Reads.ShouldBe(0);
        var providerFailure = new F { FailProvider = true };
        (await providerFailure.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), TestContext.Current.CancellationToken)).Outcome
            .ShouldBe(ConversationAgentsOutcome.Unavailable);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => service.ReadAsync("agents-service", new(F.Tenant, F.Conversation), cancellation.Token));
        var defaultAdmission = new ConversationAgentAdmissionStage(f, f, f, new UnavailableConversationCommandSourceVerifier());
        var current = await f.CurrentStateAsync();
        (await defaultAdmission.EvaluateAsync(new(new(f.Envelope(f.Membership), current)), TestContext.Current.CancellationToken)).IsRejected.ShouldBeTrue();
    }

    /// <summary>Verifies cancellation after ignoring provider cannot return valid result.</summary>
    [Fact]
    public async Task CancellationAfterIgnoringProviderCannotReturnValidResult()
    {
        using var readCancellation = new CancellationTokenSource();
        var read = new F { AfterRead = readCancellation.Cancel };
        await Should.ThrowAsync<OperationCanceledException>(() => read.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), readCancellation.Token));
        using var countCancellation = new CancellationTokenSource();
        var count = new F { AfterCatalogue = countCancellation.Cancel };
        await Should.ThrowAsync<OperationCanceledException>(() => count.Queries.CountAsync("agents-service", new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), countCancellation.Token));
        using var admissionCancellation = new CancellationTokenSource();
        var admission = new F { AfterCommandProof = admissionCancellation.Cancel };
        var current = await admission.CurrentStateAsync();
        await Should.ThrowAsync<OperationCanceledException>(() => admission.Admission.EvaluateAsync(new(new(admission.Envelope(admission.Membership), current)), admissionCancellation.Token));
    }
    /// <summary>Rejects unauthenticated receipt admission and quarantines an authenticated mismatched receipt without advancing the checkpoint.</summary>
    [Fact]
    public async Task ReceiptConflictCannotAdmitOrAdvanceCheckpoint()
    {
        var f = new F();
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(F.Human, "approval"), F.Conversation,
            "independent-approval", 1, F.At.AddMinutes(1), F.DeletionAudit(1, F.At.AddMinutes(1))), "approval-event");
        f.Persist(ConversationAggregate.Handle(approval, await f.ReplayAsync()));
        var signal = (await f.ReplayAsync()).DeletionSource.Signal!;
        var attempt = new RecordConversationDeletionDelivery(new(f.CommandMetadata(F.Human, "attempt"), F.Conversation, signal,
            ConversationDeletionDeliveryAction.Attempt, "attempt", "target", 0), "attempt-event");
        f.Persist(ConversationAggregate.Handle(attempt, await f.ReplayAsync()));
        var receipt = new ConversationDeletionAcknowledgement(signal.ConversationDeletionSignalId, signal.SourceRevision, 7,
            "target", "authenticated-local-receipt");
        var acknowledgement = new RecordConversationDeletionDelivery(new(f.CommandMetadata(F.Human, "ack"), F.Conversation, signal,
            ConversationDeletionDeliveryAction.Acknowledge, "attempt", "target", 1, receipt), "ack-event");
        var current = await f.CurrentStateAsync();
        f.ReceiptOutcome = ConversationAgentsOutcome.Conflict;
        var refused = await f.Admission.EvaluateAsync(new(new(f.Envelope(acknowledgement, "source-worker"), current)),
            TestContext.Current.CancellationToken);
        refused.IsRejected.ShouldBeTrue();
        (await f.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
        f.ReceiptOutcome = ConversationAgentsOutcome.Available;
        var accepted = await f.Admission.EvaluateAsync(new(new(f.Envelope(acknowledgement, "source-worker"), current)),
            TestContext.Current.CancellationToken);
        accepted.IsAccepted.ShouldBeTrue();
        var changedReceipt = acknowledgement with
        {
            PublicCommand = acknowledgement.PublicCommand with
            {
                Acknowledgement = receipt with
                {
                    SourceRevision = receipt.SourceRevision + 1
                }
            }
        };
        f.Persist(ConversationAggregate.Handle(changedReceipt, await f.ReplayAsync()));
        var quarantined = await f.Queries.DeletionSourceAsync("source-worker", new(F.Tenant, F.Conversation), TestContext.Current.CancellationToken);
        quarantined.Outcome.ShouldBe(ConversationAgentsOutcome.Quarantined);
        quarantined.AcknowledgedSourceRevision.ShouldBe(0);
        quarantined.Acknowledgement.ShouldBeNull();
        quarantined.Signal.ShouldBe(signal);
    }

    /// <summary>Folds each additive durable event into legacy current reads and hides approved-deleted source content.</summary>
    [Fact]
    public async Task LegacyProjectionFoldsRestrictedEventsWithoutRetainingDeletedContent()
    {
        var f = new F();
        f.Persist(ConversationAggregate.Handle(f.Membership, await f.ReplayAsync()));
        f.Persist(ConversationAggregate.Handle(f.Posting, await f.ReplayAsync()));
        var original = Project(f);
        original.Detail.Messages.Single().Text.ShouldBe("Original content");
        original.Detail.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        f.Persist(ConversationAggregate.Handle(new EditConversationMessageCommand(f.CommandMetadata(F.Human), F.Conversation,
            f.Posting.PublicCommand.MessageId, "Current human edit", F.At.AddMinutes(3)), await f.ReplayAsync()));
        Project(f).Detail.Messages.Single().Text.ShouldBe("Current human edit");
        f.Persist(ConversationAggregate.Handle(new RemoveAgentParticipant(new(f.CommandMetadata(), F.Conversation, F.Agent,
            F.At.AddMinutes(4)), "removal-event"), await f.ReplayAsync()));
        Project(f).Detail.Participants.ShouldBeEmpty();
        f.Persist(ConversationAggregate.Handle(new DeleteConversationMessageCommand(f.CommandMetadata(F.Human), F.Conversation,
            f.Posting.PublicCommand.MessageId, F.At.AddMinutes(5)), await f.ReplayAsync()));
        Project(f).Detail.Messages.ShouldBeEmpty();
        var beforeApproval = await f.ReplayAsync();
        var approval = new ApproveConversationDeletion(new(f.CommandMetadata(F.Human), F.Conversation,
            "independent-approval", beforeApproval.SourceRevision, F.At.AddMinutes(6),
            F.DeletionAudit(beforeApproval.SourceRevision, F.At.AddMinutes(6))), "approval-event");
        f.Persist(ConversationAggregate.Handle(approval, beforeApproval));
        var signal = (await f.ReplayAsync()).DeletionSource.Signal!;
        f.Persist(ConversationAggregate.Handle(new RecordConversationDeletionDelivery(new(f.CommandMetadata(F.Human), F.Conversation,
            signal, ConversationDeletionDeliveryAction.Attempt, "attempt", "target", 0), "attempt-event"), await f.ReplayAsync()));
        var deleted = Project(f);
        deleted.Detail.LifecycleState.ShouldBe("Closed");
        deleted.Detail.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        deleted.Detail.Messages.ShouldBeEmpty();
        deleted.Detail.Participants.ShouldBeEmpty();
        deleted.Detail.FileReferences.ShouldBeEmpty();
        deleted.Detail.Label.ShouldBeNull();
        deleted.Summary.Freshness.LastAppliedEventPosition.ShouldBe(f.PersistedEvents.Count);
        var publicRead = new ConversationProjectionReadService(new LegacyLocalReadAccess(), new LegacyLocalReadStore(deleted));
        var hidden = await publicRead.ReadDetailAsync(F.Tenant, "human", F.Tenant, F.Conversation, TestContext.Current.CancellationToken);
        hidden.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        hidden.Projection.ShouldBeNull();
        hidden.IsAvailableForTrustBearingActions.ShouldBeFalse();
    }

    private static ConversationProjectedReadModels Project(F fixture)
    {
        var envelopes = fixture.PersistedEvents.Select((e, index) => new ProjectionEventDto(e.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), F.Options), "json", index + 1,
            F.At.AddMinutes(index), "correlation")).ToArray();
        var events = ConversationProjectionEventDecoder.Decode(envelopes);
        return new ConversationProjectionMaterializer().Project(F.Tenant, F.Conversation, events, F.At.AddHours(1), TimeSpan.FromDays(1));
    }





    /// <summary>Caller cancellation completes each new boundary even when the injected provider never completes.</summary>
    [Fact]
    public async Task NeverCompletingProvidersRespectCallerCancellation()
    {
        var authority = new F { PendingAuthority = Pending<ConversationAgentAuthorization>() };
        await VerifyCancellationAsync(token => authority.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), token));
        var source = new F { PendingSourceRead = Pending<Hexalith.EventStore.Contracts.Streams.AuthoritativeStreamReadResult>() };
        await VerifyCancellationAsync(token => source.Queries.ReadAsync("agents-service", new(F.Tenant, F.Conversation), token));
        var catalogue = new F { PendingCatalogue = Pending<ConversationTenantCatalogueResult>() };
        await VerifyCancellationAsync(token => catalogue.Queries.CountAsync("agents-service", new(F.Tenant, F.At.AddDays(-1), F.At.AddDays(1)), token));
        var proof = new F { PendingCommandProof = Pending<Hexalith.EventStore.Contracts.Streams.AuthoritativeStreamReadResult>() };
        var current = await proof.CurrentStateAsync();
        await VerifyCancellationAsync(token => proof.Admission.EvaluateAsync(new(new(proof.Envelope(proof.Membership), current)), token));
        var approval = new F { PendingApproval = Pending<ConversationAgentsOutcome>() };
        var approvalCommand = new ApproveConversationDeletion(new(approval.CommandMetadata(F.Human), F.Conversation,
            "independent-approval", 1, F.At.AddMinutes(1), F.DeletionAudit(1, F.At.AddMinutes(1))), "approval-event");
        await VerifyCancellationAsync(token => approval.Admission.EvaluateAsync(new(new(approval.Envelope(approvalCommand, "human"), null)), token));
        var receipt = new F { PendingReceipt = Pending<ConversationAgentsOutcome>() };
        var delivery = new RecordConversationDeletionDelivery(new(receipt.CommandMetadata(F.Human), F.Conversation,
            new("signal", F.Tenant, F.Conversation, "tenant-alpha:conversation:conversation-alpha", 2, "independent-approval"),
            ConversationDeletionDeliveryAction.Attempt, "attempt", "target", 0), "delivery-event");
        await VerifyCancellationAsync(token => receipt.Admission.EvaluateAsync(new(new(receipt.Envelope(delivery, "source-worker"), null)), token));
    }

    private static Task<T> Pending<T>() => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously).Task;

    private static async Task VerifyCancellationAsync(Func<CancellationToken, Task> operation)
    {
        using var cancellation = new CancellationTokenSource();
        Task pending = operation(cancellation.Token);
        cancellation.Cancel();
        // Test watchdog only; production boundaries use the caller token with no timeout profile.
        await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

}
