// <copyright file="CapabilityReleaseScopeValidatorTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies that <see cref="CapabilityReleaseScopeValidator"/> returns the correct error tokens for all
/// scope validation paths (FR100, FR101, AC3).
/// </summary>
public sealed class CapabilityReleaseScopeValidatorTest
{
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureExpiry = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastExpiry = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReviewDate = new(2027, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static CapabilityReleaseScopeEntryV1 BuildEntry(
        CapabilityReleaseScope scope,
        IReadOnlyList<SubstrateConsequenceArea>? consequenceAreas = null,
        string? waiverRef = null,
        DateTimeOffset? conditionalExpiry = null)
        => new(
            "create-conversation",
            scope,
            consequenceAreas ?? [],
            null,
            null,
            null,
            "release-engineer",
            ReviewDate,
            waiverRef,
            conditionalExpiry);

    [Fact]
    public void ValidateEntry_V1Scope_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.V1);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_V1Point1Scope_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.V1Point1);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_DeferredWithConsequences_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(
            CapabilityReleaseScope.Deferred,
            [SubstrateConsequenceArea.TenantIsolation, SubstrateConsequenceArea.AuditPairing]);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_DeferredNoConsequences_ReturnsDeferredSubstrateNoConsequences()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Deferred);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("deferred-substrate-no-consequences");
        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ValidateEntry_WaivedWithReference_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Waived, waiverRef: "approved-scope-waiver");
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_WaivedNoReference_ReturnsWaivedNoReference()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Waived);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("waived-no-reference");
        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ValidateEntry_ConditionalWithFutureExpiry_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Conditional, conditionalExpiry: FutureExpiry);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void ValidateEntry_ConditionalWithPastExpiry_ReturnsExpiredConditionalScope()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Conditional, conditionalExpiry: PastExpiry);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("expired-conditional-scope");
        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ValidateEntry_ConditionalNullExpiry_ReturnsExpiredConditionalScope()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.Conditional);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldContain("expired-conditional-scope");
        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void ValidateEntry_OutOfScope_ReturnsNoErrors()
    {
        CapabilityReleaseScopeEntryV1 entry = BuildEntry(CapabilityReleaseScope.OutOfScope);
        IReadOnlyList<string> errors = CapabilityReleaseScopeValidator.ValidateEntry(entry, EvaluatedAt);
        errors.ShouldBeEmpty();
    }
}
