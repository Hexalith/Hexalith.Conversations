// <copyright file="ConversationProjectionMaterializer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Builds tenant-scoped conversation read models from public conversation events.
/// </summary>
public sealed class ConversationProjectionMaterializer
{
    private const string ArchivedState = "Archived";
    private const string ClosedState = "Closed";
    private const string InitializingState = "Initializing";
    private const string OpenState = "Open";

    /// <summary>
    /// Projects a summary/detail pair from an ordered public event sequence.
    /// </summary>
    /// <param name="tenantId">The tenant this projection is allowed to materialize.</param>
    /// <param name="conversationId">The conversation this projection is allowed to materialize.</param>
    /// <param name="events">The public event sequence.</param>
    /// <param name="projectionGeneratedAt">The projection generation time.</param>
    /// <param name="staleAfter">The freshness threshold.</param>
    /// <param name="isRebuilding">A value indicating whether rebuild or catch-up work is active.</param>
    /// <param name="metadataWriteFailed">A value indicating whether projection mutation succeeded but metadata update failed.</param>
    /// <returns>The projected summary/detail pair.</returns>
    public ConversationProjectedReadModels Project(
        TenantId tenantId,
        ConversationId conversationId,
        IEnumerable<ConversationProjectionEventRecord> events,
        DateTimeOffset projectionGeneratedAt,
        TimeSpan staleAfter,
        bool isRebuilding = false,
        bool metadataWriteFailed = false)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(conversationId);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(staleAfter, TimeSpan.Zero);

        ProjectionBuilder builder = new(tenantId, conversationId);
        foreach (ConversationProjectionEventRecord record in events)
        {
            builder.Apply(record);
        }

