// <copyright file="ConversationProjectionMaterializer.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
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
            builder.ProviderCorrelation);

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
            builder.Attributes);

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

    private sealed class ProjectionBuilder(TenantId tenantId, ConversationId conversationId)
    {
        private readonly Dictionary<string, string> _attributes = new(StringComparer.Ordinal);
        private readonly Dictionary<FileId, ConversationFileReferenceProjectionV1> _fileReferences = [];
        private readonly Dictionary<MessageId, (long Position, ConversationTimelineMessageProjectionV1 Message)> _messages = [];
        private readonly Dictionary<PartyId, ConversationParticipantProjectionV1> _participants = [];
        private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);
        private string _lifecycleState = InitializingState;

        public IReadOnlyDictionary<string, string> Attributes
            => new Dictionary<string, string>(
                _attributes.OrderBy(attribute => attribute.Key, StringComparer.Ordinal),
                StringComparer.Ordinal);

        public BusinessReference? BusinessReference { get; private set; }

        public IReadOnlyList<ConversationFileReferenceProjectionV1> FileReferences
            => _fileReferences
                .Values
                .OrderBy(reference => reference.FileId.Value, StringComparer.Ordinal)
                .ToArray();

        public FolderId? FolderId { get; private set; }

        public bool HasGap { get; private set; }

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
                e.ParticipantRole);
        }

        private void Apply(MessageAppended e, long position)
        {
            if (_messages.ContainsKey(e.MessageId))
            {
                Poisoned = true;
                return;
            }

            _messages[e.MessageId] = (position, new ConversationTimelineMessageProjectionV1(
                e.MessageId,
                e.AuthorPartyId,
                e.Text,
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
                e.MessageId);
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
    }
}
