// <copyright file="ReleaseWaiverValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json;

using Hexalith.Conversations.Contracts.Conformance;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Verifies <see cref="ReleaseWaiverValidator"/> against the committed fixture waiver,
/// each error token, content-safety, stable serialization, and lifecycle vocabulary coverage.
/// </summary>
public sealed class ReleaseWaiverValidationTest
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    private static readonly DateTimeOffset FixedTime = new(2026, 5, 23, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureExpiry = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset FutureReview = new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ExpiredTime = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FixtureWaiverShouldPassValidateWaiverWithZeroErrors()
    {
        ReleaseWaiverV1 waiver = LoadFixture();
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, FixedTime);
        errors.ShouldBeEmpty($"Fixture waiver validation errors: {string.Join(", ", errors)}");
    }

    [Fact]
    public void BlockerWithNullApproverShouldReturnBlockerRequiresApproverError()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(isBlocker: true, approver: null);
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, FixedTime);
        errors.ShouldContain("blocker-requires-approver");
    }

    [Fact]
    public void WaiverWithPastExpiryDateShouldReturnExpiredWaiverError()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(expiryDateUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, FixedTime);
        errors.ShouldContain("expired-waiver");
    }

    [Fact]
    public void WaiverWithPastReviewDateShouldReturnStaleReviewDateError()
    {
        ReleaseWaiverV1 waiver = BuildWaiver(reviewDateUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        IReadOnlyList<string> errors = ReleaseWaiverValidator.ValidateWaiver(waiver, FixedTime);
        errors.ShouldContain("stale-review-date");
    }

    [Fact]
    public void FixtureWaiverShouldPassContentSafetyScan()
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

        ReleaseWaiverV1 waiver = LoadFixture();
        string json = JsonSerializer.Serialize(waiver, WebOptions);

        foreach (string fragment in forbidden)
        {
            json.ShouldNotContain(fragment, Case.Insensitive, $"Fixture waiver JSON must not contain forbidden fragment '{fragment}'.");
        }
    }

    [Fact]
    public void WaiverShouldSerializeToStableCamelCaseJsonAndRoundTripDeterministically()
    {
        ReleaseWaiverV1 waiver = BuildWaiver();

        string first = JsonSerializer.Serialize(waiver, WebOptions);
        string second = JsonSerializer.Serialize(BuildWaiver(), WebOptions);

        first.ShouldBe(second);
        first.ShouldContain("\"waiverId\":");
        first.ShouldContain("\"lifecycleStatus\":");

        ReleaseWaiverV1? roundTripped = JsonSerializer.Deserialize<ReleaseWaiverV1>(first, WebOptions);
        roundTripped.ShouldNotBeNull();
        roundTripped!.WaiverId.ShouldBe(waiver.WaiverId);
        roundTripped.LifecycleStatus.ShouldBe(waiver.LifecycleStatus);
    }

    [Fact]
    public void WaiverLifecycleStatusAllShouldReturnExactlyFourValues()
    {
        WaiverLifecycleStatus.All.Count.ShouldBe(4);
        WaiverLifecycleStatus.All.ShouldContain(WaiverLifecycleStatus.Active);
        WaiverLifecycleStatus.All.ShouldContain(WaiverLifecycleStatus.Expired);
        WaiverLifecycleStatus.All.ShouldContain(WaiverLifecycleStatus.Rejected);
        WaiverLifecycleStatus.All.ShouldContain(WaiverLifecycleStatus.Superseded);
    }

    // --- Helpers ---

    private static ReleaseWaiverV1 LoadFixture()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(root, "docs", "release-evidence", "release-waiver-v1-fixture.json");

        File.Exists(path).ShouldBeTrue($"Expected waiver fixture file at '{path}'.");

        string json = File.ReadAllText(path);
        ReleaseWaiverV1? waiver = JsonSerializer.Deserialize<ReleaseWaiverV1>(json, WebOptions);
        waiver.ShouldNotBeNull();
        return waiver!;
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
        bool isBlocker = false,
        string? approver = "release-approver",
        DateTimeOffset? expiryDateUtc = null,
        DateTimeOffset? reviewDateUtc = null)
        => new(
            "waiver-validation-test-001",
            "release-engineer",
            approver,
            "FR85",
            null,
            ["5-4-support-named-waivers-for-release-gate-exceptions"],
            isBlocker,
            "Named waiver process documentation may need iteration before GA",
            "Waiver schema document provides navigable governance evidence for release approvers",
            expiryDateUtc ?? FutureExpiry,
            "Buyer can review named waivers through release evidence documents",
            null,
            ["release-waiver-v1-fixture"],
            reviewDateUtc ?? FutureReview,
            WaiverLifecycleStatus.Active,
            FixedTime);
}
