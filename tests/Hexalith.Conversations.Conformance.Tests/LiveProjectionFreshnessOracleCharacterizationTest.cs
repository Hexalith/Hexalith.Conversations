// <copyright file="LiveProjectionFreshnessOracleCharacterizationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.TrustStates;
using Hexalith.Conversations.Contracts.Versioning;
using Hexalith.Conversations.Server.Diagnostics;
using Hexalith.Conversations.Server.Projections;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 1.2 oracle-strengthening backfill — behavior #5 (projection freshness) and #4 (redaction replay).
///
/// In the oracle, <see cref="AdopterConformanceSuite"/>.CheckProjectionFreshness only proves the STALE
/// state (a single synthetic fixture). The live <see cref="ConversationProjectionMaterializer"/> downgrade
/// branches (stale / rebuilding / gap / unavailable) and the redaction-on-replay non-leakage are exercised
/// only in Server.Tests, which is NOT part of the oracle. A fail-open mutation that surfaced a degraded
/// projection as fresh, or that leaked redacted content on rebuild, would ride green through the oracle.
///
/// These characterization tests run the LIVE materializer and the LIVE freshness classifier from inside
/// the conformance project, pinning current observable behavior: degraded states are never trust-bearing,
/// and redacted content stays suppressed even when the message event replays after the redaction event.
/// </summary>
public sealed class LiveProjectionFreshnessOracleCharacterizationTest
{
    private static readonly TenantId Tenant = new("tenant-001");
    private static readonly ConversationId Conversation = new("conversation-001");
    private static readonly PartyId Actor = new("party-actor");
    private static readonly MessageId Message = new("message-001");
    private static readonly ProjectId Project = new("project-001");
    private static readonly FolderId Folder = new("folder-001");
    private static readonly DateTimeOffset Started = new(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A current, in-order projection is trust-bearing (positive control — proves "deny/degrade everything"
    /// is not how the materializer reaches non-trust-bearing).
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldReportCurrentProjectionAsTrustBearing()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created(1)), Event(2, MessageAppended(2))],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Current);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeTrue();
    }

    /// <summary>
    /// A projection whose lag exceeds the stale threshold surfaces as Stale and is NOT trust-bearing.
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldSurfaceStaleProjectionAsNonTrustBearing()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created(1))],
            Started.AddMinutes(30),
            TimeSpan.FromMinutes(1));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Stale);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.StaleThresholdExceeded);
        result.Summary.Freshness.IsStale.ShouldBeTrue();
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A source-position gap downgrades the projection to Rebuilding and is NOT trust-bearing.
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldSurfaceGapAsRebuildingNonTrustBearing()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created(1)), Event(3, MessageAppended(3))],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5));

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.GapDetected);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// An active rebuild downgrades the projection to Rebuilding and is NOT trust-bearing.
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldSurfaceActiveRebuildAsNonTrustBearing()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created(1))],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5),
            isRebuilding: true);

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Rebuilding);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// A metadata write failure after mutation downgrades the projection to Unavailable (not trust-bearing).
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldSurfaceMetadataWriteFailureAsUnavailable()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [Event(1, Created(1))],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5),
            metadataWriteFailed: true);

        result.Summary.Freshness.FreshnessState.ShouldBe(ProjectionTrustState.Unavailable);
        result.Summary.Freshness.ReasonCode.ShouldBe(ProjectionFreshnessReasonCode.MetadataWriteFailed);
        result.Summary.Freshness.AllowsTrustBearingDecision().ShouldBeFalse();
    }

    /// <summary>
    /// Behavior #4: redacted content stays suppressed on rebuild even when the message event replays AFTER
    /// the redaction event (temporal inversion in the rebuilt log). Non-leakage survives replay.
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldSuppressRedactedContentWhenMessageReplaysAfterRedaction()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created(1)),
                Event(2, Redacted(2, new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message))),
                Event(3, MessageAppended(3, "secret customer content")),
            ],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5));

        result.Detail.Messages.Single().Text.ShouldBe("[redacted]");
        result.Detail.Messages.Single().Text.ShouldNotContain("secret", Case.Insensitive);
        result.Detail.Redactions.Single().Target.MessageId.ShouldBe(Message);
    }

    /// <summary>
    /// Behavior #2: every governance mutation materialized by the live projection path carries its paired
    /// audit evidence in the read model (retention / sensitivity / redaction). A dropped pairing in the live
    /// materializer surfaces null audit evidence (or trips the contract's RequireNonNull) and turns this RED.
    /// </summary>
    [Fact]
    public void LiveMaterializerShouldPairEveryGovernanceMutationWithAuditEvidence()
    {
        ConversationProjectedReadModels result = new ConversationProjectionMaterializer().Project(
            Tenant,
            Conversation,
            [
                Event(1, Created(1)),
                Event(2, MessageAppended(2)),
                Event(3, RetentionSet(3)),
                Event(4, Sensitive(4, new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message))),
                Event(5, Redacted(5, new GovernanceTarget(GovernedTargetKind.Message, MessageId: Message))),
            ],
            Started.AddSeconds(10),
            TimeSpan.FromMinutes(5));

        result.Detail.ActiveRetentionPolicy.ShouldNotBeNull();
        result.Detail.ActiveRetentionPolicy!.AuditEvidence.ShouldNotBeNull();
        result.Detail.SensitivityMarks.ShouldNotBeEmpty();
        result.Detail.SensitivityMarks.ShouldAllBe(mark => mark.AuditEvidence != null);
        result.Detail.Redactions.ShouldNotBeEmpty();
        result.Detail.Redactions.ShouldAllBe(redaction => redaction.AuditEvidence != null);
    }

    /// <summary>
    /// The live freshness classifier maps degraded trust states to degraded freshness classes (never Current),
    /// and critical lag reason codes to CriticalLag. Pins the diagnostic mapping inside the oracle.
    /// </summary>
    [Fact]
    public void LiveFreshnessClassifierShouldNeverPromoteDegradedStatesToCurrent()
    {
        ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Stale, ProjectionFreshnessReasonCode.StaleThresholdExceeded)
            .ShouldBe(ConversationProjectionFreshnessClass.Stale);
        ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Rebuilding, ProjectionFreshnessReasonCode.Rebuilding)
            .ShouldBe(ConversationProjectionFreshnessClass.Rebuilding);
        ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Unavailable, ProjectionFreshnessReasonCode.Unavailable)
            .ShouldBe(ConversationProjectionFreshnessClass.Unavailable);
        ConversationProjectionFreshnessClassifier
            .Classify(ProjectionTrustState.Forbidden, ProjectionFreshnessReasonCode.Forbidden)
            .ShouldBe(ConversationProjectionFreshnessClass.Unavailable);
        ConversationProjectionFreshnessClassifier
            .ClassifyLag(ProjectionFreshnessReasonCode.GapDetected)
            .ShouldBe(ConversationProjectionLagClass.CriticalLag);
    }

    private static ConversationProjectionEventRecord Event(long position, object e)
        => new(position, e);

    private static ConversationCreated Created(long position)
        => new(
            Metadata(ConversationEventType.ConversationCreated, position),
            new BusinessReference("crm", "case-123"),
            Project,
            Folder,
            "Case 123");

    private static MessageAppended MessageAppended(long position, string text = "Hello")
        => new(
            Metadata(ConversationEventType.MessageAppended, position),
            Message,
            Actor,
            text);

    private static MessageContentRedacted Redacted(long position, GovernanceTarget target)
        => new(
            Metadata(ConversationEventType.MessageContentRedacted, position),
            target,
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

    private static ConversationContentMarkedSensitive Sensitive(long position, GovernanceTarget target)
        => new(
            Metadata(ConversationEventType.ConversationContentMarkedSensitive, position),
            target,
            SensitivityCategory.Restricted,
            "sensitivity-policy-standard",
            "customer-request",
            AuditEvidence(position, "sensitivity-policy-standard"));

    private static GovernanceAuditEvidenceReference AuditEvidence(long position, string policyReference)
        => new(
            new AuditEvidenceHandle("audit-evidence-001"),
            policyReference,
            Started.AddSeconds(position));

    private static ConversationEventMetadata Metadata(ConversationEventType eventType, long position)
        => new(
            SchemaVersion.Current,
            $"event-{eventType}-{position}",
            eventType,
            Tenant,
            Conversation,
            "correlation-001",
            Started.AddSeconds(position),
            Actor,
            "causation-001");
}
