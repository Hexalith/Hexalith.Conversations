using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.EventStoreExtensions;
using Hexalith.Conversations.Idempotency;
using Hexalith.EventStore.Contracts.Serialization;
using Hexalith.EventStore.DomainService;

using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

/// <summary>Verifies the internal catalog against actual owner receivers and serialized semantics.</summary>
public sealed class ConversationsCatalogIntentTest
{
    private static readonly DateTimeOffset Occurrence = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly ConversationId Conversation = new("conversation-one");
    private static readonly PartyId Actor = new("party-one");
    private static readonly ConversationCommandMetadata Metadata = new(SchemaVersion.Current, new("tenant-one"), Actor, "correlation-one", "causation-one", "retry-one");
    private static readonly GovernanceAuditEvidenceReference Audit = new(new("audit-one"), "policy-one", Occurrence.AddMinutes(-1));

    /// <summary>Verifies every serialized receiver excludes generated metadata but binds semantic changes and real aliases.</summary>
    [Fact]
    public void EverySerializedReceiverExcludesGeneratedMetadataButBindsSemanticChangesAndRealAliases()
    {
        var cases = Cases();
        cases.Count.ShouldBe(13);
        var adapters = ConversationsCatalogAdapters.Create();
        foreach (var (payload, fields) in cases)
        {
            var adapter = adapters.Single(candidate => candidate.Declaration.Route.Execution!.HandlerType == payload.GetType().FullName);
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), EventStorePayloadSerialization.Options);
            var original = Intent(adapter, bytes);
            foreach ((string path, JsonNode? value) in fields)
            {
                var changed = JsonNode.Parse(bytes)!;
                Set(changed, path, value?.DeepClone());
                Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(changed)).SemanticPayload.ShouldNotBe(original.SemanticPayload,
                    $"{payload.GetType().Name} must bind {path}");
            }
            var retry = JsonNode.Parse(bytes)!;
            string prefix = retry["publicCommand"] is null ? "" : "publicCommand.";
            Set(retry, prefix + "metadata.correlationId", JsonValue.Create("correlation-retry"));
            Set(retry, prefix + "metadata.causationId", JsonValue.Create("causation-retry"));
            if (retry["eventId"] is not null) retry["eventId"] = "generated-event-retry";
            if (payload is CreateConversation) { retry["conversationId"] = "conv:generated-allocation-retry"; retry["createdAt"] = Occurrence.AddMinutes(1); }
            if (payload is AddParticipant) retry["addedAt"] = Occurrence.AddMinutes(1);
            if (payload is ReassignConversationProject) retry["changedAt"] = Occurrence.AddMinutes(1);
            Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(retry)).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, payload.GetType().FullName).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, payload.GetType().AssemblyQualifiedName).SemanticPayload.ShouldBe(original.SemanticPayload);
            Intent(adapter, bytes, aggregate: "conversation-other").CanonicalTarget.ShouldNotBe(original.CanonicalTarget);
            Should.Throw<InvalidOperationException>(() => adapter.CreateIntent(new(adapter.CommandType, "tenant-one", "conversations", "conversation-one", bytes, null)));
            Should.Throw<InvalidOperationException>(() => Intent(adapter, bytes, "ForeignReceiver"));
        }
    }

    /// <summary>Verifies canonical public normalization and original agent posting fingerprint are preserved.</summary>
    [Fact]
    public void CanonicalPublicNormalizationAndOriginalAgentPostingFingerprintArePreserved()
    {
        var create = new CreateConversation(new(Metadata, Label: "Cafe\u0301"), new("allocated-one"), Occurrence, "event-one");
        var adapter = ConversationsCatalogAdapters.Create().Single(candidate => candidate.CommandType == nameof(CreateConversation));
        var first = JsonSerializer.SerializeToUtf8Bytes(create, EventStorePayloadSerialization.Options);
        var second = JsonNode.Parse(first)!; second["publicCommand"]!["label"] = "Café";
        Intent(adapter, first).SemanticPayload.ShouldBe(Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(second)).SemanticPayload);
        var post = Cases().Select(item => item.Payload).OfType<AppendAgentMessage>().Single();
        // Fixed vector independently computed by compiling the unchanged PostFingerprint/AddParts
        // from baseline 7c77a41e; retained source and command are in the post-review evidence.
        ConversationAgentPostFingerprint.Create(post.PublicCommand, post.PostedAt)
            .ShouldBe("a2e253c3212c55a5f58f4fe6c9c4bb7704591d2086e52ecf11bbb80163dfb993");
        var postAdapter = ConversationsCatalogAdapters.Create().Single(candidate => candidate.CommandType == nameof(AppendAgentMessage));
        var intent = Intent(postAdapter, JsonSerializer.SerializeToUtf8Bytes(post, EventStorePayloadSerialization.Options));
        using var descriptor = JsonDocument.Parse(intent.SemanticPayload);
        descriptor.RootElement.GetProperty("intent-sha256").GetString().ShouldBe(CanonicalIntentPayload.Sha256(new
        {
            Metadata = new { post.PublicCommand.Metadata.SchemaVersion, post.PublicCommand.Metadata.TenantId, post.PublicCommand.Metadata.ActorPartyId, post.PublicCommand.Metadata.IdempotencyKey },
            post.PublicCommand.ConversationId,
            Fingerprint = ConversationAgentPostFingerprint.Create(post.PublicCommand, post.PostedAt), post.PublicCommand.OperationTimestamp,
        }));
    }

    [Fact]
    public void MissingRequiredWrapperAndMetadataFieldsAreInvalidArgumentsForEveryReceiver()
    {
        var adapters = ConversationsCatalogAdapters.Create();
        foreach (var (payload, _) in Cases())
        {
            var adapter = adapters.Single(candidate => candidate.Declaration.Route.Execution!.HandlerType == payload.GetType().FullName);
            var original = JsonNode.Parse(JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), EventStorePayloadSerialization.Options))!;
            foreach (string path in original["publicCommand"] is null ? new[] { "metadata" } : new[] { "publicCommand", "publicCommand.metadata" })
            {
                var invalid = original.DeepClone(); Set(invalid, path, null);
                Should.Throw<ArgumentException>(() => Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(invalid)), payload.GetType().Name + " " + path);
                string[] parts = path.Split('.'); JsonNode owner = invalid;
                foreach (string part in parts[..^1]) owner = owner[part]!;
                owner.AsObject().Remove(parts[^1]);
                Should.Throw<ArgumentException>(() => Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(invalid)), payload.GetType().Name + " omitted " + path);
            }
        }
    }

    [Fact]
    public void DemonstratedRequiredActorAndPostIdentityDereferencesRefuseAsInvalidArguments()
    {
        foreach (var (name, path) in new[] { (nameof(AddParticipant), "metadata.actorPartyId"),
            (nameof(AppendAgentMessage), "messageId"), (nameof(AppendAgentMessage), "authorPartyId") })
        {
            var payload = Cases().Select(item => item.Payload).Single(payload => payload.GetType().Name == name);
            var adapter = ConversationsCatalogAdapters.Create().Single(adapter => adapter.CommandType == name);
            var node = JsonNode.Parse(JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType(), EventStorePayloadSerialization.Options))!;
            Set(node["publicCommand"]!, path, null);
            Should.Throw<ArgumentNullException>(() => Intent(adapter, JsonSerializer.SerializeToUtf8Bytes(node)), name + " " + path);
        }
    }

    private static IdempotencyCanonicalIntent Intent(IDeclaredIdempotencyIntentAdapter adapter, byte[] bytes, string? wire = null, string aggregate = "conversation-one")
        => adapter.CreateIntent(new(wire ?? adapter.CommandType, "tenant-one", "conversation", aggregate, bytes, null));

    private static void Set(JsonNode node, string path, JsonNode? value)
    {
        string[] parts = path.Split('.');
        foreach (string part in parts[..^1]) node = node[part]!;
        node[parts[^1]] = value;
    }

    private static IReadOnlyList<(object Payload, IReadOnlyList<(string Path, JsonNode? Value)> Fields)> Cases()
    {
        var signal = new ConversationDeletionSignal("signal-one", Metadata.TenantId, Conversation, "stream-one", 4, "approval-one");
        var acknowledgement = new ConversationDeletionAcknowledgement("signal-one", 4, 7, "target-one", "evidence-one");
        AddParticipantCommand add = new(Metadata, Conversation, Actor, ParticipantType.AiAgent, ParticipantRole.Member,
            new("provider-one", "model", SchemaVersion.Current), Occurrence);
        AppendMessageCommand post = new(Metadata, Conversation, new("message-one"), Actor, "original-text",
            new("provider-one", "model", SchemaVersion.Current), new(SchemaVersion.Current, ClientName: "client-one"),
            new("trace-one", true, false), Occurrence);
        List<(object, IReadOnlyList<(string, JsonNode?)>)> cases = [
            (new CreateConversation(new(Metadata, Label: "label-one", CallerMetadata: new(SchemaVersion.Current, ClientName: "client-one")), new("allocated-one"), Occurrence, "event-one"),
                [("publicCommand.label", JsonValue.Create("label-two")), ("publicCommand.callerMetadata.clientName", JsonValue.Create("client-two"))]),
            (new AddParticipant(add, Occurrence, "event-one"), [("publicCommand.participantPartyId", JsonValue.Create("party:party-two")), ("publicCommand.providerCorrelation.providerName", JsonValue.Create("provider-two"))]),
            (new ReassignConversationProject(new(Metadata, Conversation, new(ConversationProjectAssignmentOperation.Assign, new("project-one"))), Occurrence, "event-one"), [("publicCommand.target.projectId", JsonValue.Create("project:project-two"))]),
            (new SetConversationRetentionPolicy(new(Metadata, Conversation, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1))), ("auditEvidence.handle.value", JsonValue.Create("audit-two"))]),
            (new MarkConversationContentSensitive(new(Metadata, Conversation, new(GovernedTargetKind.Message, new("message-one")), SensitivityCategory.Sensitive, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.target.messageId", JsonValue.Create("message:message-two")), ("auditEvidence.policyReference", JsonValue.Create("policy-two"))]),
            (new RedactMessageContent(new(Metadata, Conversation, new(GovernedTargetKind.Message, new("message-one")), RedactionCategory.DisplayMask, "policy-one", "rationale-one", Occurrence), Audit, "event-one"), [("publicCommand.target.messageId", JsonValue.Create("message:message-two")), ("auditEvidence.capturedAt", JsonValue.Create(Occurrence.AddMinutes(-2)))]),
            (new AddAgentParticipant(add, Occurrence, "event-one"), [("addedAt", JsonValue.Create(Occurrence.AddSeconds(1))), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new RemoveAgentParticipant(new(Metadata, Conversation, Actor, Occurrence), "event-one"), [("publicCommand.participantPartyId", JsonValue.Create("party:party-two")), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new AppendAgentMessage(post, Occurrence, "event-one"), [("postedAt", JsonValue.Create(Occurrence.AddSeconds(1))), ("publicCommand.agentProvenance.agentCallTraceReference", JsonValue.Create("trace-two")), ("publicCommand.providerCorrelation.providerName", JsonValue.Create("provider-two")), ("publicCommand.callerMetadata.clientName", JsonValue.Create("client-two")), ("publicCommand.text", JsonValue.Create("changed-text"))]),
            (new ApproveConversationDeletion(new(Metadata, Conversation, "approval-one", 3, Occurrence, Audit), "event-one"), [("publicCommand.approvalReference", JsonValue.Create("approval-two")), ("publicCommand.sourceRevision", JsonValue.Create(4L)), ("publicCommand.auditEvidence.handle.value", JsonValue.Create("audit-two")), ("publicCommand.operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new RecordConversationDeletionDelivery(new(Metadata, Conversation, signal, ConversationDeletionDeliveryAction.Acknowledge, "attempt-one", "target-one", 2, acknowledgement), "event-one"), [("publicCommand.deliveryAttemptId", JsonValue.Create("attempt-two")), ("publicCommand.targetVersion", JsonValue.Create("target-two")), ("publicCommand.expectedDeliveryRevision", JsonValue.Create(3L)), ("publicCommand.signal.sourceRevision", JsonValue.Create(5L)), ("publicCommand.acknowledgement.evidence", JsonValue.Create("evidence-two"))]),
            (new EditConversationMessageCommand(Metadata, Conversation, new("message-one"), "text-one", Occurrence), [("text", JsonValue.Create("text-two")), ("messageId", JsonValue.Create("message:message-two")), ("operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
            (new DeleteConversationMessageCommand(Metadata, Conversation, new("message-one"), Occurrence), [("messageId", JsonValue.Create("message:message-two")), ("operationTimestamp", JsonValue.Create(Occurrence.AddSeconds(1)))]),
        ];
        return cases.Select(item =>
        {
            string prefix = item.Item1 is EditConversationMessageCommand or DeleteConversationMessageCommand ? "" : "publicCommand.";
            return (item.Item1, (IReadOnlyList<(string, JsonNode?)>)[.. item.Item2,
                (prefix + "metadata.actorPartyId", JsonValue.Create("party:party-other")), (prefix + "metadata.tenantId", JsonValue.Create("tenant:tenant-other"))]);
        }).ToArray();
    }
}
