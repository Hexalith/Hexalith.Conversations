// <copyright file="AuditRecordActionClassification.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Defines the closed public action classes for governed audit-record access.
/// </summary>
[JsonConverter(typeof(AuditRecordActionClassificationJsonConverter))]
public sealed record AuditRecordActionClassification
{
    public static AuditRecordActionClassification Allowed { get; } = new(nameof(Allowed));

    public static AuditRecordActionClassification Denied { get; } = new(nameof(Denied));

    public static AuditRecordActionClassification Redacted { get; } = new(nameof(Redacted));

    public static AuditRecordActionClassification Exported { get; } = new(nameof(Exported));

    public static AuditRecordActionClassification SeparatelyLogged { get; } = new(nameof(SeparatelyLogged));

    public static AuditRecordActionClassification PolicyBlocked { get; } = new(nameof(PolicyBlocked));

    private static readonly IReadOnlyDictionary<string, AuditRecordActionClassification> KnownValues =
        new[]
        {
            Allowed,
            Denied,
            Redacted,
            Exported,
            SeparatelyLogged,
            PolicyBlocked,
        }.ToDictionary(value => value.Value, StringComparer.Ordinal);

    private AuditRecordActionClassification(string value)
        => Value = GovernanceContractValidation.RequiredSafeToken(value, nameof(value));

    public string Value { get; }

    public static AuditRecordActionClassification Parse(string value)
    {
        GovernanceContractValidation.RequiredSafeToken(value, nameof(value));
        return KnownValues.TryGetValue(value, out AuditRecordActionClassification? known)
            ? known
            : throw new ArgumentException("Unsupported AuditRecordActionClassification value.", nameof(value));
    }

    public override string ToString() => Value;
}
