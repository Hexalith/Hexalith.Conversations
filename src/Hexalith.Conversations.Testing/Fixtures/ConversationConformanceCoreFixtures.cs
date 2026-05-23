// <copyright file="ConversationConformanceCoreFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Participants;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Provides the deterministic, synthetic, content-safe CORE fixture exercised by the adopter-facing
/// conformance suite.
/// </summary>
/// <remarks>
/// The fixture reuses existing projection, error, and identifier contracts rather than inventing a parallel
/// transcript model, mirroring the <see cref="BuyerAcceptanceDemoFixtures"/> synthetic-fixture pattern from
/// Story 3.7. It provides one authorized tenant-scoped happy-path conversation (participants, message
/// attribution, business references, and trust-bearing <c>Current</c> projection freshness) plus typed
/// failure cases covering unsupported schema/version, stale projection, cross-tenant denial (hidden shape),
/// duplicate-command idempotency conflict, and a sanitized error-envelope case. All data is synthetic and
/// clearly marked; unique cross-tenant poison sentinel values must never appear in any authorized-tenant
/// client-observable surface, safe label, copied text, diagnostics text, or conformance summary. Loading the
/// fixture appends no events, mutates no aggregate state, writes no production projection store, persists no
/// export artifact, and requires no nested submodule initialization.
/// </remarks>
public static class ConversationConformanceCoreFixtures
{
    /// <summary>
    /// Marks every client-observable fixture as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    private static readonly DateTimeOffset GeneratedAtUtc = new(2026, 5, 23, 9, 0, 0, TimeSpan.Zero);
    private static readonly TenantId AuthorizedTenant = new("authorized-conformance-scope");
    private static readonly TenantId PoisonTenant = new("poison-conformance-scope");
    private static readonly PartyId Actor = new("party-conformance-actor");
    private static readonly PartyId HumanParticipant = new("party-conformance-human");
    private static readonly PartyId AssistantParticipant = new("party-conformance-assistant");
    private static readonly BusinessReference Business = new("conformance-crm", "case-conformance-001");
    private static readonly ProjectId Project = new("project-conformance");
    private static readonly FolderId Folder = new("folder-conformance");
    private static readonly MessageId Message = new("message-conformance-001");

    /// <summary>
    /// Creates the deterministic synthetic CORE fixture data set.
    /// </summary>
    /// <returns>The complete synthetic CORE fixture data set.</returns>
    public static ConversationConformanceCoreSeedData Create()
    {
        ProjectionFreshnessV1 currentFreshness = Freshness(ProjectionTrustState.Current, isStale: false);
        ProjectionFreshnessV1 staleFreshness = Freshness(ProjectionTrustState.Stale, isStale: true);

        ConversationSummaryProjectionV1 happySummary = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            HappyConversationId,
            currentFreshness,
            "Open",
            "Synthetic conformance conversation",
            Business,
            Project,
            Folder,
            [Actor, HumanParticipant, AssistantParticipant],
            MessageCount: 1,
            FileReferenceCount: 0);

