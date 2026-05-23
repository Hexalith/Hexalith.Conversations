// <copyright file="OnboardingDiagnosticRunResultV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Versioning;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Carries the content-safe machine-readable result of a CORE onboarding diagnostics run.
/// </summary>
/// <param name="SchemaVersion">The result schema version.</param>
/// <param name="OverallStatus">The aggregate closed-vocabulary status across all checks.</param>
/// <param name="SafeSummary">The bounded content-safe summary message.</param>
/// <param name="CorrelationId">The safe correlation identifier.</param>
/// <param name="GeneratedAtUtc">The UTC timestamp when the run was generated.</param>
/// <param name="Checks">The per-check results.</param>
public sealed record OnboardingDiagnosticRunResultV1(
    SchemaVersion SchemaVersion,
    OnboardingDiagnosticStatus OverallStatus,
    string SafeSummary,
    string CorrelationId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<OnboardingDiagnosticCheckResultV1> Checks)
{
    /// <summary>
    /// Gets the result schema version.
    /// </summary>
    public SchemaVersion SchemaVersion { get; } = SchemaVersion ?? throw new ArgumentNullException(nameof(SchemaVersion));

    /// <summary>
    /// Gets the aggregate closed-vocabulary status.
    /// </summary>
    public OnboardingDiagnosticStatus OverallStatus { get; } = OverallStatus ?? throw new ArgumentNullException(nameof(OverallStatus));

    /// <summary>
    /// Gets the bounded content-safe summary message.
    /// </summary>
    public string SafeSummary { get; } = DiagnosticContractValidation.RequiredSafeText(SafeSummary, nameof(SafeSummary));

    /// <summary>
    /// Gets the safe correlation identifier.
    /// </summary>
    public string CorrelationId { get; } = DiagnosticContractValidation.RequiredSafeToken(CorrelationId, nameof(CorrelationId));

    /// <summary>
    /// Gets the UTC timestamp when the run was generated.
    /// </summary>
    public DateTimeOffset GeneratedAtUtc { get; } = ValidateGeneratedAt(GeneratedAtUtc);

    /// <summary>
    /// Gets the per-check results.
    /// </summary>
    public IReadOnlyList<OnboardingDiagnosticCheckResultV1> Checks { get; } = ValidateChecks(Checks);

    private static DateTimeOffset ValidateGeneratedAt(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(GeneratedAtUtc), "Timestamp must use UTC offset zero.");
        }

        if (value.Year < 2000 || value.Year > 9998)
        {
            throw new ArgumentOutOfRangeException(nameof(GeneratedAtUtc), "Timestamp must be within the plausible business range.");
        }

        return value;
    }

    private static IReadOnlyList<OnboardingDiagnosticCheckResultV1> ValidateChecks(
        IReadOnlyList<OnboardingDiagnosticCheckResultV1>? values)
        => values is null || values.Count == 0 || values.Any(value => value is null)
            ? throw new ArgumentException("At least one diagnostic check result is required.", nameof(values))
            : values.ToArray();
}
