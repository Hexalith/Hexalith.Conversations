// <copyright file="TemporalReconstructionContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Governance;
using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Contracts.TrustStates;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies point-in-time reconstruction contracts remain safe and serializable.
/// </summary>
public sealed class TemporalReconstructionContractTest
{
    [Fact]
    public void TemporalResultShouldSerializeWithoutInfrastructureVocabulary()
    {
        ConversationTemporalAnchorV1 anchor = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ConversationTemporalAnchorV1.CompositeCursorKind,
            SafeSourcePosition: 42,
            ProjectionCursor: ContractSamples.FreshnessV1.ProjectionCursor,
            ContractCursor: "temporal:v1:pos:0000000042:projection:0000000042",
            ProjectionVersion: ContractSamples.FreshnessV1.LastAppliedEventPosition,
            SupportingTimestamp: ContractSamples.EventMetadata.CommittedAt);
        ConversationTemporalConfidenceV1 confidence = new(
            ContractSamples.Version,
            ProjectionTrustState.Current,
            ProjectionFreshnessReasonCode.Current,
            true,
            "Temporal evidence is complete for the requested anchor.");
        ConversationTemporalDetailsV1 details = new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            anchor,
            confidence,
            ContractSamples.FreshnessV1,
            "Open",
            Messages:
            [
                new(
                    ContractSamples.Message,
                    ContractSamples.Actor,
                    "[redacted]",
                    ContractSamples.EventMetadata.CommittedAt),
            ],
            Redactions:
            [
                new ConversationRedactionProjectionV1(
                    ContractSamples.RedactionMessageTarget,
                    RedactionCategory.ContentSuppression,
                    "redaction-policy-standard",
                    "customer-request",
                    ContractSamples.Actor,
                    ContractSamples.GovernanceTimestamp,
                    ContractSamples.AuditEvidence,
                    ProjectionTrustState.Current),
            ]);

        ConversationTemporalDetailResult result = ConversationTemporalDetailResult.Visible(
            ContractSamples.Version,
            details,
            "Use the returned temporal anchor for stable historical evidence.");

        string json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.ShouldContain("authoritativeTemporalAnchor");
        json.ShouldContain("safeSourcePosition");
        json.ShouldContain("projectionCursor");
        json.ShouldContain("projectionVersion");
        json.ShouldContain("supportingTimestamp");
        json.ShouldContain("confidence");
        json.ShouldContain("redactions");
        json.ShouldNotContain("EventStore", Case.Insensitive);
        json.ShouldNotContain("stream", Case.Insensitive);
        json.ShouldNotContain("snapshot", Case.Insensitive);
        json.ShouldNotContain("raw", Case.Insensitive);
        json.ShouldNotContain("secret-before-redaction", Case.Insensitive);
    }

    [Theory]
    [InlineData(ConversationTemporalAnchorV1.TimestampKind)]
    [InlineData(ConversationTemporalAnchorV1.SafeSourcePositionKind)]
    [InlineData(ConversationTemporalAnchorV1.ProjectionCursorKind)]
    [InlineData(ConversationTemporalAnchorV1.ContractCursorKind)]
    [InlineData(ConversationTemporalAnchorV1.CompositeCursorKind)]
    public void TemporalAnchorShouldAcceptSupportedForms(string kind)
    {
        ConversationTemporalAnchorV1 anchor = kind switch
        {
            ConversationTemporalAnchorV1.TimestampKind => new(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                kind,
                Timestamp: ContractSamples.GovernanceTimestamp),
            ConversationTemporalAnchorV1.SafeSourcePositionKind => new(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                kind,
                SafeSourcePosition: 7),
            ConversationTemporalAnchorV1.ProjectionCursorKind => new(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                kind,
                ProjectionCursor: "pos:0000000007"),
            ConversationTemporalAnchorV1.ContractCursorKind => new(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                kind,
                ContractCursor: "temporal:v1:pos:0000000007"),
            ConversationTemporalAnchorV1.CompositeCursorKind => new(
                ContractSamples.Version,
                ContractSamples.Tenant,
                ContractSamples.Conversation,
                kind,
                SafeSourcePosition: 7,
                ProjectionCursor: "pos:0000000007",
                ContractCursor: "temporal:v1:pos:0000000007:projection:0000000007",
                ProjectionVersion: 7,
                SupportingTimestamp: ContractSamples.EventMetadata.CommittedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported test row."),
        };

        anchor.AnchorKind.ShouldBe(kind);
    }

    [Fact]
    public void TemporalAnchorShouldRejectAmbiguousForms()
    {
        Should.Throw<ArgumentException>(() => new ConversationTemporalAnchorV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ConversationTemporalAnchorV1.TimestampKind,
            Timestamp: ContractSamples.GovernanceTimestamp,
            SafeSourcePosition: 1));

        Should.Throw<ArgumentException>(() => new ConversationTemporalAnchorV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            "stream-position",
            ProjectionCursor: "pos:0000000001"));

        Should.Throw<ArgumentException>(() => new ConversationTemporalAnchorV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ConversationTemporalAnchorV1.CompositeCursorKind,
            SafeSourcePosition: 7,
            ProjectionCursor: ContractSamples.FreshnessV1.ProjectionCursor,
            ContractCursor: "temporal:v1:pos:0000000008",
            ProjectionVersion: ContractSamples.FreshnessV1.LastAppliedEventPosition));

        Should.Throw<ArgumentException>(() => new ConversationTemporalAnchorV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ConversationTemporalAnchorV1.CompositeCursorKind,
            SafeSourcePosition: 7,
            ProjectionCursor: "pos:0000000007",
            ContractCursor: "temporal:v1:pos:0000000007",
            ProjectionVersion: 7));

        Should.Throw<ArgumentException>(() => new ConversationTemporalAnchorV1(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ConversationTemporalAnchorV1.CompositeCursorKind,
            SafeSourcePosition: 7,
            ProjectionCursor: "pos:0000000008",
            ContractCursor: "temporal:v1:pos:0000000007:projection:0000000008",
            ProjectionVersion: 9));
    }

    [Fact]
    public void HiddenTemporalResultShouldKeepContentSafeShape()
    {
        ConversationTemporalDetailResult hidden = ConversationTemporalDetailResult.Hidden(ContractSamples.Version);

        hidden.Details.ShouldBeNull();
        hidden.AuthoritativeTemporalAnchor.ShouldBeNull();
        hidden.FreshnessState.ShouldBe(ProjectionTrustState.Forbidden);
        hidden.SafeNextAction.ShouldBe("The requested historical view is not available.");
    }
}
