// <copyright file="ConformanceVocabulary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Defines the closed adopter-facing conformance check vocabulary covering the CORE integration surface.
/// </summary>
/// <remarks>
/// Each value is a stable machine identifier that maps one conformance check to a CORE behavior
/// (create conversation, append message, read timeline, tenant binding, Party identity, idempotency,
/// error envelope, projection freshness, event publication, governance preconditions, and compatibility
/// discovery). These are bounded closed-vocabulary tokens, not free text, so they are safe machine
/// identifiers for CI consumption.
/// </remarks>
[JsonConverter(typeof(ConformanceCheckJsonConverter))]
public sealed record ConformanceCheck
{
    /// <summary>
    /// Gets the create-conversation conformance check.
    /// </summary>
    public static ConformanceCheck CreateConversation { get; } = new("create-conversation");

    /// <summary>
    /// Gets the append-message conformance check.
    /// </summary>
    public static ConformanceCheck AppendMessage { get; } = new("append-message");

    /// <summary>
    /// Gets the read-timeline conformance check.
    /// </summary>
    public static ConformanceCheck ReadTimeline { get; } = new("read-timeline");

    /// <summary>
    /// Gets the tenant-binding conformance check.
    /// </summary>
    public static ConformanceCheck TenantBinding { get; } = new("tenant-binding");

    /// <summary>
    /// Gets the Party-identity conformance check.
    /// </summary>
    public static ConformanceCheck PartyIdentity { get; } = new("party-identity");

    /// <summary>
    /// Gets the idempotency conformance check.
    /// </summary>
    public static ConformanceCheck Idempotency { get; } = new("idempotency");

    /// <summary>
    /// Gets the error-envelope conformance check.
    /// </summary>
    public static ConformanceCheck ErrorEnvelope { get; } = new("error-envelope");

    /// <summary>
    /// Gets the projection-freshness conformance check.
    /// </summary>
    public static ConformanceCheck ProjectionFreshness { get; } = new("projection-freshness");

    /// <summary>
    /// Gets the event-publication conformance check.
    /// </summary>
    public static ConformanceCheck EventPublication { get; } = new("event-publication");

    /// <summary>
    /// Gets the governance-precondition conformance check.
    /// </summary>
    public static ConformanceCheck GovernancePrecondition { get; } = new("governance-precondition");

    /// <summary>
    /// Gets the compatibility-discovery conformance check.
    /// </summary>
    public static ConformanceCheck CompatibilityDiscovery { get; } = new("compatibility-discovery");

    private static readonly IReadOnlyDictionary<string, ConformanceCheck> KnownValues = Known(
        CreateConversation,
        AppendMessage,
        ReadTimeline,
        TenantBinding,
        PartyIdentity,
        Idempotency,
        ErrorEnvelope,
        ProjectionFreshness,
        EventPublication,
        GovernancePrecondition,
        CompatibilityDiscovery);

    private ConformanceCheck(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported conformance check in canonical order.
    /// </summary>
    public static IReadOnlyList<ConformanceCheck> All { get; } =
    [
        CreateConversation,
        AppendMessage,
        ReadTimeline,
        TenantBinding,
        PartyIdentity,
        Idempotency,
        ErrorEnvelope,
        ProjectionFreshness,
        EventPublication,
        GovernancePrecondition,
        CompatibilityDiscovery,
    ];

    /// <summary>
    /// Resolves a supported conformance check.
    /// </summary>
    /// <param name="value">The canonical check value.</param>
    /// <returns>The matching conformance check.</returns>
    public static ConformanceCheck Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConformanceCheck));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed conformance outcome vocabulary aligned to the shared trust/freshness and
/// Story 4.4 readiness language.
/// </summary>
/// <remarks>
/// The vocabulary deliberately reuses the Story 4.4 onboarding readiness language rather than inventing
/// conformance-only synonyms (no <c>ok</c>, <c>healthy</c>, <c>pass-ish</c>, or <c>maybe</c>):
/// <list type="bullet">
/// <item><description><c>ready</c> means the check observed trust-bearing <c>Current</c> behavior and the CORE
/// invariant holds.</description></item>
/// <item><description><c>degraded</c> means an authorized but non-trust-bearing state (<c>Stale</c>/<c>Rebuilding</c>)
/// surfaced exactly as the contract requires, with safe retry remediation.</description></item>
/// <item><description><c>blocked</c> means the check observed a fail-closed rejection (<c>Unavailable</c>,
/// unsupported, conflict, or denial) surfaced exactly as the contract requires.</description></item>
/// <item><description><c>unknown</c> means a hidden/forbidden side-channel-equivalent shape was observed and the
/// check could not prove existence without disclosing protected detail.</description></item>
/// </list>
/// A check is conformant when its observed outcome equals the contract-required outcome for the exercised
/// scenario; an unexpected outcome is reported through <see cref="ConformanceFailureClassification"/>.
/// </remarks>
[JsonConverter(typeof(ConformanceOutcomeJsonConverter))]
public sealed record ConformanceOutcome
{
    /// <summary>
    /// Gets the ready outcome (trust-bearing <c>Current</c> behavior; the CORE invariant holds).
    /// </summary>
    public static ConformanceOutcome Ready { get; } = new("ready");