        ProjectionFreshnessV1 freshness = CreateFreshness(
            builder,
            projectionGeneratedAt,
            staleAfter,
            isRebuilding,
            metadataWriteFailed);
        ConversationSearchTrustPreviewV1 searchTrustPreview = CreateSearchTrustPreview(builder, freshness);
        ConversationEvidenceTrustPostureV1 trustPosture = CreateTrustPosture(builder, freshness);
        IReadOnlyList<ConversationEvidenceEntryV1> evidenceEntries = CreateEvidenceEntries(builder, freshness);

        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            builder.LifecycleState,
            builder.Label,
            builder.BusinessReference,
            builder.ProjectId,
            builder.FolderId,
            builder.ParticipantPartyIds,
            builder.Messages.Count,
            builder.FileReferences.Count,
            builder.ProviderCorrelation,
            searchTrustPreview);

        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            builder.LifecycleState,
            builder.Label,
            builder.BusinessReference,
            builder.ProjectId,
            builder.FolderId,
            builder.ProviderCorrelation,
            builder.Participants,
            builder.Messages,
            builder.FileReferences,
            builder.Attributes,
            builder.ActiveRetentionPolicy,
            builder.SensitivityMarks,
            builder.Redactions,
            trustPosture,
            evidenceEntries);

        return new(summary, detail);
    }

    private static ProjectionFreshnessV1 CreateFreshness(
        ProjectionBuilder builder,
        DateTimeOffset projectionGeneratedAt,
        TimeSpan staleAfter,
        bool isRebuilding,
        bool metadataWriteFailed)
    {
        // ProjectionFreshnessV1 requires position >= 1; clamp here for the no-events case so the
        // freshness can still be constructed. The empty-stream path is non-current by definition
        // (WasCreated is false, so the projection is Rebuilding), and consumers must not trust the
        // synthesized cursor for ordering.
        long position = Math.Max(builder.LastAppliedPosition, 1);
        DateTimeOffset lastApplied = builder.LastAppliedTimestamp ?? projectionGeneratedAt;
        bool contradictoryTimestamp = projectionGeneratedAt < lastApplied;

        // When metadata is contradictory the projection is reported as Unavailable. The public
        // ProjectionGeneratedAt is clamped to lastApplied so the contract invariant
        // (generatedAt >= lastApplied) holds; consumers must not treat the clamped value as truth.
        DateTimeOffset publicGeneratedAt = contradictoryTimestamp ? lastApplied : projectionGeneratedAt;
        TimeSpan lag = publicGeneratedAt - lastApplied;

        ProjectionTrustState state = ProjectionTrustState.Current;
        ProjectionFreshnessReasonCode reason = ProjectionFreshnessReasonCode.Current;
        bool stale = false;

        if (metadataWriteFailed)
        {
            state = ProjectionTrustState.Unavailable;
            reason = ProjectionFreshnessReasonCode.MetadataWriteFailed;
        }
        else if (builder.Poisoned)
        {
            state = ProjectionTrustState.Unavailable;
            reason = ProjectionFreshnessReasonCode.PoisonEvent;
        }
        else if (builder.UnsupportedVersion)
        {
            state = ProjectionTrustState.Unavailable;
            reason = ProjectionFreshnessReasonCode.Unavailable;
        }
        else if (contradictoryTimestamp)
        {
            state = ProjectionTrustState.Unavailable;
            reason = ProjectionFreshnessReasonCode.MetadataContradictory;
        }
        else if (builder.HasGap)
        {
            state = ProjectionTrustState.Rebuilding;
            reason = ProjectionFreshnessReasonCode.GapDetected;
        }
        else if (builder.HasOutOfOrderEvent)
        {
            state = ProjectionTrustState.Rebuilding;
            reason = ProjectionFreshnessReasonCode.OutOfOrderEvent;
        }
        else if (isRebuilding || !builder.WasCreated)
        {
            state = ProjectionTrustState.Rebuilding;
            reason = ProjectionFreshnessReasonCode.Rebuilding;
        }
        else if (lag > staleAfter)
        {
            state = ProjectionTrustState.Stale;
            reason = ProjectionFreshnessReasonCode.StaleThresholdExceeded;
            stale = true;
        }

        return new ProjectionFreshnessV1(
            SchemaVersion.Current,
            FormatCursor(position),
            position,
            lastApplied,
            publicGeneratedAt,
            lag,
            stale,
            state,
            reason);
    }

    private static string FormatCursor(long position)
        => FormattableString.Invariant($"pos:{position:D10}");

    private static ConversationSearchTrustPreviewV1 CreateSearchTrustPreview(
        ProjectionBuilder builder,
        ProjectionFreshnessV1 freshness)
    {
        bool current = freshness.AllowsTrustBearingDecision();
        ProjectionTrustState redactionState = builder.Redactions.Count > 0
            ? ProjectionTrustState.Redacted
            : current ? ProjectionTrustState.Current : ProjectionTrustState.Unavailable;
        ConversationCitationAvailability citationAvailability = current && builder.WasCreated
            ? ConversationCitationAvailability.Available
            : ConversationCitationAvailability.Unavailable;
        ConversationAuditReadinessState auditReadiness = current && builder.HasAuditEvidence
            ? ConversationAuditReadinessState.Ready
            : current ? ConversationAuditReadinessState.Incomplete : ConversationAuditReadinessState.Unknown;

        return new(
            freshness.FreshnessState,
            freshness.ReasonCode,
            redactionState,
            builder.ParticipantPartyIds.Count == 0 ? ProjectionTrustState.Current : ProjectionTrustState.Unavailable,
            citationAvailability,
            auditReadiness,
            ConversationVerificationState.Unknown,
            ConversationSearchMatchSource.TenantScope,
            current
                ? "Visible through authorized tenant scope."
                : "Visible through authorized tenant scope with non-current metadata.");
    }

    private static ConversationEvidenceTrustPostureV1 CreateTrustPosture(
        ProjectionBuilder builder,
        ProjectionFreshnessV1 freshness)
    {
        bool current = freshness.AllowsTrustBearingDecision();
        ProjectionTrustState completeness = current && builder.WasCreated
            ? ProjectionTrustState.Current
            : freshness.FreshnessState == ProjectionTrustState.Current ? ProjectionTrustState.Unavailable : freshness.FreshnessState;
        ConversationAuditReadinessState auditReadiness = current && builder.HasAuditEvidence
            ? ConversationAuditReadinessState.Ready
            : current ? ConversationAuditReadinessState.Incomplete : ConversationAuditReadinessState.Unknown;
        ConversationCitationAvailability citationAvailability = current && builder.WasCreated
            ? ConversationCitationAvailability.Available
            : ConversationCitationAvailability.Unavailable;

        return new(
            SchemaVersion.Current,
            builder.TenantId,
            builder.ConversationId,
            freshness.ProjectionCursor,
            freshness,
            completeness,
            builder.ParticipantPartyIds.Count == 0 ? ProjectionTrustState.Current : ProjectionTrustState.Unavailable,
            citationAvailability,
            auditReadiness,
            ConversationVerificationState.Unknown,
            DefaultCommandEligibility(freshness, auditReadiness));
    }

    private static IReadOnlyList<ConversationCommandAvailabilityV1> DefaultCommandEligibility(
        ProjectionFreshnessV1 freshness,
        ConversationAuditReadinessState auditReadiness)
        =>
        [
            new ConversationCommandAvailabilityV1(
                "set-retention-policy",
                ProjectionTrustState.Unavailable,
                "conversations.governance",
                freshness.FreshnessState,
                "governance",
                ProjectionTrustState.Current,
                auditReadiness,
                "Command execution is blocked from the governed read surface.",
                freshness.ProjectionGeneratedAt),
            new ConversationCommandAvailabilityV1(
                "mark-content-sensitive",
                ProjectionTrustState.Unavailable,
                "conversations.governance",
                freshness.FreshnessState,
                "governance",
                ProjectionTrustState.Current,
                auditReadiness,
                "Command execution is blocked from the governed read surface.",
                freshness.ProjectionGeneratedAt),
            new ConversationCommandAvailabilityV1(
                "redact-message-content",
                ProjectionTrustState.Unavailable,
                "conversations.governance",
                freshness.FreshnessState,
                "governance",
                ProjectionTrustState.Current,
                auditReadiness,
                "Command execution is blocked from the governed read surface.",
                freshness.ProjectionGeneratedAt),
        ];

    private static IReadOnlyList<ConversationEvidenceEntryV1> CreateEvidenceEntries(
        ProjectionBuilder builder,
        ProjectionFreshnessV1 freshness)
    {
        List<ConversationEvidenceEntryV1> entries = [];
        ProjectionTrustState currentOrUnavailable = freshness.AllowsTrustBearingDecision()
            ? ProjectionTrustState.Current
            : ProjectionTrustState.Unavailable;
        ConversationCitationAvailability citationAvailability = freshness.AllowsTrustBearingDecision()
            ? ConversationCitationAvailability.Available
            : ConversationCitationAvailability.Unavailable;
        ConversationAuditReadinessState auditReadiness = freshness.AllowsTrustBearingDecision() && builder.HasAuditEvidence
            ? ConversationAuditReadinessState.Ready
            : freshness.AllowsTrustBearingDecision() ? ConversationAuditReadinessState.Incomplete : ConversationAuditReadinessState.Unknown;

        entries.Add(new ConversationEvidenceEntryV1(
            $"freshness:{freshness.ProjectionCursor}",
            "Freshness",
            null,
            freshness.ProjectionGeneratedAt,
            freshness.FreshnessState,
            citationAvailability,
            auditReadiness,
            freshness.FreshnessState));

        foreach (ConversationParticipantProjectionV1 participant in builder.Participants)
        {
            entries.Add(new ConversationEvidenceEntryV1(
                $"participant:{participant.ParticipantPartyId.Value}",
                "Participant",
                participant.ParticipantPartyId,
                participant.OccurredAt ?? freshness.LastAppliedEventTimestamp,
                currentOrUnavailable,
                citationAvailability,
                auditReadiness,
                currentOrUnavailable));
        }

        HashSet<MessageId> redactedMessageIds = builder.Redactions
            .Where(redaction => redaction.Target.MessageId is not null)
            .Select(redaction => redaction.Target.MessageId!)
            .ToHashSet();

        foreach (ConversationTimelineMessageProjectionV1 message in builder.Messages)
        {
            bool redacted = redactedMessageIds.Contains(message.MessageId);
            ProjectionTrustState state = redacted ? ProjectionTrustState.Redacted : currentOrUnavailable;
            entries.Add(new ConversationEvidenceEntryV1(
                $"message:{message.MessageId.Value}",
                "Message",
                message.AuthorPartyId,
                message.CreatedAt,
                state,
                citationAvailability,
                auditReadiness,
                state,
                MessageId: message.MessageId,
                VisibleText: message.Text,
                ProviderCorrelation: ConversationProviderCorrelationV1.From(message.ProviderCorrelation)));
        }

        foreach (ConversationFileReferenceProjectionV1 file in builder.FileReferences)
        {
            entries.Add(new ConversationEvidenceEntryV1(
                $"attachment:{file.FileId.Value}",
                "Attachment",
                null,
                file.OccurredAt ?? freshness.LastAppliedEventTimestamp,
                currentOrUnavailable,
                citationAvailability,
                auditReadiness,
                currentOrUnavailable,
                FileId: file.FileId));
        }

        if (builder.ActiveRetentionPolicy is not null)
        {
            ConversationRetentionPolicyProjectionV1 retention = builder.ActiveRetentionPolicy;
            entries.Add(new ConversationEvidenceEntryV1(
                $"retention:{retention.PolicyReference}",
                "RetentionPolicy",
                retention.ActorPartyId,
                retention.AppliedAt,
                currentOrUnavailable,
                citationAvailability,
                ConversationAuditReadinessState.Ready,
                currentOrUnavailable,
                PolicyReference: retention.PolicyReference));
        }

        foreach (ConversationSensitivityMarkProjectionV1 sensitivity in builder.SensitivityMarks)
        {
            entries.Add(new ConversationEvidenceEntryV1(
                $"sensitivity:{sensitivity.Target.ToTargetKey()}",
                "SensitivityMark",
                sensitivity.ActorPartyId,
                sensitivity.MarkedAt,
                sensitivity.TrustState,
                citationAvailability,
                ConversationAuditReadinessState.Ready,
                sensitivity.TrustState,
                MessageId: sensitivity.Target.MessageId,
                PolicyReference: sensitivity.PolicyReference));
        }

        foreach (ConversationRedactionProjectionV1 redaction in builder.Redactions)
        {
            entries.Add(new ConversationEvidenceEntryV1(
                $"redaction:{redaction.Target.ToTargetKey()}",
                "Redaction",
                redaction.ActorPartyId,
                redaction.RedactedAt,
                redaction.TrustState,
                citationAvailability,
                redaction.AuditEvidence is null ? ConversationAuditReadinessState.Incomplete : ConversationAuditReadinessState.Ready,
                redaction.TrustState,
                MessageId: redaction.Target.MessageId,
                VisibleText: redaction.Placeholder,
                PolicyReference: redaction.PolicyReference));
        }

        return entries
            .OrderBy(entry => entry.OccurredAt)
            .ThenBy(entry => entry.EntryId, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class ProjectionBuilder(TenantId tenantId, ConversationId conversationId)
    {
        private readonly Dictionary<string, string> _attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<FileId, ConversationFileReferenceProjectionV1> _fileReferences = [];
        private readonly Dictionary<MessageId, (long Position, ConversationTimelineMessageProjectionV1 Message)> _messages = [];
        private readonly Dictionary<PartyId, ConversationParticipantProjectionV1> _participants = [];
        private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ConversationRedactionProjectionV1> _redactions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ConversationSensitivityMarkProjectionV1> _sensitivityMarks = new(StringComparer.Ordinal);
        private string _lifecycleState = InitializingState;

        public IReadOnlyDictionary<string, string> Attributes
            => new Dictionary<string, string>(
                _attributes.OrderBy(attribute => attribute.Key, StringComparer.Ordinal),
                StringComparer.Ordinal);

        public ConversationRetentionPolicyProjectionV1? ActiveRetentionPolicy { get; private set; }

        public BusinessReference? BusinessReference { get; private set; }

        public ConversationId ConversationId => conversationId;

        public IReadOnlyList<ConversationFileReferenceProjectionV1> FileReferences
            => _fileReferences
                .Values
                .OrderBy(reference => reference.FileId.Value, StringComparer.Ordinal)
                .ToArray();

        public FolderId? FolderId { get; private set; }

        public bool HasGap { get; private set; }

        public bool HasAuditEvidence
            => ActiveRetentionPolicy?.AuditEvidence is not null
                || Redactions.Any(redaction => redaction.AuditEvidence is not null)
                || SensitivityMarks.Any(mark => mark.AuditEvidence is not null);

        public bool HasOutOfOrderEvent { get; private set; }

        public long LastAppliedPosition { get; private set; }

        public DateTimeOffset? LastAppliedTimestamp { get; private set; }

        public string? Label { get; private set; }

        public string LifecycleState => _lifecycleState;

        public IReadOnlyList<ConversationTimelineMessageProjectionV1> Messages
            => _messages
                .Values
                .OrderBy(entry => entry.Message.CreatedAt)
                .ThenBy(entry => entry.Position)
                .Select(entry => entry.Message)
                .ToArray();

        public IReadOnlyList<PartyId> ParticipantPartyIds
            => _participants
                .Keys
                .OrderBy(partyId => partyId.Value, StringComparer.Ordinal)
                .ToArray();

        public IReadOnlyList<ConversationParticipantProjectionV1> Participants
            => _participants
                .Values
                .OrderBy(participant => participant.ParticipantPartyId.Value, StringComparer.Ordinal)
                .ToArray();

        public bool Poisoned { get; private set; }

        public ProjectId? ProjectId { get; private set; }

        public ProviderCorrelationMetadata? ProviderCorrelation { get; private set; }

        public TenantId TenantId => tenantId;

        public IReadOnlyList<ConversationRedactionProjectionV1> Redactions
            => _redactions
                .Values
                .OrderBy(redaction => redaction.Target.ToTargetKey(), StringComparer.Ordinal)
                .ToArray();

        public IReadOnlyList<ConversationSensitivityMarkProjectionV1> SensitivityMarks
            => _sensitivityMarks
                .Values
                .OrderBy(mark => mark.Target.ToTargetKey(), StringComparer.Ordinal)
                .ToArray();

        public bool UnsupportedVersion { get; private set; }

        public bool WasCreated { get; private set; }

        public void Apply(ConversationProjectionEventRecord record)
        {
            ConversationEventMetadata? metadata = TryGetMetadata(record.Event);
            if (metadata is null)
            {
                // Unknown event type: cannot mutate state, downgrade to non-current.
                HasOutOfOrderEvent = true;
                return;
            }

            // Tenant/conversation mismatch is checked before dedup so a same-EventId legitimate
            // follow-up event in the same pass is not silently swallowed by a poison dedup entry.
            if (!tenantId.Equals(metadata.TenantId) || !conversationId.Equals(metadata.ConversationId))
            {
                Poisoned = true;
                return;
            }

            if (metadata.SchemaVersion != SchemaVersion.Current)
            {
                UnsupportedVersion = true;
                return;
            }

            if (!_processedEventIds.Add(metadata.EventId))
            {
                return;
            }

            if (LastAppliedPosition == 0)
            {
                if (record.Position != 1)
                {
                    HasGap = true;
                }
            }
            else if (record.Position != LastAppliedPosition + 1)
            {
                if (record.Position <= LastAppliedPosition)
                {
                    HasOutOfOrderEvent = true;
                }
                else
                {
                    HasGap = true;
                }
            }

            if (!WasCreated && record.Event is not ConversationCreated)
            {
                HasOutOfOrderEvent = true;
            }

            LastAppliedPosition = Math.Max(LastAppliedPosition, record.Position);
            LastAppliedTimestamp = MaxTimestamp(LastAppliedTimestamp, metadata.CommittedAt);

            try
            {
                Dispatch(record);
            }
            catch (ArgumentException)
            {
                // Contract-validation failure from an event payload (e.g., whitespace text):
                // treat as poison rather than crashing the projection pass.
                Poisoned = true;
            }
        }

        private void Dispatch(ConversationProjectionEventRecord record)
        {
            switch (record.Event)
            {
                case ConversationCreated created:
                    Apply(created);
                    break;
                case ParticipantAdded participant:
                    Apply(participant);
                    break;
                case MessageAppended message:
                    Apply(message, record.Position);
                    break;
                case FileReferenceAttached file:
                    Apply(file);
                    break;
                case ConversationMetadataUpdated update:
                    Apply(update);
                    break;
                case ConversationClosed:
                    if (_lifecycleState != ArchivedState)
                    {
                        _lifecycleState = ClosedState;
                    }

                    break;
                case ConversationArchived:
                    _lifecycleState = ArchivedState;
                    break;
                case ConversationLifecycleChanged lifecycle:
                    Apply(lifecycle);
                    break;
                case RetentionPolicySet retentionSet:
                    Apply(retentionSet);
                    break;
                case RetentionPolicyReplaced retentionReplaced:
                    Apply(retentionReplaced);
                    break;
                case ConversationContentMarkedSensitive sensitive:
                    Apply(sensitive);
                    break;
                case MessageContentRedacted redacted:
                    Apply(redacted);
                    break;
                default:
                    HasOutOfOrderEvent = true;
                    break;
            }
        }

        private static ConversationEventMetadata? TryGetMetadata(object e)
            => e switch
            {
                ConversationCreated created => created.Metadata,
                ParticipantAdded participant => participant.Metadata,
                MessageAppended message => message.Metadata,
                FileReferenceAttached file => file.Metadata,
                ConversationMetadataUpdated update => update.Metadata,
                ConversationClosed closed => closed.Metadata,
                ConversationArchived archived => archived.Metadata,
                ConversationLifecycleChanged lifecycle => lifecycle.Metadata,
                RetentionPolicySet retentionSet => retentionSet.Metadata,
                RetentionPolicyReplaced retentionReplaced => retentionReplaced.Metadata,
                ConversationContentMarkedSensitive sensitive => sensitive.Metadata,
                MessageContentRedacted redacted => redacted.Metadata,
                _ => null,
            };

        private static DateTimeOffset MaxTimestamp(DateTimeOffset? current, DateTimeOffset next)
            => current is null || next > current ? next : current.Value;

        private void Apply(ConversationCreated e)
        {
            WasCreated = true;
            if (_lifecycleState == InitializingState)
            {
                _lifecycleState = OpenState;
            }

            Label ??= e.Label;
            BusinessReference ??= e.BusinessReference;
            ProjectId ??= e.ProjectId;
            FolderId ??= e.FolderId;
            ProviderCorrelation ??= e.ProviderCorrelation;
        }

        private void Apply(ParticipantAdded e)
        {
            if (_participants.ContainsKey(e.ParticipantPartyId))
            {
                Poisoned = true;
                return;
            }

            _participants[e.ParticipantPartyId] = new ConversationParticipantProjectionV1(
                e.ParticipantPartyId,
                e.ParticipantType,
                e.ParticipantRole,
                e.Metadata.CommittedAt);
        }

        private void Apply(MessageAppended e, long position)
        {
            if (_messages.ContainsKey(e.MessageId))
            {
                Poisoned = true;
                return;
            }

            string targetKey = new GovernanceTarget(GovernedTargetKind.Message, MessageId: e.MessageId).ToTargetKey();
            string text = _redactions.TryGetValue(targetKey, out ConversationRedactionProjectionV1? redaction)
                ? redaction.Placeholder
                : e.Text;

            _messages[e.MessageId] = (position, new ConversationTimelineMessageProjectionV1(
                e.MessageId,
                e.AuthorPartyId,
                text,
                e.Metadata.CommittedAt,
                e.ProviderCorrelation));
        }

        private void Apply(FileReferenceAttached e)
        {
            if (_fileReferences.ContainsKey(e.FileId))
            {
                Poisoned = true;
                return;
            }

            _fileReferences[e.FileId] = new ConversationFileReferenceProjectionV1(
                e.FileId,
                e.FolderId,
                e.MessageId,
                e.Metadata.CommittedAt);
        }

        private void Apply(ConversationMetadataUpdated e)
        {
            // Replace-all semantics: empty/null Attributes = no-op; non-empty = full replace.
            // Producers cannot signal per-key deletion through this event.
            if (e.Label is not null)
            {
                Label = e.Label;
            }

            if (e.BusinessReference is not null)
            {
                BusinessReference = e.BusinessReference;
            }

            if (e.Attributes is null || e.Attributes.Count == 0)
            {
                return;
            }

            _attributes.Clear();
            foreach (KeyValuePair<string, string> attribute in e.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal))
            {
                _attributes[attribute.Key] = attribute.Value;
            }
        }

        private void Apply(ConversationLifecycleChanged e)
        {
            _lifecycleState = e.CurrentState.Value;
        }

        private void Apply(RetentionPolicySet e)
        {
            ActiveRetentionPolicy = new ConversationRetentionPolicyProjectionV1(
                e.PolicyReference,
                e.Rationale,
                e.Metadata.ActorPartyId,
                e.Metadata.CommittedAt,
                e.AuditEvidence);
        }

        private void Apply(RetentionPolicyReplaced e)
        {
            ActiveRetentionPolicy = new ConversationRetentionPolicyProjectionV1(
                e.PolicyReference,
                e.Rationale,
                e.Metadata.ActorPartyId,
                e.Metadata.CommittedAt,
                e.AuditEvidence,
                e.PreviousPolicyReference);
        }

        private void Apply(ConversationContentMarkedSensitive e)
        {
            _sensitivityMarks[e.Target.ToTargetKey()] = new ConversationSensitivityMarkProjectionV1(
                e.Target,
                e.Category,
                e.PolicyReference,
                e.Rationale,
                e.Metadata.ActorPartyId,
                e.Metadata.CommittedAt,
                e.AuditEvidence,
                ProjectionTrustState.Current);
        }

        private void Apply(MessageContentRedacted e)
        {
            ConversationRedactionProjectionV1 redaction = new(
                e.Target,
                e.Category,
                e.PolicyReference,
                e.Rationale,
                e.Metadata.ActorPartyId,
                e.Metadata.CommittedAt,
                e.AuditEvidence,
                ProjectionTrustState.Redacted);
            _redactions[e.Target.ToTargetKey()] = redaction;

            if (e.Target.Kind == GovernedTargetKind.Message
                && e.Target.MessageId is not null
                && _messages.TryGetValue(e.Target.MessageId, out (long Position, ConversationTimelineMessageProjectionV1 Message) existing))
            {
                _messages[e.Target.MessageId] = (
                    existing.Position,
                    new ConversationTimelineMessageProjectionV1(
                        existing.Message.MessageId,
                        existing.Message.AuthorPartyId,
                        redaction.Placeholder,
                        existing.Message.CreatedAt,
                        existing.Message.ProviderCorrelation));
            }
        }
    }
}
