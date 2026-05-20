// <copyright file="ConversationIdempotencyOutcomeTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Idempotency;
using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Tests.Idempotency;

/// <summary>
/// Verifies idempotency outcome invariants used by duplicate replay.
/// </summary>
public sealed class ConversationIdempotencyOutcomeTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");

    /// <summary>
    /// P31: Outcome factories require a server-derived audit handle and never fall back to caller correlation.
    /// </summary>
    [Fact]
    public void OutcomeFactoriesShouldRequireAuditHandle()
    {
        string? auditHandle = null;

        ArgumentException exception = Should.Throw<ArgumentException>(() => ConversationIdempotencyOutcome.Success(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            messageId: null,
            participantPartyId: null,
            fileId: null,
            correlationId: "caller-correlation",
            auditHandle: auditHandle!));

        exception.ParamName.ShouldBe("auditHandle");
    }

    /// <summary>
    /// P34: Rejection retryability must match the canonical error-code taxonomy.
    /// </summary>
    [Fact]
    public void RejectionRetryabilityShouldMatchErrorCodeTaxonomy()
    {
        Should.Throw<ArgumentException>(() => ConversationIdempotencyOutcome.Rejection(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            ConversationErrorCode.DuplicateParticipant,
            originalReasonCode: "participant_membership_duplicate",
            isRetryable: true,
            correlationId: "audit-001",
            auditHandle: "audit-001"));

        Should.Throw<ArgumentException>(() => ConversationIdempotencyOutcome.Rejection(
            SchemaVersion.Current,
            Tenant,
            ConversationCommandType.CreateConversationCommand,
            Conversation,
            ConversationErrorCode.ParticipantValidationUnavailable,
            originalReasonCode: "participant_validation_unavailable",
            isRetryable: false,
            correlationId: "audit-001",
            auditHandle: "audit-001"));
    }

    /// <summary>
    /// P34: Pending records cannot carry an outcome because no mutation result is yet authoritative.
    /// </summary>
    [Fact]
    public void PendingRecordShouldRejectOutcome()
    {
        ConversationCommandFingerprint fingerprint = ConversationCommandFingerprint.Create(
            new Hexalith.Conversations.Contracts.Commands.CreateConversationCommand(
                new Hexalith.Conversations.Contracts.Commands.ConversationCommandMetadata(
                    SchemaVersion.Current,
                    Tenant,
                    new PartyId("party-actor"),
                    "correlation-001",
                    "causation-001",
                    "idempotency-001"),
                new BusinessReference("crm", "case-123"),
                new ProjectId("project-001"),
                new FolderId("folder-001"),
                "Case 123"),
            Conversation);

        Should.Throw<ArgumentException>(() => new ConversationIdempotencyRecord(
            fingerprint.Scope,
            fingerprint.PayloadFingerprint,
            ConversationIdempotencyRecordStatus.Pending,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            ConversationIdempotencyRecord.CurrentRecordVersion,
            ConversationIdempotencyOutcome.NoOp(
                SchemaVersion.Current,
                Tenant,
                ConversationCommandType.CreateConversationCommand,
                Conversation,
                correlationId: "audit-001",
                auditHandle: "audit-001")));
    }
}
