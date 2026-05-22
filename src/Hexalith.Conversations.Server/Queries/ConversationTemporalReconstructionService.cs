// <copyright file="ConversationTemporalReconstructionService.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Events;
using Hexalith.Conversations.Replay;
using Hexalith.Conversations.State;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

namespace Hexalith.Conversations.Server.Queries;

/// <summary>
/// Reconstructs tenant-authorized conversation state at a safe temporal anchor.
/// </summary>
public sealed class ConversationTemporalReconstructionService
{
    private readonly IConversationTemporalEventSource _eventSource;
    private readonly ConversationProjectionReadService _projectionReadService;
    private readonly IConversationTenantAccessService _tenantAccessService;

    public ConversationTemporalReconstructionService(
        IConversationTenantAccessService tenantAccessService,
        ConversationProjectionReadService projectionReadService,
        IConversationTemporalEventSource eventSource)
    {
        _tenantAccessService = tenantAccessService ?? throw new ArgumentNullException(nameof(tenantAccessService));
        _projectionReadService = projectionReadService ?? throw new ArgumentNullException(nameof(projectionReadService));
        _eventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
    }

    /// <summary>
    /// Reconstructs one historical detail result.
    /// </summary>
    public async ValueTask<ConversationTemporalDetailResult> ReconstructAsync(
        GetConversationAtPointInTimeQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ConversationTenantAccessDecision decision = await _tenantAccessService
            .CheckAccessAsync(
                ConversationTenantAccessRequirement.Read,
                query.TenantId,
                query.CallerPrincipalId,
                routeTenantId: query.TenantId,
                projectionTenantId: query.TenantId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        if (query.Anchor.TenantId != query.TenantId || query.Anchor.ConversationId != query.ConversationId)
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        if (!TryResolvePosition(query.Anchor, out long? requestedPosition))
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        ConversationProjectionReadResult currentProjection = await _projectionReadService
            .ReadDetailAsync(
                query.TenantId,
                query.CallerPrincipalId,
                query.TenantId,
                query.ConversationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (currentProjection.Projection is null)
        {
            return ProjectionUnavailableResult(query.SchemaVersion, currentProjection);
        }

        ConversationTemporalEventSourceResult source;
        try
        {
            source = await _eventSource.ReadAsync(query.TenantId, query.ConversationId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return ConversationTemporalDetailResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is available.");
        }
        catch (IOException)
        {
            return ConversationTemporalDetailResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is available.");
        }
        catch (TimeoutException)
        {
            return ConversationTemporalDetailResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is available.");
        }

        if (source.State == ConversationTemporalEventSourceState.Unavailable)
        {
            return ConversationTemporalDetailResult.Unavailable(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is available.");
        }

        if (source.State == ConversationTemporalEventSourceState.Rebuilding)
        {
            return ConversationTemporalDetailResult.Rebuilding(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Rebuilding);
        }

        if (source.State == ConversationTemporalEventSourceState.OutsideCoverage || source.Events.Count == 0)
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        if (!source.IsComplete)
        {
            return ConversationTemporalDetailResult.Rebuilding(
                query.SchemaVersion,
                ProjectionFreshnessReasonCode.Rebuilding);
        }

        if (requestedPosition is long position && position > source.Events.Max(e => e.Position))
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        IReadOnlyList<ConversationReplayEventRecord> bounded = BoundEvents(source.Events, query.Anchor, requestedPosition);
        if (bounded.Count == 0)
        {
            return ConversationTemporalDetailResult.Hidden(query.SchemaVersion);
        }

        ConversationReplayResult replay = ConversationReplayVerifier.Replay(query.TenantId, query.ConversationId, bounded);
        if (replay.Outcome != ConversationReplayOutcome.Replay || replay.State is null)
        {
            return ReplayFailureResult(query.SchemaVersion, replay.DiagnosticCode);
        }

        ConversationTemporalAnchorV1 resolvedAnchor = ResolveAuthoritativeAnchor(query.Anchor, bounded[^1]);
        ConversationTemporalConfidenceV1 confidence = new(
            query.SchemaVersion,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            true,
            "Temporal evidence is complete for the requested anchor.");

        ConversationTemporalDetailsV1 details = BuildDetails(
            query.SchemaVersion,
            replay.State,
            currentProjection.Projection,
            resolvedAnchor,
            confidence);

        return ConversationTemporalDetailResult.Visible(
            query.SchemaVersion,
            details,
            "Use the returned temporal anchor for stable historical evidence.");
    }

    private static ConversationTemporalDetailResult ProjectionUnavailableResult(
        SchemaVersion schemaVersion,
        ConversationProjectionReadResult currentProjection)
    {
        if (currentProjection.FreshnessState == ProjectionTrustState.Forbidden)
        {
            return ConversationTemporalDetailResult.Hidden(schemaVersion);
        }

        if (currentProjection.FreshnessState == ProjectionTrustState.Rebuilding)
        {
            return ConversationTemporalDetailResult.Rebuilding(schemaVersion, currentProjection.ReasonCode);
        }

        return ConversationTemporalDetailResult.Unavailable(
            schemaVersion,
            currentProjection.ReasonCode == ProjectionFreshnessReasonCode.Forbidden
                ? ProjectionFreshnessReasonCode.Unavailable
                : currentProjection.ReasonCode,
            "Retry after current disclosure policy is available.");
    }

    private static ConversationTemporalDetailResult ReplayFailureResult(SchemaVersion schemaVersion, string? diagnosticCode)
    {
        return diagnosticCode switch
        {
            "event_position_gap" => ConversationTemporalDetailResult.Rebuilding(
                schemaVersion,
                ProjectionFreshnessReasonCode.GapDetected),
            "event_position_reordered" => ConversationTemporalDetailResult.Rebuilding(
                schemaVersion,
                ProjectionFreshnessReasonCode.OutOfOrderEvent),
            "unsupported_schema_version" => ConversationTemporalDetailResult.Unavailable(
                schemaVersion,
                ProjectionFreshnessReasonCode.Unavailable,
                "Retry after temporal evidence is migrated."),
            _ => ConversationTemporalDetailResult.Hidden(schemaVersion),
        };
    }

    private static bool TryResolvePosition(ConversationTemporalAnchorV1 anchor, out long? requestedPosition)
    {
        requestedPosition = null;
        if (anchor.AnchorKind == ConversationTemporalAnchorV1.TimestampKind)
        {
            return true;
        }

        if (anchor.AnchorKind == ConversationTemporalAnchorV1.SafeSourcePositionKind)
        {
            requestedPosition = anchor.SafeSourcePosition;
            return true;
        }

        string? cursor = anchor.AnchorKind == ConversationTemporalAnchorV1.ProjectionCursorKind
            ? anchor.ProjectionCursor
            : anchor.ContractCursor;

        return TryParsePositionCursor(cursor, out requestedPosition);
    }

    private static bool TryParsePositionCursor(string? cursor, out long? requestedPosition)
    {
        requestedPosition = null;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        string value = cursor.StartsWith("temporal:v1:", StringComparison.Ordinal)
            ? cursor["temporal:v1:".Length..]
            : cursor;

        if (!value.StartsWith("pos:", StringComparison.Ordinal))
        {
            return false;
        }

        if (!long.TryParse(value["pos:".Length..], out long position) || position < 1)
        {
            return false;
        }

        requestedPosition = position;
        return true;
    }

    private static IReadOnlyList<ConversationReplayEventRecord> BoundEvents(
        IReadOnlyList<ConversationReplayEventRecord> events,
        ConversationTemporalAnchorV1 anchor,
        long? requestedPosition)
    {
        IEnumerable<ConversationReplayEventRecord> ordered = events.OrderBy(e => e.Position);

        if (anchor.AnchorKind == ConversationTemporalAnchorV1.TimestampKind)
        {
            DateTimeOffset timestamp = anchor.Timestamp!.Value;
            return ordered
                .Where(e => Metadata(e.Event)?.CommittedAt <= timestamp)
                .ToArray();
        }

        long position = requestedPosition.GetValueOrDefault();
        return ordered.Where(e => e.Position <= position).ToArray();
    }

    private static ConversationTemporalAnchorV1 ResolveAuthoritativeAnchor(
        ConversationTemporalAnchorV1 requested,
        ConversationReplayEventRecord last)
    {
        return new(
            requested.SchemaVersion,
            requested.TenantId,
            requested.ConversationId,
            ConversationTemporalAnchorV1.SafeSourcePositionKind,
            SafeSourcePosition: last.Position,
            ContractCursor: null,
            ProjectionCursor: null,
            Timestamp: null);
    }

    private static ConversationTemporalDetailsV1 BuildDetails(
        SchemaVersion schemaVersion,
        ConversationState state,
        ConversationDetailProjectionV1 currentProjection,
        ConversationTemporalAnchorV1 anchor,
        ConversationTemporalConfidenceV1 confidence)
    {
        IReadOnlyDictionary<string, ConversationRedactionProjectionV1> currentRedactions = currentProjection.Redactions
            .ToDictionary(redaction => redaction.Target.ToTargetKey(), StringComparer.Ordinal);

        IReadOnlyList<ConversationTimelineMessageProjectionV1> messages = state.Messages
            .Select(message => CurrentPolicyMessage(message, currentRedactions))
            .ToArray();

        return new(
            schemaVersion,
            state.TenantId!,
            state.ConversationId!,
            anchor,
            confidence,
            currentProjection.Freshness,
            state.Lifecycle.ToString(),
            state.Label,
            state.BusinessReference,
            state.ProjectId,
            state.FolderId,
            ConversationProviderCorrelationV1.From(state.ProviderCorrelation),
            state.Participants.Select(p => new ConversationParticipantProjectionV1(p.PartyId, p.ParticipantType, p.ParticipantRole)).ToArray(),
            messages,
            state.FileReferences.Select(f => new ConversationFileReferenceProjectionV1(f.FileId, f.FolderId, f.MessageId)).ToArray(),
            Retention(state.ActiveRetentionPolicy),
            currentRedactions.Count == 0 ? "Applied" : "SuppressedByCurrentDisclosurePolicy",
            state.Attributes,
            state.SensitivityMarks.Select(Sensitivity).ToArray(),
            currentProjection.Redactions);
    }

    private static ConversationTimelineMessageProjectionV1 CurrentPolicyMessage(
        ConversationMessage message,
        IReadOnlyDictionary<string, ConversationRedactionProjectionV1> currentRedactions)
    {
        string key = new GovernanceTarget(GovernedTargetKind.Message, MessageId: message.MessageId).ToTargetKey();
        string text = currentRedactions.TryGetValue(key, out ConversationRedactionProjectionV1? redaction)
            ? redaction.Placeholder
            : message.Text;

        return new(
            message.MessageId,
            message.AuthorPartyId,
            text,
            message.CreatedAt,
            message.ProviderCorrelation);
    }

    private static ConversationRetentionPolicyProjectionV1? Retention(ConversationRetentionPolicyState? state)
        => state is null
            ? null
            : new ConversationRetentionPolicyProjectionV1(
                state.PolicyReference,
                state.Rationale,
                state.ActorPartyId,
                state.AppliedAt,
                state.AuditEvidence,
                state.PreviousPolicyReference);

    private static ConversationSensitivityMarkProjectionV1 Sensitivity(ConversationSensitivityMarkState state)
        => new(
            state.Target,
            state.Category,
            state.PolicyReference,
            state.Rationale,
            state.ActorPartyId,
            state.MarkedAt,
            state.AuditEvidence,
            ProjectionTrustState.Current);

    private static ConversationEventMetadata? Metadata(object e)
        => e switch
        {
            ConversationCreatedDomainEvent created => created.Metadata,
            ParticipantAddedDomainEvent participant => participant.Metadata,
            RetentionPolicySetDomainEvent retentionSet => retentionSet.Metadata,
            RetentionPolicyReplacedDomainEvent retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitiveDomainEvent sensitive => sensitive.Metadata,
            MessageContentRedactedDomainEvent redacted => redacted.Metadata,
            ConversationCreated created => created.Metadata,
            ParticipantAdded participant => participant.Metadata,
            RetentionPolicySet retentionSet => retentionSet.Metadata,
            RetentionPolicyReplaced retentionReplaced => retentionReplaced.Metadata,
            ConversationContentMarkedSensitive sensitive => sensitive.Metadata,
            MessageContentRedacted redacted => redacted.Metadata,
            MessageAppended message => message.Metadata,
            FileReferenceAttached file => file.Metadata,
            ConversationMetadataUpdated update => update.Metadata,
            ConversationClosed closed => closed.Metadata,
            ConversationArchived archived => archived.Metadata,
            ConversationLifecycleChanged lifecycle => lifecycle.Metadata,
            _ => null,
        };
}
