// <copyright file="GovernanceAuditPairingSafetyNetTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;

namespace Hexalith.Conversations.Server.Tests.Governance;

/// <summary>
/// Release-gate inventory for governance mutation paths that must stay audit-paired.
/// </summary>
public sealed class GovernanceAuditPairingSafetyNetTest
{
    /// <summary>
    /// Every implemented governance mutation path is intentionally listed under the audit-pairing safety net.
    /// </summary>
    [Fact]
    public void ImplementedGovernanceMutationPathsShouldRemainExplicit()
    {
        ImplementedGovernanceMutationPath[] paths =
        {
            new(
                typeof(SetConversationRetentionPolicyCommandHandler),
                typeof(SetConversationRetentionPolicy),
                typeof(RetentionPolicySetDomainEvent),
                GovernanceOperationKind.SetRetentionPolicy),
            new(
                typeof(SetConversationRetentionPolicyCommandHandler),
                typeof(SetConversationRetentionPolicy),
                typeof(RetentionPolicyReplacedDomainEvent),
                GovernanceOperationKind.ReplaceRetentionPolicy),
            new(
                typeof(MarkConversationContentSensitiveCommandHandler),
                typeof(MarkConversationContentSensitive),
                typeof(ConversationContentMarkedSensitiveDomainEvent),
                GovernanceOperationKind.MarkContentSensitive),
            new(
                typeof(RedactMessageContentCommandHandler),
                typeof(RedactMessageContent),
                typeof(MessageContentRedactedDomainEvent),
                GovernanceOperationKind.RedactMessageContent),
        };

        paths.Select(path => path.HandlerType).Distinct().ShouldBe(new[]
        {
            typeof(SetConversationRetentionPolicyCommandHandler),
            typeof(MarkConversationContentSensitiveCommandHandler),
            typeof(RedactMessageContentCommandHandler),
        }, ignoreOrder: true);
        paths.Select(path => path.AggregateCommandType).Distinct().ShouldBe(new[]
        {
            typeof(SetConversationRetentionPolicy),
            typeof(MarkConversationContentSensitive),
            typeof(RedactMessageContent),
        }, ignoreOrder: true);
        paths.Select(path => path.MutationEventType).ShouldBe(new[]
        {
            typeof(RetentionPolicySetDomainEvent),
            typeof(RetentionPolicyReplacedDomainEvent),
            typeof(ConversationContentMarkedSensitiveDomainEvent),
            typeof(MessageContentRedactedDomainEvent),
        }, ignoreOrder: true);
        paths.Select(path => path.OperationKind).ShouldBe(new[]
        {
            GovernanceOperationKind.SetRetentionPolicy,
            GovernanceOperationKind.ReplaceRetentionPolicy,
            GovernanceOperationKind.MarkContentSensitive,
            GovernanceOperationKind.RedactMessageContent,
        }, ignoreOrder: true);

        Type[] implementedGovernanceCommands = paths.Select(path => path.AggregateCommandType).Distinct().ToArray();
        Type[] aggregateGovernanceCommandTypes = typeof(ConversationAggregate).GetMethods()
            .Where(method => method.Name == nameof(ConversationAggregate.Handle))
            .SelectMany(method => method.GetParameters().Take(1).Select(parameter => parameter.ParameterType))
            .Where(HasRequiredGovernanceAuditEvidence)
            .Distinct()
            .ToArray();

        aggregateGovernanceCommandTypes.ShouldBe(implementedGovernanceCommands, ignoreOrder: true);
    }

    /// <summary>
    /// Future governance vocabulary remains prepared but outside the implemented mutation inventory.
    /// </summary>
    [Fact]
    public void FutureGovernanceVocabularyShouldNotAppearAsImplementedMutationPaths()
    {
        GovernanceOperationKind[] implemented =
        {
            GovernanceOperationKind.SetRetentionPolicy,
            GovernanceOperationKind.ReplaceRetentionPolicy,
            GovernanceOperationKind.MarkContentSensitive,
            GovernanceOperationKind.RedactMessageContent,
        };
        GovernanceOperationKind[] futureOnly =
        {
            GovernanceOperationKind.ArchiveConversation,
            GovernanceOperationKind.LogicallyDeleteConversation,
            GovernanceOperationKind.DeferForLegalHold,
            GovernanceOperationKind.GovernAuditRecord,
            GovernanceOperationKind.RecordPrivilegedJustification,
        };

        foreach (GovernanceOperationKind operationKind in futureOnly)
        {
            implemented.ShouldNotContain(operationKind);
        }
    }

    /// <summary>
    /// Non-governance command paths stay explicit and do not depend on the governance audit sink.
    /// </summary>
    [Fact]
    public void NonGovernanceConversationActivityShouldRemainOutsideAuditDegradationHandling()
    {
        Type[] nonGovernanceAggregateCommands = typeof(ConversationAggregate).GetMethods()
            .Where(method => method.Name == nameof(ConversationAggregate.Handle))
            .SelectMany(method => method.GetParameters().Take(1).Select(parameter => parameter.ParameterType))
            .Where(type => !HasRequiredGovernanceAuditEvidence(type))
            .Distinct()
            .ToArray();

        nonGovernanceAggregateCommands.ShouldBe(new[]
        {
            typeof(CreateConversation),
            typeof(AddParticipant),
        }, ignoreOrder: true);

        typeof(AddParticipantCommandHandler).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ShouldNotContain(typeof(IConversationGovernanceAuditService));
    }

    private sealed record ImplementedGovernanceMutationPath(
        Type HandlerType,
        Type AggregateCommandType,
        Type MutationEventType,
        GovernanceOperationKind OperationKind);

    private static bool HasRequiredGovernanceAuditEvidence(Type type)
        => type.GetProperty(nameof(SetConversationRetentionPolicy.AuditEvidence))?.PropertyType == typeof(GovernanceAuditEvidenceReference);
}
