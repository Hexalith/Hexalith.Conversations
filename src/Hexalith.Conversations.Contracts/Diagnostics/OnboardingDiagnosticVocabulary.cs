// <copyright file="OnboardingDiagnosticVocabulary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Diagnostics.OnboardingDiagnosticVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Diagnostics;

/// <summary>
/// Defines the closed set of v1 CORE onboarding diagnostic checks.
/// </summary>
[JsonConverter(typeof(OnboardingDiagnosticCheckJsonConverter))]
public sealed record OnboardingDiagnosticCheck
{
    /// <summary>
    /// Gets the trusted tenant-context check.
    /// </summary>
    public static OnboardingDiagnosticCheck TenantContext { get; } = new("tenant-context");

    /// <summary>
    /// Gets the supported contract-version check.
    /// </summary>
    public static OnboardingDiagnosticCheck ContractVersion { get; } = new("contract-version");

    /// <summary>
    /// Gets the provider-configuration check.
    /// </summary>
    public static OnboardingDiagnosticCheck ProviderConfiguration { get; } = new("provider-configuration");

    /// <summary>
    /// Gets the projection-subscription health check.
    /// </summary>
    public static OnboardingDiagnosticCheck ProjectionSubscription { get; } = new("projection-subscription");

    /// <summary>
    /// Gets the schema-compatibility check.
    /// </summary>
    public static OnboardingDiagnosticCheck SchemaCompatibility { get; } = new("schema-compatibility");

    /// <summary>
    /// Gets the audit-availability check.
    /// </summary>
    public static OnboardingDiagnosticCheck AuditAvailability { get; } = new("audit-availability");

    /// <summary>
    /// Gets the Parties-integration check.
    /// </summary>
    public static OnboardingDiagnosticCheck PartiesIntegration { get; } = new("parties-integration");

    private static readonly IReadOnlyDictionary<string, OnboardingDiagnosticCheck> KnownValues = Known(
        TenantContext,
        ContractVersion,
        ProviderConfiguration,
        ProjectionSubscription,
        SchemaCompatibility,
        AuditAvailability,
        PartiesIntegration);

    private OnboardingDiagnosticCheck(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported diagnostic check in canonical order.
    /// </summary>
    public static IReadOnlyList<OnboardingDiagnosticCheck> All { get; } =
    [
        TenantContext,
        ContractVersion,
        ProviderConfiguration,
        ProjectionSubscription,
        SchemaCompatibility,
        AuditAvailability,
        PartiesIntegration,
    ];

    /// <summary>
    /// Resolves a supported diagnostic check.
    /// </summary>
    /// <param name="value">The canonical check value.</param>
    /// <returns>The matching diagnostic check.</returns>
    public static OnboardingDiagnosticCheck Parse(string value)
        => ParseKnown(value, KnownValues, nameof(OnboardingDiagnosticCheck));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed CORE onboarding diagnostic status vocabulary mapped to the shared trust/freshness language.
/// </summary>
/// <remarks>
/// Each value maps to the shared trust/freshness gate, never inventing diagnostics-only synonyms:
/// <list type="bullet">
/// <item><description><c>ready</c> means the precondition is met using only the trust-bearing <c>Current</c> state.</description></item>
/// <item><description><c>degraded</c> means an authorized but non-trust-bearing state (<c>Stale</c>/<c>Rebuilding</c>) with safe retry remediation.</description></item>
/// <item><description><c>blocked</c> means the precondition is not met (<c>Unavailable</c>, unsupported, or fail-closed) and dependent operations must not weaken isolation.</description></item>
/// <item><description><c>unknown</c> means the readiness state could not be proven without disclosing protected detail (hidden/forbidden equivalent).</description></item>
/// </list>
/// </remarks>
[JsonConverter(typeof(OnboardingDiagnosticStatusJsonConverter))]
public sealed record OnboardingDiagnosticStatus
{
    /// <summary>
    /// Gets the ready status (trust-bearing <c>Current</c> state only).
    /// </summary>
    public static OnboardingDiagnosticStatus Ready { get; } = new("ready");

    /// <summary>
    /// Gets the degraded status (authorized non-trust-bearing state with safe remediation).
    /// </summary>
    public static OnboardingDiagnosticStatus Degraded { get; } = new("degraded");

    /// <summary>
    /// Gets the blocked status (precondition not met, never silently weakened).
    /// </summary>
    public static OnboardingDiagnosticStatus Blocked { get; } = new("blocked");

    /// <summary>
    /// Gets the unknown status (hidden or unprovable without disclosing protected detail).
    /// </summary>
    public static OnboardingDiagnosticStatus Unknown { get; } = new("unknown");

    private static readonly IReadOnlyDictionary<string, OnboardingDiagnosticStatus> KnownValues = Known(
        Ready,
        Degraded,
        Blocked,
        Unknown);

    private OnboardingDiagnosticStatus(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported diagnostic status.
    /// </summary>
    public static IReadOnlyList<OnboardingDiagnosticStatus> All { get; } =
    [
        Ready,
        Degraded,
        Blocked,
        Unknown,
    ];

    /// <summary>
    /// Resolves a supported diagnostic status.
    /// </summary>
    /// <param name="value">The canonical status value.</param>
    /// <returns>The matching diagnostic status.</returns>
    public static OnboardingDiagnosticStatus Parse(string value)
        => ParseKnown(value, KnownValues, nameof(OnboardingDiagnosticStatus));

    /// <inheritdoc />
    public override string ToString() => Value;
}

internal static class OnboardingDiagnosticVocabularyValidation
{
    internal static IReadOnlyDictionary<string, T> Known<T>(params T[] values)
        where T : notnull
        => values.ToDictionary(value => value.ToString() ?? string.Empty, StringComparer.Ordinal);

    internal static T ParseKnown<T>(string value, IReadOnlyDictionary<string, T> knownValues, string vocabularyName)
    {
        string safe = ValidateVocabularyValue(value, nameof(value));
        return knownValues.TryGetValue(safe, out T? known)
            ? known
            : throw new ArgumentException($"Unsupported {vocabularyName} value.", nameof(value));
    }

    internal static string ValidateVocabularyValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length > 64 || value.Any(static c => !IsVocabularyCharacter(c)))
        {
            throw new ArgumentException("Value must be a bounded closed vocabulary token.", parameterName);
        }

        return value;
    }

    private static bool IsVocabularyCharacter(char value)
        => (value >= 'a' && value <= 'z') || char.IsAsciiDigit(value) || value is '-';
}
