// <copyright file="ConversationProjectionHandler.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Hexalith.Commons.Serialization;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Serialization;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>
/// Serves the conversation full-replay projection through the platform <see cref="IDomainProjectionHandler"/>
/// seam (FR-6). The generic replay/routing/discovery orchestration and the <c>/project</c> endpoint are owned by
/// the shared platform SDK; this handler decodes the request's events into the public conversation event
/// vocabulary and <b>delegates the replay loop, field selection, freshness formula, and evidence construction</b>
/// to the preserved <see cref="ConversationProjectionMaterializer"/> — the conversation-specific kept logic.
/// </summary>
/// <remarks>
/// Stateless full-replay (Model a): the handler holds no projection state between calls and rebuilds from event
/// one, so it is replay-safe and idempotent under at-least-once / out-of-order delivery (NFR5). It is registered
/// as a singleton by the platform convention scan over the Server assembly (Story 2.1) — no host edit is needed
/// to discover it. The handler is a pure seam adapter: it returns the projected read model as the opaque
/// <see cref="ProjectionResponse"/> the gateway projection actor stores and serves; it does not drive the
/// separate (query-side) persisted read-model writer (see the story Dev Agent Record for that open thread).
/// </remarks>
public sealed class ConversationProjectionHandler : IDomainProjectionHandler
{
    /// <summary>
    /// The kebab-case domain the platform routes projection requests on. This is the <b>aggregate</b> domain
    /// (singular — the platform naming convention strips the <c>Aggregate</c> suffix from
    /// <c>ConversationAggregate</c>), which is the domain the projection actor sends in the request — not the
    /// plural query namespace. The platform matches it case-insensitively.
    /// </summary>
    public const string ConversationDomain = "conversation";

    /// <summary>The stable projection type name carried on the returned projection state.</summary>
    public const string ConversationProjectionType = "conversation";

    // No production freshness-threshold configuration key exists today: the materializer's staleAfter was only
    // ever supplied by tests, and the read store serves pre-materialized models. Story 2.5 deliberately
    // introduces no new config key (out of scope — the config surface stays unchanged), reusing the established
    // steady-state threshold the projection tests already pin.
    private static readonly TimeSpan DefaultStaleAfter = TimeSpan.FromMinutes(5);

    // Public events and value objects carry attribute-based JSON converters. The generated context is queried
    // first; reflection fallback is limited to the server-owned projection wrapper.
    private static readonly JsonSerializerOptions EventJsonOptions =
        JsonSerializationOptions.CreateWeb([ConversationsJsonContext.Default], includeReflectionFallback: true);

    // The public conversation event vocabulary the kept materializer consumes, keyed by simple type name.
    // The platform delivers the event type name (short or fully qualified) on each event DTO; resolution
    // remains exact-then-suffix through the shared registry.
    private static readonly PolymorphicTypeRegistry PublicEventTypes = BuildPublicEventTypeRegistry();

    private readonly ConversationProjectionMaterializer _materializer;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversationProjectionHandler"/> class.
    /// </summary>
    /// <param name="materializer">The preserved conversation-specific materialization logic.</param>
    /// <param name="timeProvider">
    /// The clock used to source the projection generation time (deterministic-replay rule: never
    /// <see cref="DateTimeOffset.UtcNow"/> directly). Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    public ConversationProjectionHandler(ConversationProjectionMaterializer materializer, TimeProvider? timeProvider = null)
    {
        _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public string Domain => ConversationDomain;

    internal static IReadOnlyDictionary<string, Type> PublicEventTypeEntries => PublicEventTypes.Entries;

    /// <inheritdoc/>
    public ProjectionResponse Project(ProjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The aggregate identity comes from the request, never from the events (the event DTO deliberately omits
        // tenant/aggregate identity). The materializer re-checks each event's metadata against this scope and
        // fails closed (unavailable) on any tenant/conversation mismatch.
        TenantId tenantId = new(request.TenantId);
        ConversationId conversationId = new(request.AggregateId);

        // Freshness inputs the stateless seam does not carry:
        //  - projectionGeneratedAt: the injected clock (deterministic-replay rule).
        //  - staleAfter: the established steady-state threshold (no new config key — see above).
        //  - isRebuilding / metadataWriteFailed: false on the steady-state full-replay path (a stateless seam has
        //    no prior-state knowledge to infer them); rebuild and metadata-failure freshness stay exercised
        //    through ConversationProjectionRebuildVerifier and the read-service degraded-state path.
        ConversationProjectedReadModels models = _materializer.Project(
            tenantId,
            conversationId,
            DecodeEvents(request.Events),
            _timeProvider.GetUtcNow(),
            DefaultStaleAfter,
            isRebuilding: false,
            metadataWriteFailed: false);

        return new ProjectionResponse(
            ConversationProjectionType,
            JsonSerializer.SerializeToElement(models, EventJsonOptions));
    }

    private static IReadOnlyList<ConversationProjectionEventRecord> DecodeEvents(ProjectionEventDto[]? events)
    {
        if (events is null || events.Length == 0)
        {
            return [];
        }

        List<ConversationProjectionEventRecord> records = new(events.Length);
        foreach (ProjectionEventDto? evt in events)
        {
            // SequenceNumber is 1-based by contract; a non-positive value is malformed. Skip it rather than throw
            // out of the seam — the source-position gap detection below then surfaces the omission as a degraded
            // (never falsely Current) freshness state.
            if (evt is null || evt.SequenceNumber < 1)
            {
                continue;
            }

            if (!TryResolvePublicEventType(evt.EventTypeName, out Type? eventType))
            {
                // Event type outside the conversation projection vocabulary: skipped (the platform reference
                // handlers skip events that do not affect the projection). The materializer's source-position gap
                // detection still operates over the decoded sequence, so a skipped position can only degrade
                // freshness (never falsely report Current) — fail-closed by construction.
                continue;
            }

            object? decoded = evt.Payload is { Length: > 0 }
                ? JsonSerializer.Deserialize(evt.Payload, eventType, EventJsonOptions)
                : null;
            if (decoded is null)
            {
                continue;
            }

            records.Add(new ConversationProjectionEventRecord(evt.SequenceNumber, decoded));
        }

        return records;
    }

    private static bool TryResolvePublicEventType(string? eventTypeName, [NotNullWhen(true)] out Type? eventType)
        => PublicEventTypes.TryResolveExactThenSuffix(eventTypeName, out eventType);

    private static PolymorphicTypeRegistry BuildPublicEventTypeRegistry()
    {
        Type[] types =
        [
            typeof(ConversationCreated),
            typeof(MessageAppended),
            typeof(ParticipantAdded),
            typeof(FileReferenceAttached),
            typeof(ConversationMetadataUpdated),
            typeof(ConversationProjectChanged),
            typeof(ConversationClosed),
            typeof(ConversationArchived),
            typeof(ConversationLifecycleChanged),
            typeof(RetentionPolicySet),
            typeof(RetentionPolicyReplaced),
            typeof(ConversationContentMarkedSensitive),
            typeof(MessageContentRedacted),
        ];
        return PolymorphicTypeRegistry.FromTypeNames(types);
    }
}