    /// <summary>
    /// Gets the degraded outcome (authorized non-trust-bearing state surfaced as required).
    /// </summary>
    public static ConformanceOutcome Degraded { get; } = new("degraded");

    /// <summary>
    /// Gets the blocked outcome (fail-closed rejection surfaced as required).
    /// </summary>
    public static ConformanceOutcome Blocked { get; } = new("blocked");

    /// <summary>
    /// Gets the unknown outcome (hidden/forbidden side-channel-equivalent shape).
    /// </summary>
    public static ConformanceOutcome Unknown { get; } = new("unknown");

    private static readonly IReadOnlyDictionary<string, ConformanceOutcome> KnownValues = Known(
        Ready,
        Degraded,
        Blocked,
        Unknown);

    private ConformanceOutcome(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported conformance outcome.
    /// </summary>
    public static IReadOnlyList<ConformanceOutcome> All { get; } =
    [
        Ready,
        Degraded,
        Blocked,
        Unknown,
    ];

    /// <summary>
    /// Resolves a supported conformance outcome.
    /// </summary>
    /// <param name="value">The canonical outcome value.</param>
    /// <returns>The matching conformance outcome.</returns>
    public static ConformanceOutcome Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConformanceOutcome));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed conformance failure-classification vocabulary distinguishing product-invariant failures
/// from infrastructure, configuration, unavailable-dependency, and execution failures (AC3).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>conformant</c> means the observed outcome matched the contract-required outcome; no failure.</description></item>
/// <item><description><c>product-invariant</c> means a Conversations contract invariant was violated (a true product defect).</description></item>
/// <item><description><c>infrastructure</c> means an environment or platform fault, not a product defect.</description></item>
/// <item><description><c>configuration</c> means missing or incorrect adopter/host configuration.</description></item>
/// <item><description><c>unavailable-dependency</c> means a required dependency was unavailable (retry-class).</description></item>
/// <item><description><c>execution</c> means the conformance harness itself failed to execute the check.</description></item>
/// </list>
/// </remarks>
[JsonConverter(typeof(ConformanceFailureClassificationJsonConverter))]
public sealed record ConformanceFailureClassification
{
    /// <summary>
    /// Gets the conformant classification (no failure; observed outcome matched the contract).
    /// </summary>
    public static ConformanceFailureClassification Conformant { get; } = new("conformant");

    /// <summary>
    /// Gets the product-invariant failure classification (a Conversations contract invariant was violated).
    /// </summary>
    public static ConformanceFailureClassification ProductInvariant { get; } = new("product-invariant");

    /// <summary>
    /// Gets the infrastructure failure classification.
    /// </summary>
    public static ConformanceFailureClassification Infrastructure { get; } = new("infrastructure");

    /// <summary>
    /// Gets the configuration failure classification.
    /// </summary>
    public static ConformanceFailureClassification Configuration { get; } = new("configuration");

    /// <summary>
    /// Gets the unavailable-dependency failure classification.
    /// </summary>
    public static ConformanceFailureClassification UnavailableDependency { get; } = new("unavailable-dependency");

    /// <summary>
    /// Gets the execution failure classification.
    /// </summary>
    public static ConformanceFailureClassification Execution { get; } = new("execution");

    private static readonly IReadOnlyDictionary<string, ConformanceFailureClassification> KnownValues = Known(
        Conformant,
        ProductInvariant,
        Infrastructure,
        Configuration,
        UnavailableDependency,
        Execution);

    private ConformanceFailureClassification(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>
    /// Gets the canonical wire value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets every supported failure classification.
    /// </summary>
    public static IReadOnlyList<ConformanceFailureClassification> All { get; } =
    [
        Conformant,
        ProductInvariant,
        Infrastructure,
        Configuration,
        UnavailableDependency,
        Execution,
    ];

    /// <summary>
    /// Gets a value indicating whether this classification represents a failure (anything but conformant).
    /// </summary>
    public bool IsFailure => !Equals(Conformant);

    /// <summary>
    /// Resolves a supported failure classification.
    /// </summary>
    /// <param name="value">The canonical classification value.</param>
    /// <returns>The matching failure classification.</returns>
    public static ConformanceFailureClassification Parse(string value)
        => ParseKnown(value, KnownValues, nameof(ConformanceFailureClassification));

    /// <inheritdoc />
    public override string ToString() => Value;
}

internal static class ConformanceVocabularyValidation
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
