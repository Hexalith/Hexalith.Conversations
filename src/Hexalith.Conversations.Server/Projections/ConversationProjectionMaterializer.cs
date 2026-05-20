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
    private const string OpenState = "Open";
    private const string RebuildingState = "Rebuilding";

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

        string lifecycleState = builder.LifecycleState;
        if (freshness.FreshnessState == ProjectionTrustState.Rebuilding && lifecycleState == RebuildingState)
        {
            lifecycleState = RebuildingState;
        }

        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            tenantId,
            conversationId,
            freshness,
            lifecycleState,
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
            lifecycleState,
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
        long position = Math.Max(builder.LastAppliedPosition, 1);
        DateTimeOffset lastApplied = builder.LastAppliedTimestamp ?? projectionGeneratedAt;
        bool contradictoryTimestamp = projectionGeneratedAt < lastApplied;
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
        private readonly Dictionary<MessageId, ConversationTimelineMessageProjectionV1> _messages = [];
        private readonly Dictionary<PartyId, ConversationParticipantProjectionV1> _participants = [];
        private readonly HashSet<string> _processedEventIds = new(StringComparer.Ordinal);
        private string _lifecycleState = RebuildingState;

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
                .OrderBy(message => message.CreatedAt)
                .ThenBy(message => message.MessageId.Value, StringComparer.Ordinal)
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

        public bool WasCreated { get; private set; }

        public void Apply(ConversationProjectionEventRecord record)
        {
            ConversationEventMetadata metadata = GetMetadata(record.Event);
            if (!_processedEventIds.Add(metadata.EventId))
            {
                return;
            }

            if (!tenantId.Equals(metadata.TenantId) || !conversationId.Equals(metadata.ConversationId))
            {
                Poisoned = true;
                return;
            }

            if (LastAppliedPosition > 0 && record.Position != LastAppliedPosition + 1)
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

            switch (record.Event)
            {
                case ConversationCreated created:
                    Apply(created);
                    break;
                case ParticipantAdded participant:
                    Apply(participant);
                    break;
                case MessageAppended message:
                    Apply(message);
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
                default:
                    HasOutOfOrderEvent = true;
                    break;
            }
        }

        private static ConversationEventMetadata GetMetadata(object e)
            => e switch
            {
                ConversationCreated created => created.Metadata,
                ParticipantAdded participant => participant.Metadata,
                MessageAppended message => message.Metadata,
                FileReferenceAttached file => file.Metadata,
                ConversationMetadataUpdated update => update.Metadata,
                ConversationClosed closed => closed.Metadata,
                ConversationArchived archived => archived.Metadata,
                _ => throw new ArgumentException("Unsupported conversation projection event.", nameof(e)),
            };

        private static DateTimeOffset MaxTimestamp(DateTimeOffset? current, DateTimeOffset next)
            => current is null || next > current ? next : current.Value;

        private void Apply(ConversationCreated e)
        {
            WasCreated = true;
            if (_lifecycleState == RebuildingState)
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
            => _participants[e.ParticipantPartyId] = new ConversationParticipantProjectionV1(
                e.ParticipantPartyId,
                e.ParticipantType,
                e.ParticipantRole);

        private void Apply(MessageAppended e)
            => _messages[e.MessageId] = new ConversationTimelineMessageProjectionV1(
                e.MessageId,
                e.AuthorPartyId,
                e.Text,
                e.Metadata.CommittedAt,
                e.ProviderCorrelation);

        private void Apply(FileReferenceAttached e)
            => _fileReferences[e.FileId] = new ConversationFileReferenceProjectionV1(
                e.FileId,
                e.FolderId,
                e.MessageId);

        private void Apply(ConversationMetadataUpdated e)
        {
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
    }
}
