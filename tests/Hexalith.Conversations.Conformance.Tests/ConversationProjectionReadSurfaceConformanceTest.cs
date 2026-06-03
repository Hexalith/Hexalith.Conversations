// <copyright file="ConversationProjectionReadSurfaceConformanceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.TenantAccess;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.3 (AC2, Task 3) — projection release-gate behavior re-expressed through the PUBLIC projection-read
/// surface (<see cref="ConversationProjectionReadService.ReadDetailAsync"/> returning
/// <c>Contracts.Projections.*</c> DTOs and <see cref="ProjectionTrustState"/>), rather than through the raw
/// <c>ConversationProjectionMaterializer.Project(...)</c> output and its Server-internal
/// <c>ConversationProjectedReadModels</c> wrapper.
///
/// The original <c>Hexalith.Conversations.Server.Tests.Projections.ConversationProjectionMaterializerTest</c>
/// asserts on the raw materializer output; Story 1.2's <c>LiveProjectionFreshnessOracleCharacterizationTest</c>
/// pins the same materializer-level freshness/redaction branches inside the oracle. Neither asserts the
/// behavior through the adopter-facing read service. This test covers what they do not: that the public read
/// path preserves redaction non-leakage, anchors governance audit evidence, and fails closed (no trust-bearing
/// projection) for every degraded materializer state.
///
/// Disposition (at-risk register): coupled-by-design-retarget-in-owning-story @ Story 2.5 (FR-6 SDK projection
/// seam). FR-6 promotes the materializer's replay/dispatch orchestration to the SDK and keeps the
/// field-selection/freshness logic; the read-service seam asserted here is the stable public surface, so the
/// owning story retargets the seed seam without dropping the behavior. Pins current behavior on <c>main</c>.
/// </summary>
public sealed class ConversationProjectionReadSurfaceConformanceTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly TenantId OtherTenant = new("tenant-other");
    private static readonly ConversationId OtherConversation = new("conversation-other");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-001");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly DateTimeOffset Started = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 5, 20, 9, 0, 10, TimeSpan.Zero);

    /// <summary>
    /// A current projection whose message was redacted returns the suppressed placeholder through the public
    /// read DTO — the redacted text never leaks across the adopter-facing read surface.
    /// </summary>
    [Fact]
    public async Task RedactedMessageShouldStaySuppressedThroughPublicReadSurface()
    {
        ConversationProjectionReadResult result = await Read(Materialize(
        [
            Event(1, Created(1)),
            Event(2, MessageAppended(2, "secret customer content")),
            Event(3, Redacted(3)),
        ]));

        result.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.IsAvailableForTrustBearingActions.ShouldBeTrue();
        ConversationDetailProjectionV1 detail = result.Projection.ShouldNotBeNull();
        detail.Messages.Single().Text.ShouldBe("[redacted]");
        detail.Messages.Single().Text.ShouldNotContain("secret", Case.Insensitive);
    }

    /// <summary>
    /// A current projection anchors governance audit evidence on the public read DTO (retention policy and
    /// sensitivity mark each carry their <see cref="GovernanceAuditEvidenceReference"/>).
    /// </summary>
    [Fact]
    public async Task GovernanceEvidenceShouldBeAnchoredThroughPublicReadSurface()
    {
        ConversationProjectionReadResult result = await Read(Materialize(
        [
            Event(1, Created(1)),
            Event(2, MessageAppended(2)),
            Event(3, RetentionSet(3)),
            Event(4, Sensitive(4)),
        ]));

        result.IsAvailableForTrustBearingActions.ShouldBeTrue();
        ConversationDetailProjectionV1 detail = result.Projection.ShouldNotBeNull();
        detail.ActiveRetentionPolicy.ShouldNotBeNull();
        detail.ActiveRetentionPolicy!.AuditEvidence.ShouldNotBeNull();
        detail.SensitivityMarks.ShouldNotBeEmpty();
        detail.SensitivityMarks.ShouldAllBe(mark => mark.AuditEvidence != null);
    }

    /// <summary>
    /// Every degraded materializer state surfaces as non-trust-bearing through the public read surface and
    /// exposes no projection detail. Pins the fail-closed read gate for stale / rebuilding / unavailable.
    /// </summary>
    /// <param name="degradedState">The degraded state fixture key.</param>
    /// <returns>A task.</returns>
    [Theory]
    [InlineData("stale")]
    [InlineData("rebuilding")]
    [InlineData("gap")]
    [InlineData("unavailable")]
    public async Task DegradedProjectionShouldNotExposeTrustBearingDetail(string degradedState)
    {
        ConversationProjectedReadModels models = degradedState switch
        {
            "stale" => Materializer().Project(Tenant, Conversation, [Event(1, Created(1))], Generated.AddMinutes(30), TimeSpan.FromMinutes(5)),
            "rebuilding" => Materializer().Project(Tenant, Conversation, [Event(1, Created(1))], Generated, TimeSpan.FromMinutes(5), isRebuilding: true),

            // A position gap (events at 1 and 3, none at 2) downgrades the projection so it is not trust-bearing.
            // AC2 names "gap" among the degraded states the public read surface must fail closed on; the internal
            // gap reason code stays plumbing-only, but the fail-closed read outcome is observable here.
            "gap" => Materializer().Project(Tenant, Conversation, [Event(1, Created(1)), Event(3, MessageAppended(3))], Generated, TimeSpan.FromMinutes(5)),
            "unavailable" => Materializer().Project(Tenant, Conversation, [Event(1, Created(1))], Generated, TimeSpan.FromMinutes(5), metadataWriteFailed: true),
            _ => throw new ArgumentOutOfRangeException(nameof(degradedState), degradedState, "Unsupported state fixture."),
        };

        ConversationProjectionReadResult result = await Read(models);

        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
        result.Projection.ShouldBeNull();
        result.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
    }

    /// <summary>
    /// A mixed-tenant poison event makes the projection unavailable and exposes no detail through the public
    /// read surface.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task PoisonProjectionShouldNotExposeDetailThroughPublicReadSurface()
    {
        ConversationProjectionReadResult result = await Read(Materialize(
        [
            Event(1, Created(1)),
            Event(2, new ParticipantAdded(
                Metadata(ConversationEventType.ParticipantAdded, 2, OtherTenant, OtherConversation),
                new PartyId("party-participant"),
                Hexalith.Conversations.Contracts.Participants.ParticipantType.Human,
                Hexalith.Conversations.Contracts.Participants.ParticipantRole.Member)),
        ]));

        result.IsAvailableForTrustBearingActions.ShouldBeFalse();
        result.Projection.ShouldBeNull();
        result.FreshnessState.ShouldNotBe(ProjectionTrustState.Current);
    }

    private static async Task<ConversationProjectionReadResult> Read(ConversationProjectedReadModels models)
    {
        ConversationProjectionReadService service = new(
            new FakeTenantAccessService(ConversationTenantAccessDecision.Allowed(
                ConversationTenantAccessRequirement.Read,
                Tenant,
                "user-001")),
            new FakeProjectionReadStore { Models = models });
        return await service.ReadDetailAsync(Tenant, "user-001", Tenant, Conversation, TestContext.Current.CancellationToken);
    }

    private static ConversationProjectedReadModels Materialize(ConversationProjectionEventRecord[] events)
        => Materializer().Project(Tenant, Conversation, events, Generated, TimeSpan.FromMinutes(5));

    private static ConversationProjectionMaterializer Materializer() => new();

    private static ConversationProjectionEventRecord Event(long position, object e) => new(position, e);

    private static ConversationCreated Created(long position)
        => new(
            Metadata(ConversationEventType.ConversationCreated, position),
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            "Case 123");

    private static MessageAppended MessageAppended(long position, string text = "Hello")
        => new(Metadata(ConversationEventType.MessageAppended, position), Message, Actor, text);

    private static MessageContentRedacted Redacted(long position)
        => new(
            Metadata(ConversationEventType.MessageContentRedacted, position),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            AuditEvidence(position, "redaction-policy-standard"));

    private static RetentionPolicySet RetentionSet(long position)
        => new(
            Metadata(ConversationEventType.RetentionPolicySet, position),
            "retention-policy-standard",
            "customer-request",
            AuditEvidence(position, "retention-policy-standard"));

    private static ConversationContentMarkedSensitive Sensitive(long position)
        => new(
            Metadata(ConversationEventType.ConversationContentMarkedSensitive, position),
            new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message),
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AuditEvidence(position, "sensitivity-policy-standard"));

    private static GovernanceAuditEvidenceReference AuditEvidence(long position, string policyReference)
        => new(new AuditEvidenceHandle("audit-evidence-001"), policyReference, Started.AddSeconds(position));

    private static ConversationEventMetadata Metadata(
        ConversationEventType eventType,
        long position,
        TenantId? tenantId = null,
        ConversationId? conversationId = null)
        => new(
            SchemaVersion.Current,
            $"event-{eventType}-{position}",
            eventType,
            tenantId ?? Tenant,
            conversationId ?? Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");

    private sealed class FakeTenantAccessService(ConversationTenantAccessDecision decision) : IConversationTenantAccessService
    {
        public ValueTask<ConversationTenantAccessDecision> CheckAccessAsync(
            ConversationTenantAccessRequirement requirement,
            TenantId? trustedTenantId,
            string? callerPrincipalId,
            TenantId? routeTenantId = null,
            TenantId? commandTenantId = null,
            TenantId? aggregateTenantId = null,
            TenantId? projectionTenantId = null,
            TenantId? idempotencyTenantId = null,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(decision);
    }

    private sealed class FakeProjectionReadStore : IConversationProjectionReadStore
    {
        public ConversationProjectedReadModels? Models { get; set; }

        public ValueTask<ConversationProjectedReadModels?> ReadAsync(
            TenantId tenantId,
            ConversationId conversationId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Models);

        public ValueTask<IReadOnlyList<ConversationSummaryProjectionV1>> ListAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<ConversationSummaryProjectionV1>>([]);
    }
}
