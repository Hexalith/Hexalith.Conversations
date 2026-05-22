// <copyright file="RedactionContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Errors;
using Hexalith.Conversations.Contracts.Events;
using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies public redaction contracts stay content-safe and serializable.
/// </summary>
public sealed class RedactionContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// A redaction command carries only governed metadata, target identity, category, and policy attribution.
    /// </summary>
    [Fact]
    public void RedactionCommandShouldRoundTripWithoutRawContent()
    {
        RedactMessageContentCommand command = ContractSamples.RedactionCommand;

        string json = JsonSerializer.Serialize(command, WebOptions);
        RedactMessageContentCommand? roundTrip =
            JsonSerializer.Deserialize<RedactMessageContentCommand>(json, WebOptions);

        roundTrip.ShouldNotBeNull();
        roundTrip.Target.Kind.ShouldBe(GovernedTargetKind.Message);
        roundTrip.Target.MessageId.ShouldBe(ContractSamples.Message);
        roundTrip.Category.ShouldBe(RedactionCategory.ContentSuppression);
        json.ShouldContain("\"category\":\"ContentSuppression\"", Case.Sensitive);
        json.ShouldNotContain("Hello", Case.Insensitive);
        json.ShouldNotContain("provider", Case.Insensitive);
        json.ShouldNotContain("storage", Case.Insensitive);
        json.ShouldNotContain("EventStore", Case.Insensitive);
    }

    /// <summary>
    /// Content segment redaction targets may carry only bounded opaque references.
    /// </summary>
    [Fact]
    public void SegmentTargetShouldRejectUnsafeContentReferences()
    {
        Should.Throw<ArgumentException>(() => new RedactMessageContentCommand(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "selected text"),
            RedactionCategory.DisplayMask,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.GovernanceTimestamp));

        RedactMessageContentCommand command = new(
            ContractSamples.CommandMetadata,
            ContractSamples.Conversation,
            new GovernanceTarget(GovernedTargetKind.ContentSegment, SegmentReference: "segment-001"),
            RedactionCategory.DisplayMask,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.GovernanceTimestamp);

        command.Target.SegmentReference.ShouldBe("segment-001");
    }

    /// <summary>
    /// Public redaction events and results omit raw rationale/policy details from ToString output.
    /// </summary>
    [Fact]
    public void RedactionRecordsShouldKeepToStringContentSafe()
    {
        MessageContentRedacted redacted = new(
            ContractSamples.RedactionEventMetadata,
            ContractSamples.RedactionMessageTarget,
            RedactionCategory.ContentSuppression,
            "redaction-policy-standard",
            "customer-request",
            ContractSamples.AuditEvidence);

        redacted.ToString().ShouldNotContain("customer-request", Case.Insensitive);
        redacted.ToString().ShouldNotContain("redaction-policy-standard", Case.Insensitive);
        ContractSamples.RedactionCommand.ToString().ShouldNotContain("customer-request", Case.Insensitive);
        ContractSamples.RedactionCommand.ToString().ShouldNotContain("redaction-policy-standard", Case.Insensitive);
    }

    /// <summary>
    /// Documented redaction outcomes survive JSON round trips without leaking governance rationale or substrate details.
    /// </summary>
    [Fact]
    public void RedactionResultsShouldRoundTripDocumentedOutcomes()
    {
        ConversationRedactionResult[] results =
        [
            Result(GovernanceOutcome.Succeeded, RedactionCategory.ContentSuppression, "correlation-success", ContractSamples.AuditEvidence),
            Result(GovernanceOutcome.Denied, null, "correlation-denied", errorCode: ConversationErrorCode.TenantIsolationViolation),
            Result(
                GovernanceOutcome.AuditUnavailableFailed,
                null,
                "correlation-audit-unavailable",
                errorCode: ConversationErrorCode.AuditSinkUnavailable,
                remediation: GovernanceRemediation.RetryWhenAuditAvailable),
            Result(
                GovernanceOutcome.PolicyBlocked,
                null,
                "correlation-policy-blocked",
                errorCode: ConversationErrorCode.CommandValidationFailed,
                remediation: GovernanceRemediation.WaitForLegalHoldRelease),
            Result(
                GovernanceOutcome.Denied,
                null,
                "correlation-unsupported-target",
                errorCode: ConversationErrorCode.CommandValidationFailed,
                remediation: GovernanceRemediation.ResubmitWithPolicyReference),
            Result(GovernanceOutcome.Succeeded, RedactionCategory.ContentSuppression, "correlation-already-redacted", ContractSamples.AuditEvidence),
            Result(
                GovernanceOutcome.Denied,
                null,
                "correlation-idempotency-conflict",
                errorCode: ConversationErrorCode.IdempotencyConflict,
                remediation: GovernanceRemediation.ResubmitWithPolicyReference),
        ];

        foreach (ConversationRedactionResult result in results)
        {
            string json = JsonSerializer.Serialize(result, WebOptions);
            ConversationRedactionResult? roundTrip = JsonSerializer.Deserialize<ConversationRedactionResult>(json, WebOptions);

            roundTrip.ShouldNotBeNull();
            roundTrip.Outcome.ShouldBe(result.Outcome);
            roundTrip.Target.ShouldBe(ContractSamples.RedactionMessageTarget);
            json.ShouldNotContain("customer-request", Case.Insensitive);
            json.ShouldNotContain("redaction-policy-standard", Case.Insensitive);
            json.ShouldNotContain("EventStore", Case.Insensitive);
            json.ShouldNotContain("stream", Case.Insensitive);
            json.ShouldNotContain("provider", Case.Insensitive);
        }
    }

    private static ConversationRedactionResult Result(
        GovernanceOutcome outcome,
        RedactionCategory? category,
        string correlationId,
        GovernanceAuditEvidenceReference? auditEvidence = null,
        ConversationErrorCode? errorCode = null,
        GovernanceRemediation? remediation = null)
        => new(
            ContractSamples.Version,
            ContractSamples.Tenant,
            ContractSamples.Conversation,
            ContractSamples.RedactionMessageTarget,
            category,
            outcome,
            correlationId,
            auditEvidence,
            errorCode is null ? null : ContractSamples.SafeError(errorCode),
            remediation ?? GovernanceRemediation.None);
}
