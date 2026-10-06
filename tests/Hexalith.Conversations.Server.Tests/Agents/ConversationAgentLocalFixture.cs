// <copyright file="ConversationAgentLocalFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Agents;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Local serialized persisted-event simulation, never live storage or production authority.</summary>
public sealed class ConversationAgentLocalFixture : IConversationAgentAuthority, IAuthoritativeEventStreamReader,
    IConversationDeletionApprovalVerifier, IConversationDeletionReceiptVerifier, IConversationTenantCatalogue, IConversationCommandSourceVerifier
{
    /// <summary>Exact local tenant scope.</summary>
    public static readonly TenantId Tenant = new("tenant-alpha");
    /// <summary>Exact local Conversation source.</summary>
    public static readonly ConversationId Conversation = new("conversation-alpha");
    /// <summary>Immutable local Organization Party.</summary>
    public static readonly PartyId Agent = new("immutable-organization-agent");
    /// <summary>Independent local human Party.</summary>
    public static readonly PartyId Human = new("human-party");
    /// <summary>Stable local operation timestamp.</summary>
    public static readonly DateTimeOffset At = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
    /// <summary>SDK-compatible local JSON settings.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    /// <summary>Serialized replay inputs for the local persisted-event simulation.</summary>
    public List<object> PersistedEvents
    {
        get;
    } = [];
    /// <summary>Number of authoritative query source reads.</summary>
    public int Reads
    {
        get;
        private set;
    }
    /// <summary>Number of exact current authority checks.</summary>
    public int AuthorityCalls
    {
        get;
        private set;
    }
    /// <summary>Cancellation hook for a query provider that ignores cancellation.</summary>
    public Action? AfterRead
    {
        get;
        set;
    }
    /// <summary>Cancellation hook for a catalogue provider that ignores cancellation.</summary>
    public Action? AfterCatalogue
    {
        get;
        set;
    }
    /// <summary>Cancellation hook for a command-prefix provider that ignores cancellation.</summary>
    public Action? AfterCommandProof
    {
        get;
        set;
    }
    /// <summary>Gets or sets an injected never-completing local authority task for caller-cancellation proof.</summary>
    public Task<ConversationAgentAuthorization>? PendingAuthority
    {
        get; set;
    }
    /// <summary>Gets or sets an injected never-completing local sourceread task for caller-cancellation proof.</summary>
    public Task<AuthoritativeStreamReadResult>? PendingSourceRead
    {
        get; set;
    }
    /// <summary>Gets or sets an injected never-completing local commandproof task for caller-cancellation proof.</summary>
    public Task<AuthoritativeStreamReadResult>? PendingCommandProof
    {
        get; set;
    }
    /// <summary>Gets or sets an injected never-completing local catalogue task for caller-cancellation proof.</summary>
    public Task<ConversationTenantCatalogueResult>? PendingCatalogue
    {
        get; set;
    }
    /// <summary>Gets or sets an injected never-completing local approval task for caller-cancellation proof.</summary>
    public Task<ConversationAgentsOutcome>? PendingApproval
    {
        get; set;
    }
    /// <summary>Gets or sets an injected never-completing local receipt task for caller-cancellation proof.</summary>
    public Task<ConversationAgentsOutcome>? PendingReceipt
    {
        get; set;
    }
    /// <summary>Whether local authority is denied.</summary>
    public bool Deny
    {
        get;
        set;
    }
    /// <summary>Whether authority is revoked after an awaited read.</summary>
    public bool RevokeAfterRead
    {
        get;
        set;
    }
    /// <summary>Whether immutable Organization identity evidence is absent.</summary>
    public bool WrongOrganization
    {
        get;
        set;
    }
    /// <summary>Whether query provider failure is simulated.</summary>
    public bool FailProvider
    {
        get;
        set;
    }
    /// <summary>Whether source head evidence is incomplete.</summary>
    public bool CorruptHead
    {
        get;
        set;
    }
    /// <summary>Independent local approval verification result.</summary>
    public ConversationAgentsOutcome ApprovalOutcome
    {
        get;
        set;
    } = ConversationAgentsOutcome.Available;
    /// <summary>Independent local receiver verification result.</summary>
    public ConversationAgentsOutcome ReceiptOutcome
    {
        get;
        set;
    } = ConversationAgentsOutcome.Available;
    /// <summary>Complete local tenant catalogue evidence.</summary>
    public ConversationTenantCatalogueResult Catalogue
    {
        get;
        set;
    } = new(ConversationAgentsOutcome.Unavailable);

    /// <summary>Creates one serialized local source creation event.</summary>
    public ConversationAgentLocalFixture()
        => PersistedEvents.Add(new ConversationCreatedDomainEvent(Metadata(ConversationEventType.ConversationCreated, Human, At), null, null, null, null));

    /// <summary>Current source query service bound to local ports.</summary>
    public ConversationAgentQueryService Queries => new(this, new ConversationAgentSourceReader(this), this);
    /// <summary>SDK admission bound to local ports.</summary>
    public ConversationAgentAdmissionStage Admission => new(this, this, this, this);
    /// <summary>Creates stable local command identity.</summary>
    public ConversationCommandMetadata CommandMetadata(PartyId? actor = null, string key = "intent-1")
        => new(SchemaVersion.Current, Tenant, actor ?? Agent, "correlation-1", IdempotencyKey: key);
    /// <summary>Exact restricted AiAgent Member intent.</summary>
    public AddAgentParticipant Membership => new(new(CommandMetadata(), Conversation, Agent,
        ParticipantType.AiAgent, ParticipantRole.Member), At.AddMinutes(1), "membership-event");
    /// <summary>Exact deterministic posting intent.</summary>
    public AppendAgentMessage Posting => new(new(CommandMetadata(key: "post-key"), Conversation, new MessageId("message-original"), Agent,
        "Original content", AgentProvenance: new("agent-call-trace", true, false), OperationTimestamp: At.AddMinutes(2)), At.AddMinutes(2), "post-event");

    /// <summary>Appends local domain results to serialized replay inputs.</summary>
    public void Persist(DomainResult result)
    {
        foreach (var e in result.Events)
        {
            PersistedEvents.Add(e);
        }
    }

    /// <summary>Replays a fresh state from serialized local events.</summary>
    public async Task<ConversationState> ReplayAsync()
        => (await new ConversationAgentSourceReader(this).ReadAsync(Tenant, Conversation))!.Value.State;

    /// <summary>Creates synthetic audit evidence bound to an exact local approval source; never production evidence.</summary>
    public static GovernanceAuditEvidenceReference DeletionAudit(long sourceRevision, DateTimeOffset occurrence)
        => new(new AuditEvidenceHandle($"local-approval-source-{sourceRevision}"), "local-deletion-policy", occurrence);

    /// <summary>Creates exact local event scope and identity.</summary>
    public ConversationEventMetadata Metadata(ConversationEventType type, PartyId actor, DateTimeOffset occurrence)
        => new(SchemaVersion.Current, $"event-{PersistedEvents.Count + 1}", type, Tenant, Conversation, "correlation-1", occurrence, actor);

    /// <inheritdoc />
    public Task<ConversationAgentAuthorization> AuthorizeAsync(string authenticatedPrincipalId, TenantId tenantId,
        ConversationId? conversationId, string operation, CancellationToken cancellationToken = default)
    {
        if (FailProvider)
        {
            throw new IOException("Local unavailable authority.");
        }
        AuthorityCalls++;
        if (PendingAuthority is not null)
        {
            return PendingAuthority;
        }
        bool deny = Deny || tenantId != Tenant || (conversationId is not null && conversationId != Conversation)
            || (RevokeAfterRead && Reads > 0) || (operation == "GeneralCommand" && authenticatedPrincipalId == "agents-service")
            || (operation == "DeletionSource" && authenticatedPrincipalId != "source-worker")
            || (operation == "HumanMessageMutation" && authenticatedPrincipalId != "human");
        if (deny)
        {
            return Task.FromResult(new ConversationAgentAuthorization(ConversationAgentsOutcome.Denied));
        }
        if (authenticatedPrincipalId is not ("agents-service" or "human" or "source-worker"))
        {
            return Task.FromResult(new ConversationAgentAuthorization(ConversationAgentsOutcome.Denied));
        }
        return Task.FromResult(new ConversationAgentAuthorization(ConversationAgentsOutcome.Available, Tenant, authenticatedPrincipalId,
            authenticatedPrincipalId == "source-worker" ? null : authenticatedPrincipalId == "agents-service" ? Agent : Human, "current-authority-1",
            authenticatedPrincipalId == "agents-service" && !WrongOrganization));
    }

    /// <inheritdoc />
    public Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, ApproveConversationDeletionCommand command,
        CancellationToken cancellationToken = default)
        => PendingApproval ?? Task.FromResult(envelope.UserId == "human" && command.ApprovalReference == "independent-approval"
            && command.Metadata.ActorPartyId == Human && command.AuditEvidence == DeletionAudit(command.SourceRevision, command.OperationTimestamp)
            ? ApprovalOutcome : ConversationAgentsOutcome.Denied);

    /// <inheritdoc />
    public Task<ConversationAgentsOutcome> VerifyAsync(CommandEnvelope envelope, RecordConversationDeletionDeliveryCommand command,
        CancellationToken cancellationToken = default)
        => PendingReceipt ?? Task.FromResult(envelope.UserId == "source-worker" ? ReceiptOutcome : ConversationAgentsOutcome.Denied);

    /// <inheritdoc />
    public Task<ConversationTenantCatalogueResult> ReadAsync(ConversationActiveCountQuery query, CancellationToken cancellationToken = default)
    {
        if (PendingCatalogue is not null)
        {
            return PendingCatalogue;
        }
        AfterCatalogue?.Invoke();
        return Task.FromResult(Catalogue);
    }

    /// <inheritdoc />
    public Task<AuthoritativeStreamReadResult> VerifyAsync(DomainServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (PendingCommandProof is not null)
        {
            return PendingCommandProof;
        }
        var source = BuildSource(request.Command.AggregateIdentity);
        AfterCommandProof?.Invoke();
        return Task.FromResult(source);
    }

    /// <inheritdoc />
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)
    {
        Reads++;
        if (PendingSourceRead is not null)
        {
            return PendingSourceRead;
        }
        var source = BuildSource(identity);
        AfterRead?.Invoke();
        return Task.FromResult(source);
    }

    private AuthoritativeStreamReadResult BuildSource(AggregateIdentity identity)
    {
        var events = PersistedEvents.Select((e, index) => new StreamReadEvent(index + 1, e.GetType().FullName!,
            JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), Options), "json", 1, $"persisted-{index + 1}",
            "correlation-1", null, At.AddMinutes(index), "human")).ToArray();
        return new AuthoritativeStreamReadResult(new AuthoritativeEventStream(identity,
            events.Length + (CorruptHead ? 1 : 0), At.AddHours(1), events, "stable-source-observation"), null);
    }

    /// <summary>Builds the complete SDK prefix for local transport simulation.</summary>
    public async Task<DomainServiceCurrentState> CurrentStateAsync()
    {
        var source = (await ReadAsync(new AggregateIdentity(Tenant.Value, "conversation", Conversation.Value))).Stream!;
        var events = source.Events.Select(e => new EventEnvelope(new EventMetadata(e.MessageId, Conversation.Value, "Conversation",
            Tenant.Value, "conversation", e.SequenceNumber, 0, e.Timestamp, "correlation-1", "causation-1", "human", "local-fixture",
            e.EventTypeName, 1, "json"), e.Payload, null)).ToArray();
        return new(null, events, 0, source.Head);
    }

    /// <summary>Builds the authenticated local SDK envelope.</summary>
    public CommandEnvelope Envelope<T>(T command, string principal = "agents-service")
        => new("command-1", Tenant.Value, "conversation", Conversation.Value, typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command, Options), "correlation-1", null, principal, null);
}
