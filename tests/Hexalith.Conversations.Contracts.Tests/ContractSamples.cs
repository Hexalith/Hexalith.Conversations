// <copyright file="ContractSamples.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Results;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Tests;

internal static class ContractSamples
{
    internal static readonly SchemaVersion Version = new(1);
    internal static readonly TenantId Tenant = new("tenant-001");
    internal static readonly ConversationId Conversation = new("conversation-001");
    internal static readonly PartyId Actor = new("party-actor");
    internal static readonly PartyId Participant = new("party-participant");
    internal static readonly MessageId Message = new("message-001");
    internal static readonly ProjectId Project = new("project-001");
    internal static readonly FolderId Folder = new("folder-001");
    internal static readonly FileId File = new("file-001");
    internal static readonly BusinessReference Business = new("crm", "case-123");

    internal static readonly ProviderCorrelationMetadata ProviderCorrelation = new(
        "provider-a",
        "assistant",
        Version,
        "session-reference",
        "response-reference",
        new Dictionary<string, string>
        {
            ["region"] = "eu",
        });

    internal static readonly ConversationCommandMetadata CommandMetadata = new(
        Version,
        Tenant,
        Actor,
        "correlation-001",
        "causation-001",
        "idempotency-001");

    internal static readonly ConversationEventMetadata EventMetadata = new(
        Version,
        "event-001",
        ConversationEventType.ConversationCreated,
        Tenant,
        Conversation,
        "correlation-001",
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        Actor,
        "causation-001");

    internal static readonly ConversationEventMetadata EventMetadataWithoutCausation = new(
        Version,
        "event-002",
        ConversationEventType.ConversationCreated,
        Tenant,
        Conversation,
        "correlation-002",
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        Actor);

    internal static readonly ConversationEventMetadata ParticipantEventMetadata = new(
        Version,
        "event-participant-001",
        ConversationEventType.ParticipantAdded,
        Tenant,
        Conversation,
        "correlation-001",
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        Actor,
        "causation-001");

    internal static readonly ProjectionFreshness Freshness = new(
        ProjectionTrustState.Current,
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        Version,
        "Visible after accepted writes are projected.");

    internal static readonly ProjectionFreshnessV1 FreshnessV1 = new(
        Version,
        "pos:0000000042",
        42,
        new DateTimeOffset(2026, 5, 18, 11, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 5, 18, 11, 0, 5, TimeSpan.Zero),
        TimeSpan.FromSeconds(5),
        IsStale: false,
        ProjectionTrustState.Current,
        ProjectionFreshnessReasonCode.Current);

    internal static readonly ReadModelVisibility Visibility = new(
        ProjectionTrustState.Stale,
        "Read models may lag immediately after command acceptance.");

