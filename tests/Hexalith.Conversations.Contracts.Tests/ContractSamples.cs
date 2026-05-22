// <copyright file="ContractSamples.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
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

    internal static readonly ConversationEventMetadata LifecycleChangedEventMetadata = new(
        Version,
        "event-lifecycle-001",
        ConversationEventType.ConversationLifecycleChanged,
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

    internal static readonly DateTimeOffset GovernanceTimestamp = new(2026, 5, 18, 12, 0, 0, TimeSpan.Zero);

    internal static readonly GovernanceOperationMetadata GovernanceMetadata = new(
        Version,
        Tenant,
        Conversation,
        Actor,
        "customer-request",
        "retention-policy-standard",
        GovernanceTimestamp,
        "correlation-001",
        "causation-001");

    internal static readonly GovernanceTarget GovernanceConversationTarget = new(GovernedTargetKind.Conversation);

    internal static readonly GovernanceTarget SensitivityMessageTarget = new(GovernedTargetKind.Message, MessageId: Message);

    internal static readonly GovernanceTarget RedactionMessageTarget = new(GovernedTargetKind.Message, MessageId: Message);

    internal static readonly GovernanceAuditEvidenceReference AuditEvidence = new(
        new AuditEvidenceHandle("audit-evidence-001"),
        "retention-policy-standard",
        GovernanceTimestamp);

    internal static readonly PrivilegedOperationalJustificationV1 PrivilegedJustification = new(
        Version,
        Tenant,
        Conversation,
        GovernanceConversationTarget,
        Actor,
        PrivilegedOperationalActionClass.Read,
        PrivilegedActionClass.ComplianceReview,
        "privileged-review-policy",
        "customer-request",
        GovernanceTimestamp,
        "correlation-001",
        "causation-001",
        AuditEvidence);

    internal static readonly GovernanceRequest GovernanceRequest = new(
        GovernanceMetadata,
        GovernanceOperationKind.SetRetentionPolicy,
        GovernanceConversationTarget,
        RetentionAction.ApplyPolicy);

    internal static readonly SetConversationRetentionPolicyCommand RetentionCommand = new(
        CommandMetadata,
        Conversation,
        "retention-policy-standard",
        "customer-request",
        GovernanceTimestamp);

    internal static readonly MarkConversationContentSensitiveCommand SensitivityCommand = new(
        CommandMetadata,
        Conversation,
        SensitivityMessageTarget,
        SensitivityCategory.Restricted,
        "sensitivity-policy-standard",
        "customer-request",
        GovernanceTimestamp);

    internal static readonly RedactMessageContentCommand RedactionCommand = new(
        CommandMetadata,
        Conversation,
        RedactionMessageTarget,
        RedactionCategory.ContentSuppression,
        "redaction-policy-standard",
        "customer-request",
        GovernanceTimestamp);

    internal static readonly ConversationEventMetadata RetentionSetEventMetadata = new(
        Version,
        "event-retention-set-001",
        ConversationEventType.RetentionPolicySet,
        Tenant,
        Conversation,
        "correlation-001",
        GovernanceTimestamp,
        Actor,
        "causation-001");

    internal static readonly ConversationEventMetadata RetentionReplacedEventMetadata = new(
        Version,
        "event-retention-replaced-001",
        ConversationEventType.RetentionPolicyReplaced,
        Tenant,
        Conversation,
        "correlation-001",
        GovernanceTimestamp.AddMinutes(1),
        Actor,
        "causation-001");

    internal static readonly ConversationEventMetadata SensitivityMarkedEventMetadata = new(
        Version,
        "event-sensitive-marked-001",
        ConversationEventType.ConversationContentMarkedSensitive,
        Tenant,
        Conversation,
        "correlation-001",
        GovernanceTimestamp,
        Actor,
        "causation-001");

    internal static readonly ConversationEventMetadata RedactionEventMetadata = new(
        Version,
        "event-redacted-001",
        ConversationEventType.MessageContentRedacted,
        Tenant,
        Conversation,
        "correlation-001",
        GovernanceTimestamp,
        Actor,
        "causation-001");

    internal static IReadOnlyList<object> AllContracts =>
    [
        Version,
        new ContractVersionInfo("Conversations", Version, Version),
        new UnsupportedSchemaVersion(new SchemaVersion(2), Version, Version),
        ConversationCommandType.CreateConversationCommand,
        ConversationCommandType.SetConversationRetentionPolicyCommand,
        ConversationCommandType.MarkConversationContentSensitiveCommand,
        ConversationCommandType.RedactMessageContentCommand,
        ConversationEventType.ConversationCreated,
        ConversationEventType.RetentionPolicySet,
        ConversationEventType.ConversationContentMarkedSensitive,
        ConversationEventType.MessageContentRedacted,
        ConversationLifecycleStatus.Open,
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
        GovernanceOperationKind.SetRetentionPolicy,
        GovernedTargetKind.Conversation,
        RetentionAction.ApplyPolicy,
        SensitivityCategory.Sensitive,
        RedactionCategory.DisplayMask,
        ArchivalState.Archived,
        LegalHoldDeferral.DeferredUntilRelease,
        PolicyBlockedOutcome.LegalHoldActive,
        PrivilegedActionClass.OperationalOverride,
        PrivilegedOperationalActionClass.Read,
        GovernanceOutcome.Succeeded,
        GovernanceRemediation.None,
        GovernanceStateConcept.EventHistory,
        AuditRecordActionClassification.Allowed,
        GovernanceMetadata,
        GovernanceConversationTarget,
        SensitivityMessageTarget,
        new GovernanceTarget(GovernedTargetKind.AuditRecord, AuditEvidenceHandle: AuditEvidence.Handle),
        AuditEvidence.Handle,
        AuditEvidence,
        new AuditRecordPolicyTreatmentV1(
            Version,
            Tenant,
            Conversation,
            AuditEvidence.Handle,
            ProjectionTrustState.Current,
            ProjectionTrustState.Redacted,
            AuditRecordActionClassification.Allowed,
            ExportEligible: false,
            SeparateLogRequired: false,
            "retention-policy-standard",
            "Use the returned audit handle as governed evidence."),
        new GetConversationAuditRecordQuery(
            Version,
            Tenant,
            "caller-001",
            "correlation-001",
            Conversation,
            "audit-evidence-001",
            AuditRecordActionClassification.Allowed),
        new ConversationAuditRecordDetailsV1(
            Version,
            Tenant,
            Conversation,
            Actor,
            GovernanceTimestamp,
            AuditRecordActionClassification.Allowed,
            GovernanceOutcome.Succeeded,
            "retention-policy-standard",
            "customer-request",
            new GovernanceTarget(GovernedTargetKind.AuditRecord, AuditEvidenceHandle: AuditEvidence.Handle),
            AuditEvidence,
            new AuditRecordPolicyTreatmentV1(
                Version,
                Tenant,
                Conversation,
                AuditEvidence.Handle,
                ProjectionTrustState.Current,
                ProjectionTrustState.Redacted,
                AuditRecordActionClassification.Allowed,
                ExportEligible: false,
                SeparateLogRequired: false,
                "retention-policy-standard",
                "Use the returned audit handle as governed evidence."),
            FreshnessV1,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            "correlation-001",
            "causation-001"),
        ConversationAuditRecordResult.Visible(
            Version,
            new ConversationAuditRecordDetailsV1(
                Version,
                Tenant,
                Conversation,
                Actor,
                GovernanceTimestamp,
                AuditRecordActionClassification.Allowed,
                GovernanceOutcome.Succeeded,
                "retention-policy-standard",
                "customer-request",
                new GovernanceTarget(GovernedTargetKind.AuditRecord, AuditEvidenceHandle: AuditEvidence.Handle),
                AuditEvidence,
                new AuditRecordPolicyTreatmentV1(
                    Version,
                    Tenant,
                    Conversation,
                    AuditEvidence.Handle,
                    ProjectionTrustState.Current,
                    ProjectionTrustState.Redacted,
                    AuditRecordActionClassification.Allowed,
                    ExportEligible: false,
                    SeparateLogRequired: false,
                    "retention-policy-standard",
                    "Use the returned audit handle as governed evidence."),
                FreshnessV1,
                ProjectionTrustState.Current,
                ProjectionFreshnessReasonCode.Current,
                "correlation-001",
                "causation-001"),
            "Use the returned audit handle as governed evidence."),
        PrivilegedJustification,
        new RecordPrivilegedOperationalJustificationCommand(PrivilegedJustification),
        new GetPrivilegedOperationalJustificationQuery(
            Version,
            Tenant,
            "caller-001",
            "correlation-001",
            Conversation,
            "audit-evidence-001"),
        new PrivilegedOperationalJustificationDetailsV1(
            Version,
            Tenant,
            Conversation,
            GovernanceConversationTarget,
            Actor,
            PrivilegedOperationalActionClass.Read,
            PrivilegedActionClass.ComplianceReview,
            "privileged-review-policy",
            "customer-request",
            GovernanceTimestamp,
            GovernanceOutcome.Succeeded,
            AuditEvidence,
            ProjectionTrustState.Current,
            FreshnessV1,
            "Use the returned audit handle as governed evidence.",
            "correlation-001",
            "causation-001"),
        PrivilegedOperationalJustificationResult.Visible(
            Version,
            new PrivilegedOperationalJustificationDetailsV1(
                Version,
                Tenant,
                Conversation,
                GovernanceConversationTarget,
                Actor,
                PrivilegedOperationalActionClass.Read,
                PrivilegedActionClass.ComplianceReview,
                "privileged-review-policy",
                "customer-request",
                GovernanceTimestamp,
                GovernanceOutcome.Succeeded,
                AuditEvidence,
                ProjectionTrustState.Current,
                FreshnessV1,
                "Use the returned audit handle as governed evidence.",
                "correlation-001",
                "causation-001"),
            "Use the returned audit handle as governed evidence."),
        GovernanceRequest,
        GovernanceEvidence(GovernanceOperationKind.SetRetentionPolicy, GovernanceOutcome.Succeeded),
        RetentionCommand,
        SensitivityCommand,
        RedactionCommand,
        new ConversationRetentionPolicyResult(
            Version,
            Tenant,
            Conversation,
            GovernanceOutcome.Succeeded,
            "correlation-001",
            AuditEvidence,
            Remediation: GovernanceRemediation.None),
        new ConversationRetentionPolicyResult(
            Version,
            Tenant,
            Conversation,
            GovernanceOutcome.Denied,
            "correlation-002",
            Error: SafeError(ConversationErrorCode.TenantIsolationViolation),
            Remediation: GovernanceRemediation.ResubmitWithPolicyReference),
        new ConversationRetentionPolicyResult(
            Version,
            Tenant,
            Conversation,
            GovernanceOutcome.AuditUnavailableFailed,
            "correlation-003",
            Error: SafeError(ConversationErrorCode.AuditSinkUnavailable),
            Remediation: GovernanceRemediation.RetryWhenAuditAvailable),
        new ConversationRetentionPolicyResult(
            Version,
            Tenant,
            Conversation,
            GovernanceOutcome.PolicyBlocked,
            "correlation-004",
            Error: SafeError(ConversationErrorCode.CommandValidationFailed),
            Remediation: GovernanceRemediation.WaitForLegalHoldRelease),
        new ConversationSensitivityMarkResult(
            Version,
            Tenant,
            Conversation,
            SensitivityMessageTarget,
            SensitivityCategory.Restricted,
            GovernanceOutcome.Succeeded,
            "correlation-001",
            AuditEvidence,
            Remediation: GovernanceRemediation.None),
        new ConversationSensitivityMarkResult(
            Version,
            Tenant,
            Conversation,
            SensitivityMessageTarget,
            null,
            GovernanceOutcome.Denied,
            "correlation-002",
            Error: SafeError(ConversationErrorCode.TenantIsolationViolation),
            Remediation: GovernanceRemediation.RequestAuthorization),
        new ConversationSensitivityMarkResult(
            Version,
            Tenant,
            Conversation,
            SensitivityMessageTarget,
            null,
            GovernanceOutcome.AuditUnavailableFailed,
            "correlation-003",
            Error: SafeError(ConversationErrorCode.AuditSinkUnavailable),
            Remediation: GovernanceRemediation.RetryWhenAuditAvailable),
        new ConversationSensitivityMarkResult(
            Version,
            Tenant,
            Conversation,
            SensitivityMessageTarget,
            null,
            GovernanceOutcome.PolicyBlocked,
            "correlation-004",
            Error: SafeError(ConversationErrorCode.CommandValidationFailed),
            Remediation: GovernanceRemediation.WaitForLegalHoldRelease),
        new ConversationRedactionResult(
            Version,
            Tenant,
            Conversation,
            RedactionMessageTarget,
            RedactionCategory.ContentSuppression,
            GovernanceOutcome.Succeeded,
            "correlation-001",
            AuditEvidence,
            Remediation: GovernanceRemediation.None),
        new ConversationRedactionResult(
            Version,
            Tenant,
            Conversation,
            RedactionMessageTarget,
            null,
            GovernanceOutcome.Denied,
            "correlation-002",
            Error: SafeError(ConversationErrorCode.TenantIsolationViolation),
            Remediation: GovernanceRemediation.RequestAuthorization),
        new ConversationRedactionResult(
            Version,
            Tenant,
            Conversation,
            RedactionMessageTarget,
            null,
            GovernanceOutcome.AuditUnavailableFailed,
            "correlation-003",
            Error: SafeError(ConversationErrorCode.AuditSinkUnavailable),
            Remediation: GovernanceRemediation.RetryWhenAuditAvailable),
        new ConversationRedactionResult(
            Version,
            Tenant,
            Conversation,
            RedactionMessageTarget,
            null,
            GovernanceOutcome.PolicyBlocked,
            "correlation-004",
            Error: SafeError(ConversationErrorCode.CommandValidationFailed),
            Remediation: GovernanceRemediation.WaitForLegalHoldRelease),
        new ConversationRetentionPolicyProjectionV1(
            "retention-policy-standard",
            "customer-request",
            Actor,
            GovernanceTimestamp,
            AuditEvidence),
        new ConversationSensitivityMarkProjectionV1(
            SensitivityMessageTarget,
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            Actor,
            GovernanceTimestamp,
            AuditEvidence,
            ProjectionTrustState.Current),
        new ConversationRedactionProjectionV1(
            RedactionMessageTarget,
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            Actor,
            GovernanceTimestamp,
            AuditEvidence,
            ProjectionTrustState.Current),
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
        new ConversationLifecycleChanged(
            LifecycleChangedEventMetadata,
            ConversationLifecycleStatus.Open,
            ConversationLifecycleStatus.Closed,
            "resolved"),
        new RetentionPolicySet(
            RetentionSetEventMetadata,
            "retention-policy-standard",
            "customer-request",
            AuditEvidence),
        new RetentionPolicyReplaced(
            RetentionReplacedEventMetadata,
            "retention-policy-extended",
            "retention-policy-standard",
            "customer-request",
            AuditEvidence),
        new ConversationContentMarkedSensitive(
            SensitivityMarkedEventMetadata,
            SensitivityMessageTarget,
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AuditEvidence),
        new MessageContentRedacted(
            RedactionEventMetadata,
            RedactionMessageTarget,
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AuditEvidence),
        ProjectionTrustState.Current,
        ProjectionFreshnessReasonCode.Current,
        Freshness,
        FreshnessV1,
        new GetConversationQuery(Version, Tenant, "caller-001", "correlation-001", Conversation),
        new ConversationTemporalAnchorV1(
            Version,
            Tenant,
            Conversation,
            ConversationTemporalAnchorV1.SafeSourcePositionKind,
            SafeSourcePosition: 42),
        new GetConversationAtPointInTimeQuery(
            Version,
            Tenant,
            "caller-001",
            "correlation-001",
            Conversation,
            new ConversationTemporalAnchorV1(
                Version,
                Tenant,
                Conversation,
                ConversationTemporalAnchorV1.ProjectionCursorKind,
                ProjectionCursor: "pos:0000000042")),
        new ConversationTemporalConfidenceV1(
            Version,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            true,
            "Temporal evidence is complete for the requested anchor."),
        new ConversationTemporalDetailsV1(
            Version,
            Tenant,
            Conversation,
            new ConversationTemporalAnchorV1(
                Version,
                Tenant,
                Conversation,
                ConversationTemporalAnchorV1.SafeSourcePositionKind,
                SafeSourcePosition: 42),
            new ConversationTemporalConfidenceV1(
                Version,
                ProjectionTrustState.Current,
                ProjectionFreshnessReasonCode.Current,
                true,
                "Temporal evidence is complete for the requested anchor."),
            FreshnessV1,
            "Open",
            "Case 123",
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    Message,
                    Actor,
                    "[redacted]",
                    EventMetadata.CommittedAt),
            ],
            Redactions:
            [
                new ConversationRedactionProjectionV1(
                    RedactionMessageTarget,
                    RedactionCategory.ContentSuppression,
                    "redaction-policy-standard",
                    "customer-request",
                    Actor,
                    GovernanceTimestamp,
                    AuditEvidence,
                    ProjectionTrustState.Current),
            ]),
        ConversationCitationAvailability.Available,
        ConversationAuditReadinessState.Ready,
        ConversationVerificationState.Verified,
        ConversationSearchMatchSource.BusinessReference,
        new ConversationSearchTrustPreviewV1(
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            ProjectionTrustState.Redacted,
            ProjectionTrustState.Current,
            ConversationCitationAvailability.Available,
            ConversationAuditReadinessState.Ready,
            ConversationVerificationState.Verified,
            ConversationSearchMatchSource.BusinessReference,
            "Visible through authorized tenant scope and matched business reference."),
        new ConversationListFilterV1(Business, Project, Folder, "Open", ParticipantPartyId: Participant),
        new ConversationPageRequest(25),
        new ConversationPageMetadata(1, "opaque-cursor"),
        new ListConversationsQuery(
            Version,
            Tenant,
            "caller-001",
            "correlation-001",
            new ConversationListFilterV1(Business, Project, Folder, "Open", ParticipantPartyId: Participant),
            new ConversationPageRequest(25)),
        new ConversationProviderCorrelationV1("provider-a", "assistant", Version),
        new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member),
        new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt, ProviderCorrelation),
        new ConversationFileReferenceProjectionV1(File, Folder, Message),
        new PartyReferenceHydrationV1(Participant, ProjectionTrustState.Current, true, "Project participant", "participant-token", "Available"),
        new ProjectReferenceHydrationV1(Project, ProjectionTrustState.Unavailable, false, "Reference unavailable", "unavailable", "Unavailable"),
        new FolderReferenceHydrationV1(Folder, ProjectionTrustState.Redacted, false, "Reference redacted", "redacted", "Redacted"),
        new FileReferenceHydrationV1(File, ProjectionTrustState.Forbidden, false, "Reference unavailable", "unavailable", "Unavailable"),
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
            new Dictionary<string, string> { ["priority"] = "normal" },
            new ConversationRetentionPolicyProjectionV1(
                "retention-policy-standard",
                "customer-request",
                Actor,
                GovernanceTimestamp,
                AuditEvidence),
            [
                new ConversationSensitivityMarkProjectionV1(
                    SensitivityMessageTarget,
                    SensitivityCategory.Restricted,
                    "sensitivity-policy-standard",
                    "customer-request",
                    Actor,
                    GovernanceTimestamp,
                    AuditEvidence,
                    ProjectionTrustState.Current),
            ]),
        new ConversationSummaryV1(
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
            new ConversationProviderCorrelationV1("provider-a", "assistant", Version)),
        new ConversationDetailsV1(
            Version,
            Tenant,
            Conversation,
            FreshnessV1,
            "Open",
            "Case 123",
            Business,
            Project,
            Folder,
            new ConversationProviderCorrelationV1("provider-a", "assistant", Version),
            [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
            [new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt)],
            [new ConversationFileReferenceProjectionV1(File, Folder, Message)],
            "Unavailable",
            PartyHydration: [new PartyReferenceHydrationV1(Participant, ProjectionTrustState.Current, true, "Project participant", "participant-token", "Available")],
            ProjectHydration: new ProjectReferenceHydrationV1(Project, ProjectionTrustState.Unavailable, false, "Reference unavailable", "unavailable", "Unavailable"),
            FolderHydration: new FolderReferenceHydrationV1(Folder, ProjectionTrustState.Redacted, false, "Reference redacted", "redacted", "Redacted"),
            FileHydration: [new FileReferenceHydrationV1(File, ProjectionTrustState.Forbidden, false, "Reference unavailable", "unavailable", "Unavailable")]),
        ConversationDetailResult.Visible(
            Version,
            new ConversationDetailsV1(
                Version,
                Tenant,
                Conversation,
                FreshnessV1,
                "Open",
                "Case 123",
                Business,
                Project,
                Folder,
                new ConversationProviderCorrelationV1("provider-a", "assistant", Version),
                [new ConversationParticipantProjectionV1(Participant, ParticipantType.Human, ParticipantRole.Member)],
                [new ConversationTimelineMessageProjectionV1(Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt)],
                [new ConversationFileReferenceProjectionV1(File, Folder, Message)],
                "Unavailable",
                PartyHydration: [new PartyReferenceHydrationV1(Participant, ProjectionTrustState.Current, true, "Project participant", "participant-token", "Available")],
                SensitivityMarks:
                [
                    new ConversationSensitivityMarkProjectionV1(
                        SensitivityMessageTarget,
                        SensitivityCategory.Restricted,
                        "sensitivity-policy-standard",
                        "customer-request",
                        Actor,
                        GovernanceTimestamp,
                        AuditEvidence,
                        ProjectionTrustState.Current),
                ],
                Redactions:
                [
                    new ConversationRedactionProjectionV1(
                        RedactionMessageTarget,
                        RedactionCategory.ContentSuppression,
                        "redaction-policy-standard",
                        "customer-request",
                        Actor,
                        GovernanceTimestamp,
                        AuditEvidence,
                        ProjectionTrustState.Current),
                ]),
            "Current projection is available."),
        ConversationTemporalDetailResult.Visible(
            Version,
            new ConversationTemporalDetailsV1(
                Version,
                Tenant,
                Conversation,
                new ConversationTemporalAnchorV1(
                    Version,
                    Tenant,
                    Conversation,
                    ConversationTemporalAnchorV1.SafeSourcePositionKind,
                    SafeSourcePosition: 42),
                new ConversationTemporalConfidenceV1(
                    Version,
                    ProjectionTrustState.Current,
                    ProjectionFreshnessReasonCode.Current,
                    true,
                    "Temporal evidence is complete for the requested anchor."),
                FreshnessV1,
                "Open",
                "Case 123",
                Messages:
                [
                    new ConversationTimelineMessageProjectionV1(
                        Message,
                        Actor,
                        "[redacted]",
                        EventMetadata.CommittedAt),
                ],
                Redactions:
                [
                    new ConversationRedactionProjectionV1(
                        RedactionMessageTarget,
                        RedactionCategory.ContentSuppression,
                        "redaction-policy-standard",
                        "customer-request",
                        Actor,
                        GovernanceTimestamp,
                        AuditEvidence,
                        ProjectionTrustState.Current),
                ]),
            "Use the returned temporal anchor for stable historical evidence."),
        new ConversationListResult(
            Version,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            [
                new ConversationSummaryV1(
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
                    new ConversationProviderCorrelationV1("provider-a", "assistant", Version)),
            ],
            new ConversationPageMetadata(1),
            "Accessible results are complete for the supplied filters."),
        new ConversationMessageProjection(Tenant, Conversation, Message, Actor, "Hello from the adopter.", EventMetadata.CommittedAt, Freshness),
        Visibility,
        new ConversationCommandAcceptedResult(Version, Tenant, Conversation, ConversationCommandType.AppendMessageCommand, "correlation-001", "idempotency-001", Visibility),
        new ConversationCreatedResult(Version, Tenant, Conversation, "correlation-001", "idempotency-001", Visibility, ConversationCommandType.CreateConversationCommand),
        SafeError(ConversationErrorCode.TenantIsolationViolation),
        new ConversationErrorResult([SafeError(ConversationErrorCode.AggregateNotFound)]),
    ];

    internal static GovernanceAuditEvidence GovernanceEvidence(GovernanceOperationKind operationKind, GovernanceOutcome outcome)
        => new(
            GovernanceMetadata,
            operationKind,
            GovernanceConversationTarget,
            outcome,
            AuditEvidence,
            RemediationFor(outcome));

    private static GovernanceRemediation RemediationFor(GovernanceOutcome outcome)
    {
        if (outcome == GovernanceOutcome.Denied)
        {
            return GovernanceRemediation.ResubmitWithPolicyReference;
        }

        if (outcome == GovernanceOutcome.AuditUnavailableFailed)
        {
            return GovernanceRemediation.RetryWhenAuditAvailable;
        }

        if (outcome == GovernanceOutcome.PolicyBlocked)
        {
            return GovernanceRemediation.WaitForLegalHoldRelease;
        }

        return GovernanceRemediation.None;
    }

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
