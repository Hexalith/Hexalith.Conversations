// <copyright file="GovernanceAuditPairingSafetyNetTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Server.Api;
using Hexalith.Conversations.Server.CommandHandlers;
using Hexalith.Conversations.Server.Governance;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;

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
            GovernanceOperationKind.RecordPrivilegedJustification,
        };
        GovernanceOperationKind[] futureOnly =
        {
            GovernanceOperationKind.ArchiveConversation,
            GovernanceOperationKind.LogicallyDeleteConversation,
            GovernanceOperationKind.DeferForLegalHold,
            GovernanceOperationKind.GovernAuditRecord,
        };

        foreach (GovernanceOperationKind operationKind in futureOnly)
        {
            implemented.ShouldNotContain(operationKind);
        }
    }

    /// <summary>
    /// Privileged justification is an implemented audit boundary, not an aggregate mutation path.
    /// </summary>
    [Fact]
    public void PrivilegedJustificationShouldBeImplementedAsPreconditionAuditBoundary()
    {
        typeof(ConversationPrivilegedOperationalJustificationService).GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ShouldContain(typeof(IConversationGovernanceAuditService));

        typeof(IConversationGovernanceAuditService).GetMethods()
            .Select(method => method.Name)
            .ShouldContain(nameof(IConversationGovernanceAuditService.RecordPrivilegedOperationalJustificationAsync));
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

    /// <summary>
    /// Read-only workspace boundaries must not depend directly on mutation handlers, audit gates, or idempotency mutation paths.
    /// </summary>
    [Fact]
    public void ReadOnlyWorkspaceBoundariesShouldNotReferenceMutationExecutionTypes()
    {
        Type[] readOnlyBoundaries =
        [
            typeof(ConversationReadApi),
            typeof(ConversationQueryHandler),
            typeof(ConversationProjectionReadService),
            typeof(ConversationCitationAccessService),
            typeof(ConversationTemporalReconstructionService),
            typeof(ConversationAuditRecordAccessService),
        ];
        Type[] forbiddenMutationTypes =
        [
            typeof(SetConversationRetentionPolicyCommandHandler),
            typeof(MarkConversationContentSensitiveCommandHandler),
            typeof(RedactMessageContentCommandHandler),
            typeof(AddParticipantCommandHandler),
            typeof(IdempotentConversationCommandExecutor),
            typeof(ConversationGovernanceAuditGate),
        ];

        foreach (Type readOnlyBoundary in readOnlyBoundaries)
        {
            Type[] directDependencies = DirectDependencies(readOnlyBoundary);
            foreach (Type forbidden in forbiddenMutationTypes)
            {
                directDependencies.ShouldNotContain(forbidden);
            }
        }
    }

    private sealed record ImplementedGovernanceMutationPath(
        Type HandlerType,
        Type AggregateCommandType,
        Type MutationEventType,
        GovernanceOperationKind OperationKind);

    private static bool HasRequiredGovernanceAuditEvidence(Type type)
        => type.GetProperty(nameof(SetConversationRetentionPolicy.AuditEvidence))?.PropertyType == typeof(GovernanceAuditEvidenceReference);

    private static Type[] DirectDependencies(Type type)
        =>
        [
            .. type.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType),
            .. type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(field => field.FieldType),
            .. type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Select(property => property.PropertyType),
        ];
}
