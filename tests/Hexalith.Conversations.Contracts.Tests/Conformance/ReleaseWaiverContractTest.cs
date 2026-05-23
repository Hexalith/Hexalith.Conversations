// <copyright file="ReleaseWaiverContractTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests.Conformance;

/// <summary>
/// Verifies the waiver lifecycle vocabulary, <see cref="ReleaseWaiverV1"/> construction,
/// <see cref="ReleaseWaiverValidator"/>, JSON shape, round-trip, additive tolerance, and fixture file validation.
/// </summary>
public sealed class ReleaseWaiverContractTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset CurrentTime = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureExpiry = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureReview = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

    // --- WaiverLifecycleStatus closed-vocabulary completeness ---

    [Fact]
    public void WaiverLifecycleStatusShouldCoverExactlyFourValues()
    {
        string[] expected = ["active", "expired", "rejected", "superseded"];
        WaiverLifecycleStatus.All.Count.ShouldBe(4);
        WaiverLifecycleStatus.All.Select(s => s.Value).ShouldBe(expected);
    }

    [Fact]
    public void WaiverLifecycleStatusShouldRejectSynonyms()
    {
        foreach (string synonym in new[] { "valid", "invalid", "cancelled", "done", "pending" })
        {
            Should.Throw<ArgumentException>(() => WaiverLifecycleStatus.Parse(synonym));
        }
    }

    [Fact]
    public void WaiverLifecycleStatusShouldRoundTripAllFourValues()
    {
        foreach (WaiverLifecycleStatus status in WaiverLifecycleStatus.All)
        {
            WaiverLifecycleStatus parsed = WaiverLifecycleStatus.Parse(status.Value);
            parsed.ShouldBe(status);
        }
    }

    [Fact]
    public void WaiverLifecycleStatusShouldSerializeAsClosedVocabularyToken()
    {
        string json = JsonSerializer.Serialize(WaiverLifecycleStatus.Active, WebOptions);
        json.ShouldBe("\"active\"");

        WaiverLifecycleStatus? parsed = JsonSerializer.Deserialize<WaiverLifecycleStatus>("\"active\"", WebOptions);
        parsed.ShouldBe(WaiverLifecycleStatus.Active);
    }

    [Fact]
    public void WaiverLifecycleStatusShouldRejectUnknownJsonTokens()
    {
        foreach (string synonym in new[] { "valid", "invalid", "cancelled", "done" })
        {
            Should.Throw<Exception>(() => JsonSerializer.Deserialize<WaiverLifecycleStatus>($"\"{synonym}\"", WebOptions));
        }
    }

    [Fact]
    public void WaiverShouldRejectNullAffectedStoryIds()
        => Should.Throw<ArgumentException>(() => new ReleaseWaiverV1(
            "waiver-test-001",
            "release-engineer",
            "release-approver",
            "FR85",
            null,
            null!,
            false,
            "Risk description",
            "Compensating control description",
            FutureExpiry,
            "Buyer impact description",
            null,
            [],
            FutureReview,
            WaiverLifecycleStatus.Active,
            CurrentTime));

    // --- WaiverLifecycleStatus.IsActive ---

    [Fact]
    public void IsActiveShouldBeTrueOnlyForActive()
    {
        WaiverLifecycleStatus.Active.IsActive.ShouldBeTrue();
        WaiverLifecycleStatus.Expired.IsActive.ShouldBeFalse();
        WaiverLifecycleStatus.Rejected.IsActive.ShouldBeFalse();
        WaiverLifecycleStatus.Superseded.IsActive.ShouldBeFalse();
    }

    // --- WaiverLifecycleStatus.IsStale ---

    [Fact]
    public void IsStaleShouldBeTrueForExpiredAndSuperseded()
    {
        WaiverLifecycleStatus.Expired.IsStale.ShouldBeTrue();
        WaiverLifecycleStatus.Superseded.IsStale.ShouldBeTrue();
        WaiverLifecycleStatus.Active.IsStale.ShouldBeFalse();
        WaiverLifecycleStatus.Rejected.IsStale.ShouldBeFalse();
    }

    // --- ReleaseWaiverV1 construction-time validation ---

    [Fact]
    public void WaiverShouldRejectNullWaiverId()
        => Should.Throw<ArgumentException>(() => BuildWaiver(waiverId: null!));

    [Fact]
    public void WaiverShouldRejectEmptyWaiverId()
        => Should.Throw<ArgumentException>(() => BuildWaiver(waiverId: string.Empty));

    [Fact]
    public void WaiverShouldRejectNullOwner()
        => Should.Throw<ArgumentException>(() => BuildWaiver(owner: null!));

    [Fact]
    public void WaiverShouldRejectEmptyOwner()
        => Should.Throw<ArgumentException>(() => BuildWaiver(owner: string.Empty));

    [Fact]
    public void WaiverShouldAcceptNullApprover()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(approver: null);
        waiver.Approver.ShouldBeNull();
    }

    [Fact]
    public void WaiverShouldRejectNullAffectedRequirementId()
        => Should.Throw<ArgumentException>(() => BuildWaiver(affectedRequirementId: null!));

    [Fact]
    public void WaiverShouldRejectEmptyAffectedRequirementId()
        => Should.Throw<ArgumentException>(() => BuildWaiver(affectedRequirementId: string.Empty));

    [Fact]
    public void WaiverShouldAcceptNullAffectedGateId()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(affectedGateId: null);
        waiver.AffectedGateId.ShouldBeNull();
    }

    [Fact]
    public void WaiverShouldRejectEmptyAffectedStoryIds()
        => Should.Throw<ArgumentException>(() => BuildWaiver(affectedStoryIds: []));

    [Fact]
    public void WaiverShouldRejectNullElementInAffectedStoryIds()
        => Should.Throw<ArgumentException>(() => BuildWaiver(affectedStoryIds: [null!]));

    [Fact]
    public void WaiverShouldRejectEmptyRisk()
        => Should.Throw<ArgumentException>(() => BuildWaiver(risk: string.Empty));

    [Fact]
    public void WaiverShouldRejectEmptyCompensatingControl()
        => Should.Throw<ArgumentException>(() => BuildWaiver(compensatingControl: string.Empty));

    [Fact]
    public void WaiverShouldRejectNonUtcExpiryDateUtc()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildWaiver(expiryDateUtc: new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.FromHours(1))));

    [Fact]
    public void WaiverShouldRejectEmptyBuyerImpact()
        => Should.Throw<ArgumentException>(() => BuildWaiver(buyerImpact: string.Empty));

    [Fact]
    public void WaiverShouldAcceptNullBuyerAcceptanceStatus()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(buyerAcceptanceStatus: null);
        waiver.BuyerAcceptanceStatus.ShouldBeNull();
    }

    [Fact]
    public void WaiverShouldRejectNullEvidenceLinks()
        => Should.Throw<ArgumentNullException>(() => new ReleaseWaiverV1(
            "waiver-test-001",
            "release-engineer",
            "release-approver",
            "FR85",
            null,
            ["5-4-support-named-waivers-for-release-gate-exceptions"],
            false,
            "Risk description",
            "Compensating control description",
            FutureExpiry,
            "Buyer impact description",
            null,
            null!,
            FutureReview,
            WaiverLifecycleStatus.Active,
            CurrentTime));

    [Fact]
    public void WaiverShouldAcceptEmptyEvidenceLinks()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(evidenceLinks: []);
        waiver.EvidenceLinks.ShouldBeEmpty();
    }

    [Fact]
    public void WaiverShouldRejectNonUtcReviewDateUtc()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildWaiver(reviewDateUtc: new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.FromHours(2))));

    [Fact]
    public void WaiverShouldRejectNullLifecycleStatus()
        => Should.Throw<ArgumentNullException>(() => new ReleaseWaiverV1(
            "waiver-test-001",
            "release-engineer",
            "release-approver",
            "FR85",
            null,
            ["5-4-support-named-waivers-for-release-gate-exceptions"],
            false,
            "Risk description",
            "Compensating control description",
            FutureExpiry,
            "Buyer impact description",
            null,
            [],
            FutureReview,
            null!,
            CurrentTime));

    [Fact]
    public void WaiverShouldRejectNonUtcCreatedAtUtc()
        => Should.Throw<ArgumentOutOfRangeException>(
            () => BuildWaiver(createdAtUtc: new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.FromHours(1))));

    // --- ReleaseWaiverValidator.ValidateWaiver ---

    [Fact]
    public void ValidateWaiverShouldReturnBlockerRequiresApproverWhenBlockerHasNullApprover()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(isBlocker: true, approver: null);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldContain("blocker-requires-approver");
    }

    [Fact]
    public void ValidateWaiverShouldNotReturnBlockerRequiresApproverWhenNonBlockerHasNullApprover()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(isBlocker: false, approver: null);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldNotContain("blocker-requires-approver");
    }

    [Fact]
    public void ValidateWaiverShouldReturnExpiredWaiverWhenExpiryIsInThePast()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(expiryDateUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldContain("expired-waiver");
    }

    [Fact]
    public void ValidateWaiverShouldReturnStaleReviewDateWhenReviewDateIsInThePast()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(reviewDateUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldContain("stale-review-date");
    }

    [Fact]
    public void ValidateWaiverShouldReturnBuyerFacingMissingAcceptanceWhenBlockerGateHasNoAcceptance()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(
            isBlocker: true,
            approver: "release-approver",
            affectedGateId: ReleaseGateId.TenantIsolation,
            buyerAcceptanceStatus: null);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldContain("buyer-facing-missing-acceptance");
    }

    [Fact]
    public void ValidateWaiverShouldNotReturnBuyerFacingMissingAcceptanceWhenBlockerGateHasAcceptance()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(
            isBlocker: true,
            approver: "release-approver",
            affectedGateId: ReleaseGateId.TenantIsolation,
            buyerAcceptanceStatus: "buyer-accepted");
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldNotContain("buyer-facing-missing-acceptance");
    }

    [Fact]
    public void ValidateWaiverShouldNotReturnBuyerFacingMissingAcceptanceWhenNonBlockerHasNullAcceptance()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(isBlocker: false, buyerAcceptanceStatus: null);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldNotContain("buyer-facing-missing-acceptance");
    }

    [Fact]
    public void ValidateWaiverShouldReturnEmptyListForValidActiveWaiverWithFutureDates()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(
            approver: "release-approver",
            expiryDateUtc: FutureExpiry,
            reviewDateUtc: FutureReview);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldBeEmpty();
    }

    // --- JSON shape, round-trip, additive tolerance ---

    [Fact]
    public void WaiverShouldSerializeToStableCamelCaseWebJson()
    {
        ReleaseWaiverV1 waiver = BuildWaiver();
        string json = JsonSerializer.Serialize(waiver, WebOptions);

        json.ShouldContain("\"waiverId\":");
        json.ShouldContain("\"owner\":");
        json.ShouldContain("\"affectedRequirementId\":");
        json.ShouldContain("\"lifecycleStatus\":");
        json.ShouldContain("\"expiryDateUtc\":");
        json.ShouldNotContain("\"WaiverId\"", Case.Sensitive);
    }

    [Fact]
    public void WaiverShouldRoundTripLosslessly()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(approver: "release-approver");
        string json = JsonSerializer.Serialize(waiver, WebOptions);
        ReleaseWaiverV1? parsed = JsonSerializer.Deserialize<ReleaseWaiverV1>(json, WebOptions);

        parsed.ShouldNotBeNull();
        parsed!.WaiverId.ShouldBe(waiver.WaiverId);
        parsed.Owner.ShouldBe(waiver.Owner);
        parsed.Approver.ShouldBe(waiver.Approver);
        parsed.AffectedRequirementId.ShouldBe(waiver.AffectedRequirementId);
        parsed.LifecycleStatus.ShouldBe(waiver.LifecycleStatus);
        parsed.ExpiryDateUtc.ShouldBe(waiver.ExpiryDateUtc);
        parsed.ReviewDateUtc.ShouldBe(waiver.ReviewDateUtc);
        parsed.CreatedAtUtc.ShouldBe(waiver.CreatedAtUtc);
        parsed.AffectedStoryIds.Count.ShouldBe(waiver.AffectedStoryIds.Count);
    }

    [Fact]
    public void WaiverShouldTolerateAdditiveJson()
    {
        ReleaseWaiverV1 waiver = BuildWaiver();
        string json = JsonSerializer.Serialize(waiver, WebOptions);
        JsonNode node = JsonNode.Parse(json)!;
        node["futureField"] = "ignored";

        ReleaseWaiverV1? parsed = JsonSerializer.Deserialize<ReleaseWaiverV1>(node.ToJsonString(), WebOptions);
        parsed.ShouldNotBeNull();
        parsed!.WaiverId.ShouldBe(waiver.WaiverId);
    }

    // --- Fixture file validation ---

    [Fact]
    public void FixtureFileShouldExistAndDeserializeWithoutError()
    {
        string path = GetFixturePath();
        File.Exists(path).ShouldBeTrue($"Expected waiver fixture file at '{path}'.");

        string json = File.ReadAllText(path);
        ReleaseWaiverV1? waiver = JsonSerializer.Deserialize<ReleaseWaiverV1>(json, WebOptions);
        waiver.ShouldNotBeNull();
    }

    [Fact]
    public void FixtureFileShouldPassValidateWaiverWithFutureEvaluatedAt()
    {
        string json = File.ReadAllText(GetFixturePath());
        ReleaseWaiverV1 waiver = JsonSerializer.Deserialize<ReleaseWaiverV1>(json, WebOptions)!;

        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, CurrentTime);
        errors.ShouldBeEmpty($"Fixture waiver validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void FixtureFileShouldPassContentSafetyScan()
    {
        string[] forbidden =
        [
            "EventStore",
            "snapshot",
            "provider payload",
            "raw exception",
            "C:\\",
            "D:\\",
        ];

        string json = File.ReadAllText(GetFixturePath());
        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Waiver fixture must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void ManifestFixtureShouldHaveFourEntriesAfterStory54Update()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "conformance-manifest-v1-fixture.json");
        File.Exists(path).ShouldBeTrue($"Expected manifest fixture at '{path}'.");

        string json = File.ReadAllText(path);
        JsonNode? node = JsonNode.Parse(json);
        JsonNode? entries = node?["entries"];
        entries.ShouldNotBeNull();
        entries!.AsArray().Count.ShouldBe(4);
    }

    // --- Helpers ---

    private static string GetFixturePath()
    {
        string root = FindRepositoryRoot();
        return Path.Combine(root, "docs", "release-evidence", "release-waiver-v1-fixture.json");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static ReleaseWaiverV1 BuildWaiver(
        string waiverId = "waiver-test-001",
        string owner = "release-engineer",
        string? approver = "release-approver",
        string affectedRequirementId = "FR85",
        ReleaseGateId? affectedGateId = null,
        IReadOnlyList<string>? affectedStoryIds = null,
        bool isBlocker = false,
        string risk = "Named waiver process may need iteration before GA",
        string compensatingControl = "Schema document provides navigable evidence for release approvers",
        DateTimeOffset? expiryDateUtc = null,
        string buyerImpact = "Buyer can review named waivers through release evidence documents",
        string? buyerAcceptanceStatus = null,
        IReadOnlyList<string>? evidenceLinks = null,
        DateTimeOffset? reviewDateUtc = null,
        WaiverLifecycleStatus? lifecycleStatus = null,
        DateTimeOffset? createdAtUtc = null)
        => new(
            waiverId,
            owner,
            approver,
            affectedRequirementId,
            affectedGateId,
            affectedStoryIds ?? (IReadOnlyList<string>)["5-4-support-named-waivers-for-release-gate-exceptions"],
            isBlocker,
            risk,
            compensatingControl,
            expiryDateUtc ?? FutureExpiry,
            buyerImpact,
            buyerAcceptanceStatus,
            evidenceLinks ?? (IReadOnlyList<string>)["release-waiver-v1-fixture"],
            reviewDateUtc ?? FutureReview,
            lifecycleStatus ?? WaiverLifecycleStatus.Active,
            createdAtUtc ?? CurrentTime);
}