        ConversationDetailProjectionV1 happyDetail = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            HappyConversationId,
            currentFreshness,
            "Open",
            "Synthetic conformance conversation",
            Business,
            Project,
            Folder,
            Participants:
            [
                new ConversationParticipantProjectionV1(HumanParticipant, ParticipantType.Human, ParticipantRole.Facilitator),
                new ConversationParticipantProjectionV1(AssistantParticipant, ParticipantType.AiAgent, ParticipantRole.Member),
            ],
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    Message,
                    Actor,
                    "Synthetic conformance message with stable Party attribution.",
                    GeneratedAtUtc),
            ]);

        ConversationDetailProjectionV1 staleDetail = new(
            SchemaVersion.Current,
            AuthorizedTenant,
            StaleConversationId,
            staleFreshness,
            "Open",
            "Synthetic stale conformance conversation",
            Business,
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    Message,
                    Actor,
                    "Synthetic stale conformance message.",
                    GeneratedAtUtc),
            ]);

        ConversationConformanceTypedFailure unsupportedSchema = TypedFailure(
            "unsupported",
            ConversationErrorCatalog.CreateError(
                ConversationErrorCode.SchemaVersionUnsupported,
                "conformance-unsupported",
                developerGuidance: "Use the active v1 contracts package and client package."));
        ConversationConformanceTypedFailure idempotencyConflict = TypedFailure(
            "duplicate-command",
            ConversationErrorCatalog.CreateError(
                ConversationErrorCode.IdempotencyConflict,
                "conformance-duplicate-command"));
        ConversationConformanceTypedFailure crossTenantDenial = TypedFailure(
            "cross-tenant",
            ConversationErrorCatalog.CreateError(
                ConversationErrorCode.AggregateNotFound,
                "conformance-cross-tenant"));
        ConversationConformanceTypedFailure sanitizedError = TypedFailure(
            "sanitized-error",
            ConversationErrorCatalog.CreateError(
                ConversationErrorCode.TenantIsolationViolation,
                "conformance-sanitized-error",
                safeFieldDiagnostics: new Dictionary<string, string> { ["target"] = "hidden" },
                developerGuidance: "The requested operation was not accepted."));

        return new ConversationConformanceCoreSeedData(
            GeneratedAtUtc,
            AuthorizedTenant,
            PoisonTenant,
            SyntheticDataMarker,
            happySummary,
            happyDetail,
            staleDetail,
            unsupportedSchema,
            idempotencyConflict,
            crossTenantDenial,
            sanitizedError,
            PoisonProjection(),
            ["POISON-SENTINEL-conformance-alpha", "POISON-SENTINEL-conformance-beta"]);
    }

    private static ConversationId HappyConversationId => new("conversation-conformance-happy");

    private static ConversationId StaleConversationId => new("conversation-conformance-stale");

    private static ConversationConformanceTypedFailure TypedFailure(string scenario, ConversationError error)
        => new(scenario, error);

    private static ProjectionFreshnessV1 Freshness(ProjectionTrustState state, bool isStale)
        => new(
            SchemaVersion.Current,
            $"pos:conformance-{state.Value.ToLowerInvariant()}",
            100,
            GeneratedAtUtc.AddSeconds(-1),
            GeneratedAtUtc,
            TimeSpan.FromSeconds(1),
            isStale,
            state,
            state == ProjectionTrustState.Current
                ? ProjectionFreshnessReasonCode.Current
                : ProjectionFreshnessReasonCode.StaleThresholdExceeded);

    private static ConversationConformanceProjectionPair PoisonProjection()
    {
        ConversationId conversationId = new("conversation-conformance-poison");
        ProjectionFreshnessV1 freshness = new(
            SchemaVersion.Current,
            "pos:conformance-poison",
            666,
            GeneratedAtUtc.AddSeconds(-1),
            GeneratedAtUtc,
            TimeSpan.FromSeconds(1),
            IsStale: false,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current);
        ConversationSummaryProjectionV1 summary = new(
            SchemaVersion.Current,
            PoisonTenant,
            conversationId,
            freshness,
            "Open",
            "POISON-SENTINEL-conformance-alpha",
            Business,
            ParticipantPartyIds: [new PartyId("party-conformance-poison")],
            MessageCount: 1);
        ConversationDetailProjectionV1 detail = new(
            SchemaVersion.Current,
            PoisonTenant,
            conversationId,
            freshness,
            "Open",
            "POISON-SENTINEL-conformance-beta",
            Business,
            Messages:
            [
                new ConversationTimelineMessageProjectionV1(
                    new MessageId("message-conformance-poison"),
                    new PartyId("party-conformance-poison"),
                    "POISON-SENTINEL-conformance-alpha",
                    GeneratedAtUtc),
            ]);

        return new ConversationConformanceProjectionPair(summary, detail);
    }
}

/// <summary>
/// Carries the deterministic synthetic CORE fixture data for the adopter-facing conformance suite.
/// </summary>
/// <param name="GeneratedAtUtc">The deterministic UTC generation timestamp.</param>
/// <param name="AuthorizedTenantId">The authorized synthetic tenant scope.</param>
/// <param name="PoisonTenantId">The cross-tenant poison scope that must never leak into authorized output.</param>
/// <param name="SyntheticDataMarker">The synthetic-data marker applied to fixtures.</param>
/// <param name="HappyPathSummary">The authorized happy-path summary projection.</param>
/// <param name="HappyPathDetail">The authorized happy-path detail projection with participants and message attribution.</param>
/// <param name="StaleDetail">The authorized stale (non-trust-bearing) detail projection.</param>
/// <param name="UnsupportedSchemaFailure">The unsupported schema/version typed failure case.</param>
/// <param name="IdempotencyConflictFailure">The duplicate-command idempotency conflict typed failure case.</param>
/// <param name="CrossTenantDenialFailure">The cross-tenant denial (hidden shape) typed failure case.</param>
/// <param name="SanitizedErrorFailure">The sanitized error-envelope typed failure case.</param>
/// <param name="PoisonProjection">The cross-tenant poison projection pair.</param>
/// <param name="PoisonSentinelValues">The unique poison sentinel values scanned by content-safety tests.</param>
public sealed record ConversationConformanceCoreSeedData(
    DateTimeOffset GeneratedAtUtc,
    TenantId AuthorizedTenantId,
    TenantId PoisonTenantId,
    string SyntheticDataMarker,
    ConversationSummaryProjectionV1 HappyPathSummary,
    ConversationDetailProjectionV1 HappyPathDetail,
    ConversationDetailProjectionV1 StaleDetail,
    ConversationConformanceTypedFailure UnsupportedSchemaFailure,
    ConversationConformanceTypedFailure IdempotencyConflictFailure,
    ConversationConformanceTypedFailure CrossTenantDenialFailure,
    ConversationConformanceTypedFailure SanitizedErrorFailure,
    ConversationConformanceProjectionPair PoisonProjection,
    IReadOnlyList<string> PoisonSentinelValues);

/// <summary>
/// Carries one synthetic summary/detail projection pair without depending on server storage types.
/// </summary>
/// <param name="Summary">The summary projection.</param>
/// <param name="Detail">The detail projection.</param>
public sealed record ConversationConformanceProjectionPair(
    ConversationSummaryProjectionV1 Summary,
    ConversationDetailProjectionV1 Detail);

/// <summary>
/// Carries one synthetic typed failure case reusing the shared <see cref="ConversationError"/> envelope.
/// </summary>
/// <param name="Scenario">The bounded scenario identifier the failure exercises.</param>
/// <param name="Error">The shared typed error.</param>
public sealed record ConversationConformanceTypedFailure(string Scenario, ConversationError Error);
