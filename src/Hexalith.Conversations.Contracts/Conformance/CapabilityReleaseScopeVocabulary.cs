// <copyright file="CapabilityReleaseScopeVocabulary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;
using static Hexalith.Conversations.Contracts.Conformance.ConformanceVocabularyValidation;

namespace Hexalith.Conversations.Contracts.Conformance;

/// <summary>
/// Defines the closed release-scope classification vocabulary for product capabilities (FR100).
/// </summary>
[JsonConverter(typeof(CapabilityReleaseScopeJsonConverter))]
public sealed record CapabilityReleaseScope
{
    /// <summary>Gets the v1 scope: capability is included in the current major release.</summary>
    public static CapabilityReleaseScope V1 { get; } = new("v1");

    /// <summary>Gets the v1.1 scope: capability is planned for the first minor release.</summary>
    public static CapabilityReleaseScope V1Point1 { get; } = new("v1-1");

    /// <summary>Gets the vnext scope: capability is planned for a future major release.</summary>
    public static CapabilityReleaseScope VNext { get; } = new("vnext");

    /// <summary>Gets the deferred scope: capability is explicitly deferred beyond roadmap.</summary>
    public static CapabilityReleaseScope Deferred { get; } = new("deferred");

    /// <summary>Gets the waived scope: capability is formally waived for release.</summary>
    public static CapabilityReleaseScope Waived { get; } = new("waived");

    /// <summary>Gets the conditional scope: capability inclusion depends on a condition with an expiry date.</summary>
    public static CapabilityReleaseScope Conditional { get; } = new("conditional");

    /// <summary>Gets the out-of-scope classification: capability is explicitly excluded from the product.</summary>
    public static CapabilityReleaseScope OutOfScope { get; } = new("out-of-scope");

    private static readonly IReadOnlyDictionary<string, CapabilityReleaseScope> KnownValues = Known(
        V1, V1Point1, VNext, Deferred, Waived, Conditional, OutOfScope);

    private CapabilityReleaseScope(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>Gets the canonical wire value.</summary>
    public string Value { get; }

    /// <summary>Gets every supported release scope in canonical order.</summary>
    public static IReadOnlyList<CapabilityReleaseScope> All { get; } =
    [
        V1, V1Point1, VNext, Deferred, Waived, Conditional, OutOfScope,
    ];

    /// <summary>Resolves a supported release scope from its wire value.</summary>
    public static CapabilityReleaseScope Parse(string value)
        => ParseKnown(value, KnownValues, nameof(CapabilityReleaseScope));

    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>
/// Defines the closed substrate-consequence area vocabulary for deferred substrate-defining capabilities (FR101).
/// </summary>
[JsonConverter(typeof(SubstrateConsequenceAreaJsonConverter))]
public sealed record SubstrateConsequenceArea
{
    /// <summary>Gets the tenant-isolation consequence area.</summary>
    public static SubstrateConsequenceArea TenantIsolation { get; } = new("tenant-isolation");

    /// <summary>Gets the audit-pairing consequence area.</summary>
    public static SubstrateConsequenceArea AuditPairing { get; } = new("audit-pairing");

    /// <summary>Gets the idempotency consequence area.</summary>
    public static SubstrateConsequenceArea Idempotency { get; } = new("idempotency");

    /// <summary>Gets the schema-evolution consequence area.</summary>
    public static SubstrateConsequenceArea SchemaEvolution { get; } = new("schema-evolution");

    /// <summary>Gets the projection-freshness consequence area.</summary>
    public static SubstrateConsequenceArea ProjectionFreshness { get; } = new("projection-freshness");

    /// <summary>Gets the redaction-replay consequence area.</summary>
    public static SubstrateConsequenceArea RedactionReplay { get; } = new("redaction-replay");

    /// <summary>Gets the provider-portability consequence area.</summary>
    public static SubstrateConsequenceArea ProviderPortability { get; } = new("provider-portability");

    /// <summary>Gets the adopter-compatibility consequence area.</summary>
    public static SubstrateConsequenceArea AdopterCompatibility { get; } = new("adopter-compatibility");

    private static readonly IReadOnlyDictionary<string, SubstrateConsequenceArea> KnownValues = Known(
        TenantIsolation, AuditPairing, Idempotency, SchemaEvolution,
        ProjectionFreshness, RedactionReplay, ProviderPortability, AdopterCompatibility);

    private SubstrateConsequenceArea(string value) => Value = ValidateVocabularyValue(value, nameof(value));

    /// <summary>Gets the canonical wire value.</summary>
    public string Value { get; }

    /// <summary>Gets every supported substrate consequence area in canonical order.</summary>
    public static IReadOnlyList<SubstrateConsequenceArea> All { get; } =
    [
        TenantIsolation,
        AuditPairing,
        Idempotency,
        SchemaEvolution,
        ProjectionFreshness,
        RedactionReplay,
        ProviderPortability,
        AdopterCompatibility,
    ];

    /// <summary>Resolves a supported substrate consequence area from its wire value.</summary>
    public static SubstrateConsequenceArea Parse(string value)
        => ParseKnown(value, KnownValues, nameof(SubstrateConsequenceArea));

    /// <inheritdoc />
    public override string ToString() => Value;
}
