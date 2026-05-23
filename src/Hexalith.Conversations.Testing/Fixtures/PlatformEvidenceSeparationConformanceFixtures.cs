// <copyright file="PlatformEvidenceSeparationConformanceFixtures.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;
using Hexalith.Conversations.Contracts.Errors;

namespace Hexalith.Conversations.Testing.Fixtures;

/// <summary>
/// Carries the deterministic synthetic scenario data for one platform evidence separation conformance check.
/// </summary>
/// <param name="ScenarioToken">The bounded safe machine-readable scenario identifier (no real tenant IDs, Party IDs, or conversation IDs).</param>
/// <param name="ExpectedOutcome">The expected conformance outcome for this scenario.</param>
/// <param name="ExpectedClassification">The expected failure classification (conformant for all 10 scenarios).</param>
/// <param name="SafeMessage">The bounded content-safe scenario description (≤512 chars, no protected identifiers).</param>
/// <param name="ExpectedErrorCode">The expected typed error code for non-ready outcomes; null for ready outcomes.</param>
public sealed record PlatformEvidenceSeparationScenarioData(
    string ScenarioToken,
    ConformanceOutcome ExpectedOutcome,
    ConformanceFailureClassification ExpectedClassification,
    string SafeMessage,
    ConversationErrorCode? ExpectedErrorCode = null);

/// <summary>
/// Provides the deterministic, synthetic, content-safe platform evidence separation fixture exercised by the
/// platform evidence separation conformance suite.
/// </summary>
/// <remarks>
/// All data is synthetic and clearly marked. Loading this fixture appends no events, mutates no
/// aggregate state, writes no projection store, and requires no nested submodule initialization.
/// The 10 scenarios cover all AC1-AC3-required platform evidence separation surfaces. Classification is
/// conformant for all scenarios because the suite proves the system CORRECTLY handles each one.
/// Outcome counts: 8 ready, 1 blocked, 1 unknown.
/// </remarks>
public static class PlatformEvidenceSeparationConformanceSeedData
{
    /// <summary>
    /// Marks every fixture scenario as synthetic conformance data.
    /// </summary>
    public const string SyntheticDataMarker = "synthetic-conformance-data";

    /// <summary>
    /// Gets the 10 deterministic synthetic platform evidence separation scenario records.
    /// </summary>
    public static IReadOnlyList<PlatformEvidenceSeparationScenarioData> Scenarios =>
    [
        new(
            "conversations-controls-documented",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Conversations-owned controls for aggregate invariants, fail-closed access, audit pairing, governance replay, idempotency, schema evolution, contract compatibility, and projection rebuild are documented with evidence links that identify the module boundary without referencing platform-owned behaviors.",
            null),

        new(
            "eventlog-controls-inherited",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Event log-inherited controls for event persistence, replay ordering, and history durability are named with source component, version reference, and scope limitation confirming that history authority belongs to the infrastructure layer rather than to Conversations.",
            null),

        new(
            "access-management-inherited",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Tenants service-inherited controls for tenant provisioning and authentication context binding are named with source component and scope limitation; Conversations uses the Tenants projection as a read-only authority and does not implement authentication independently.",
            null),

        new(
            "parties-registry-inherited",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Parties service-inherited controls for participant personal data handling and Party identity lifecycle are named with source component and scope limitation; Conversations records only stable Party identifiers without recording personal data in the event history.",
            null),

        new(
            "ui-framework-inherited",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "FrontComposer-inherited controls for UI generation, accessibility baseline, and generated surface boundaries are named with source component and scope limitation; Conversations adds only custom trust-critical components beyond the generated baseline.",
            null),

        new(
            "infra-runtime-inherited",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Dapr and Aspire-inherited controls for pub/sub reliability, sidecar health, and local orchestration are named with source component and scope limitation; Conversations does not own infrastructure runtime behavior or deployment topology.",
            null),

        new(
            "missing-inherited-evidence-hidden",
            ConformanceOutcome.Unknown,
            ConformanceFailureClassification.Conformant,
            "An inherited control with no available evidence reference is marked as unknown-accepted in the release evidence rather than silently omitted; the absence of a reference is disclosed to release approvers rather than treated as a module-proven pass.",
            ConversationErrorCode.AggregateNotFound),

        new(
            "incompatible-inherited-evidence-blocked",
            ConformanceOutcome.Blocked,
            ConformanceFailureClassification.Conformant,
            "An inherited control whose evidence uses an incompatible version or falls outside the stated scope boundary is blocked from acceptance rather than silently included; incompatibility is surfaced to release approvers with typed diagnostic information.",
            ConversationErrorCode.SchemaVersionUnsupported),

        new(
            "approver-view-summarizes-controls",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "The non-developer approver evidence view summarizes pass/fail status, blocker reason, scope, timestamp, signer, waiver status, and linked machine-readable verification output for each control boundary entry.",
            null),

        new(
            "approver-view-content-safe",
            ConformanceOutcome.Ready,
            ConformanceFailureClassification.Conformant,
            "Evidence views rendered for non-developer approvers use only permission-safe approved content without raw logs, unsafe payloads, protected identifiers, or internal infrastructure terminology.",
            null),
    ];
}
