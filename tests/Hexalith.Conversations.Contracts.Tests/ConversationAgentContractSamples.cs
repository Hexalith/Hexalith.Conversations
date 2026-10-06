// <copyright file="ConversationAgentContractSamples.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Projections;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>Portable fixtures for every additive restricted Agents public contract.</summary>
internal static class ConversationAgentContractSamples
{
    private static readonly DateTimeOffset At = new(2026, 10, 6, 10, 0, 0, TimeSpan.Zero);
    private static readonly AgentMessageProvenance Provenance = new("opaque-agent-call", true, false);
    private static readonly ConversationDeletionSignal Signal = new("signal-opaque", ContractSamples.Tenant,
        ContractSamples.Conversation, "tenant-001:conversations:conversation-001", 4, "independent-approval");
    private static readonly ConversationDeletionAcknowledgement Receipt = new("signal-opaque", 4, 7, "receiver-target", "opaque-authenticated-evidence");

    /// <summary>Gets additive fixtures while preserving every pre-existing golden wire expectation.</summary>
    internal static IReadOnlyList<object> All =>
    [
        Provenance,
        new ApproveConversationDeletionCommand(ContractSamples.CommandMetadata, ContractSamples.Conversation, "independent-approval", 3, At, new(new AuditEvidenceHandle("local-audit"), "local-policy", At)),
        new ConversationActiveCountQuery(ContractSamples.Tenant, At.AddDays(-1), At.AddDays(1)),
        new ConversationActiveCountResult(ConversationAgentsOutcome.Available, 2, "complete-catalogue", At,
            ContractSamples.Tenant, At.AddDays(-1), At.AddDays(1)),
        new ConversationAgentCommandResult(ConversationAgentsOutcome.Available, "gateway-command", ContractSamples.Message),
        new ConversationAgentMessage(ContractSamples.Message, ContractSamples.Actor, "safe local fixture", At, null, false, false, Provenance),
        new ConversationAgentReadQuery(ContractSamples.Tenant, ContractSamples.Conversation, ContractSamples.Message),
        new ConversationAgentReadResult(ConversationAgentsOutcome.Available, ContractSamples.Tenant, ContractSamples.Conversation,
            3, At, "source-observation", [new ConversationParticipantProjectionV1(ContractSamples.Actor,
                Contracts.Participants.ParticipantType.AiAgent, Contracts.Participants.ParticipantRole.Member)],
            [new ConversationAgentMessage(ContractSamples.Message, ContractSamples.Actor, "safe local fixture", At, null, false, false, Provenance)], true),
        Receipt,
        Signal,
        new ConversationDeletionSourceQuery(ContractSamples.Tenant, ContractSamples.Conversation, 4, "signal-opaque"),
        new ConversationDeletionSourceResult(ConversationAgentsOutcome.Available, Signal, 2, 4, Receipt, "attempt", "receiver-target"),
        new DeleteConversationMessageCommand(ContractSamples.CommandMetadata, ContractSamples.Conversation, ContractSamples.Message, At),
        new EditConversationMessageCommand(ContractSamples.CommandMetadata, ContractSamples.Conversation, ContractSamples.Message, "edited local fixture", At),
        new RecordConversationDeletionDeliveryCommand(ContractSamples.CommandMetadata, ContractSamples.Conversation, Signal,
            ConversationDeletionDeliveryAction.Acknowledge, "attempt", "receiver-target", 1, Receipt),
        new RemoveAgentParticipantCommand(ContractSamples.CommandMetadata, ContractSamples.Conversation, ContractSamples.Actor, At),
        new AgentParticipantRemoved(EventMetadata(ConversationEventType.AgentParticipantRemoved), ContractSamples.Actor),
        new MessageEdited(EventMetadata(ConversationEventType.MessageEdited), ContractSamples.Message, "edited local fixture", ContractSamples.Actor),
        new MessageDeleted(EventMetadata(ConversationEventType.MessageDeleted), ContractSamples.Message),
        new ConversationDeletionApproved(EventMetadata(ConversationEventType.ConversationDeletionApproved), Signal,
            new(new AuditEvidenceHandle("local-audit"), "local-policy", At)),
        new ConversationDeletionDeliveryRecorded(EventMetadata(ConversationEventType.ConversationDeletionDeliveryRecorded), Signal,
            ConversationDeletionDeliveryAction.Acknowledge, "attempt", "receiver-target", 1, Receipt),
        .. Enum.GetValues<ConversationAgentsOutcome>().Cast<object>(),
        .. Enum.GetValues<ConversationDeletionDeliveryAction>().Cast<object>(),
    ];
    private static ConversationEventMetadata EventMetadata(ConversationEventType type)
        => new(SchemaVersion.Current, $"local-{type.Value}", type, ContractSamples.Tenant, ContractSamples.Conversation,
            "correlation", At, ContractSamples.Actor);
}