    internal static IReadOnlyList<object> AllContracts =>
    [
        Version,
        new ContractVersionInfo("Conversations", Version, Version),
        new UnsupportedSchemaVersion(new SchemaVersion(2), Version, Version),
        ConversationCommandType.CreateConversationCommand,
        ConversationEventType.ConversationCreated,
        ParticipantType.Human,
        ParticipantRole.Member,
        ConversationErrorCode.TenantIsolationViolation,
        ConversationErrorCategory.Authorization,
        Tenant,
        Conversation,
        Actor,
        Project,
        Folder,
        File,
        Message,
        Business,
        ProviderCorrelation,
        CommandMetadata,
        new CreateConversationCommand(CommandMetadata, Business, Project, Folder, "Case 123", ProviderCorrelation),
        new AppendMessageCommand(CommandMetadata, Conversation, Message, Actor, "Hello from the adopter.", ProviderCorrelation),
        new AddParticipantCommand(CommandMetadata, Conversation, Participant, ParticipantType.Human, ParticipantRole.Member, ProviderCorrelation),
        new AttachFileReferenceCommand(CommandMetadata, Conversation, File, Folder, Message),
        new UpdateConversationMetadataCommand(CommandMetadata, Conversation, "Case 123", Business, new Dictionary<string, string> { ["priority"] = "normal" }),
        new CloseConversationCommand(CommandMetadata, Conversation, "resolved"),
        new ArchiveConversationCommand(CommandMetadata, Conversation, "retained"),
        EventMetadata,
        new ConversationCreated(EventMetadata, Business, Project, Folder, "Case 123", ProviderCorrelation),
        new MessageAppended(EventMetadata, Message, Actor, "Hello from the adopter.", ProviderCorrelation),
        new ParticipantAdded(ParticipantEventMetadata, Participant, ParticipantType.Human, ParticipantRole.Member),
        new FileReferenceAttached(EventMetadata, File, Folder, Message),
        new ConversationMetadataUpdated(EventMetadata, "Case 123", Business, new Dictionary<string, string> { ["priority"] = "normal" }),
        new ConversationClosed(EventMetadata, "resolved"),
        new ConversationArchived(EventMetadata, "retained"),
        ProjectionTrustState.Current,
        ProjectionFreshnessReasonCode.Current,
        Freshness,
        FreshnessV1,
        new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
        new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt, ProviderCorrelation),
        new ConversationFileReferenceProjectionV1(File, Folder, Message),
        new ConversationSummaryProjection(Tenant, Conversation, Freshness, "Case 123", Business, [Actor, Participant]),
        new ConversationSummaryProjectionV1(
            Version,
            Tenant,
            Conversation,
            FreshnessV1,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            [Actor, Participant],
            1,
            1,
            ProviderCorrelation),
        new ConversationDetailProjectionV1(
            Version,
            Tenant,
            Conversation,
            FreshnessV1,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            ProviderCorrelation,
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt, ProviderCorrelation)],
            [new ConversationFileReferenceProjectionV1(File, Folder, Message)],
            new Dictionary<string, string> { ["priority"] = "normal" }),
        new ConversationMessageProjection(Tenant, Conversation, Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt, Freshness),
        Visibility,
        new ConversationCommandAcceptedResult(Version, Tenant, Conversation, ConversationCommandType.AppendMessageCommand, "correlation-001", "idempotency-001", Visibility),
        new ConversationCreatedResult(Version, Tenant, Conversation, "correlation-001", "idempotency-001", Visibility, ConversationCommandType.CreateConversationCommand),
        SafeError(ConversationErrorCode.TenantIsolationViolation),
        new ConversationErrorResult([SafeError(ConversationErrorCode.AggregateNotFound)]),
    ];

    internal static ConversationError SafeError(ConversationErrorCode code) => new(
        Version,
        code,
        ErrorCategoryFor(code),
        ConversationErrorCode.IsRetryable(code),
        "correlation-001",
        "audit-001",
        new Uri("https://docs.hexalith.local/conversations/errors"),
        new Dictionary<string, string>
        {
            ["target"] = "hidden",
        },
        "The requested operation was not accepted.");

    internal static IReadOnlyList<ConversationErrorCode> AllErrorCodes =>
    [
        ConversationErrorCode.TenantBindingMissing,
        ConversationErrorCode.TenantIsolationViolation,
        ConversationErrorCode.TenantProjectionStale,
        ConversationErrorCode.AuditSinkUnavailable,
        ConversationErrorCode.AuditPairingRequired,
        ConversationErrorCode.IdempotencyConflict,
        ConversationErrorCode.IdempotencyOutcomeUnknown,
        ConversationErrorCode.IdempotencyKeyMissing,
        ConversationErrorCode.AggregateNotFound,
        ConversationErrorCode.SchemaVersionUnsupported,
        ConversationErrorCode.CommandValidationFailed,
        ConversationErrorCode.DuplicateParticipant,
        ConversationErrorCode.UnsupportedParticipant,
        ConversationErrorCode.ParticipantValidationUnavailable,
        ConversationErrorCode.TenantContextMismatch,
        ConversationErrorCode.ProviderOnlyIdentityForbidden,
    ];

    private static ConversationErrorCategory ErrorCategoryFor(ConversationErrorCode code)
        => code switch
        {
            _ when code == ConversationErrorCode.TenantBindingMissing => ConversationErrorCategory.Validation,
            _ when code == ConversationErrorCode.TenantIsolationViolation => ConversationErrorCategory.Authorization,
            _ when code == ConversationErrorCode.TenantProjectionStale => ConversationErrorCategory.Freshness,
            _ when code == ConversationErrorCode.AuditSinkUnavailable => ConversationErrorCategory.Audit,
            _ when code == ConversationErrorCode.AuditPairingRequired => ConversationErrorCategory.Audit,
            _ when code == ConversationErrorCode.IdempotencyConflict => ConversationErrorCategory.Conflict,

            // P53 review fix (2026-05-20): IdempotencyOutcomeUnknown is a retryable-uncertainty signal,
            // not a projection-staleness signal. The new Uncertainty category keeps Freshness reserved
            // strictly for stale read-models.
            _ when code == ConversationErrorCode.IdempotencyOutcomeUnknown => ConversationErrorCategory.Uncertainty,
            _ when code == ConversationErrorCode.IdempotencyKeyMissing => ConversationErrorCategory.Validation,
            _ when code == ConversationErrorCode.AggregateNotFound => ConversationErrorCategory.Hidden,
            _ when code == ConversationErrorCode.SchemaVersionUnsupported => ConversationErrorCategory.Versioning,
            _ when code == ConversationErrorCode.CommandValidationFailed => ConversationErrorCategory.Validation,
            _ when code == ConversationErrorCode.DuplicateParticipant => ConversationErrorCategory.Conflict,
            _ when code == ConversationErrorCode.UnsupportedParticipant => ConversationErrorCategory.Validation,
            _ when code == ConversationErrorCode.ParticipantValidationUnavailable => ConversationErrorCategory.Validation,
            _ when code == ConversationErrorCode.TenantContextMismatch => ConversationErrorCategory.Authorization,
            _ when code == ConversationErrorCode.ProviderOnlyIdentityForbidden => ConversationErrorCategory.Validation,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported error code."),
        };
}
