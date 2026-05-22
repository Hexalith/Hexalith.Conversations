// <copyright file="ConversationSearchVocabularies.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Defines whether citation metadata is safe to use from a search summary.
/// </summary>
[JsonConverter(typeof(ConversationCitationAvailabilityJsonConverter))]
public sealed record ConversationCitationAvailability
{
    public static ConversationCitationAvailability Available { get; } = new(nameof(Available));

    public static ConversationCitationAvailability Unavailable { get; } = new(nameof(Unavailable));

    public static ConversationCitationAvailability Incomplete { get; } = new(nameof(Incomplete));

    public static ConversationCitationAvailability Unknown { get; } = new(nameof(Unknown));

    private static readonly IReadOnlyDictionary<string, ConversationCitationAvailability> KnownValues =
        new Dictionary<string, ConversationCitationAvailability>(StringComparer.Ordinal)
        {
            [nameof(Available)] = Available,
            [nameof(Unavailable)] = Unavailable,
            [nameof(Incomplete)] = Incomplete,
            [nameof(Unknown)] = Unknown,
        };

    private ConversationCitationAvailability(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ConversationCitationAvailability Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownValues.TryGetValue(value, out ConversationCitationAvailability? parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported citation availability '{value}'.", nameof(value));
    }
}

/// <summary>
/// Defines whether summary metadata is ready for governed audit workflows.
/// </summary>
[JsonConverter(typeof(ConversationAuditReadinessStateJsonConverter))]
public sealed record ConversationAuditReadinessState
{
    public static ConversationAuditReadinessState Ready { get; } = new(nameof(Ready));

    public static ConversationAuditReadinessState Incomplete { get; } = new(nameof(Incomplete));

    public static ConversationAuditReadinessState Unavailable { get; } = new(nameof(Unavailable));

    public static ConversationAuditReadinessState Unknown { get; } = new(nameof(Unknown));

    private static readonly IReadOnlyDictionary<string, ConversationAuditReadinessState> KnownValues =
        new Dictionary<string, ConversationAuditReadinessState>(StringComparer.Ordinal)
        {
            [nameof(Ready)] = Ready,
            [nameof(Incomplete)] = Incomplete,
            [nameof(Unavailable)] = Unavailable,
            [nameof(Unknown)] = Unknown,
        };

    private ConversationAuditReadinessState(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ConversationAuditReadinessState Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownValues.TryGetValue(value, out ConversationAuditReadinessState? parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported audit readiness state '{value}'.", nameof(value));
    }
}

/// <summary>
/// Defines the public verification state of a search summary.
/// </summary>
[JsonConverter(typeof(ConversationVerificationStateJsonConverter))]
public sealed record ConversationVerificationState
{
    public static ConversationVerificationState Verified { get; } = new(nameof(Verified));

    public static ConversationVerificationState Unverified { get; } = new(nameof(Unverified));

    public static ConversationVerificationState Failed { get; } = new(nameof(Failed));

    public static ConversationVerificationState Unavailable { get; } = new(nameof(Unavailable));

    public static ConversationVerificationState Unknown { get; } = new(nameof(Unknown));

    private static readonly IReadOnlyDictionary<string, ConversationVerificationState> KnownValues =
        new Dictionary<string, ConversationVerificationState>(StringComparer.Ordinal)
        {
            [nameof(Verified)] = Verified,
            [nameof(Unverified)] = Unverified,
            [nameof(Failed)] = Failed,
            [nameof(Unavailable)] = Unavailable,
            [nameof(Unknown)] = Unknown,
        };

    private ConversationVerificationState(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ConversationVerificationState Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownValues.TryGetValue(value, out ConversationVerificationState? parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported verification state '{value}'.", nameof(value));
    }
}

/// <summary>
/// Defines the safe source of a visible search match.
/// </summary>
[JsonConverter(typeof(ConversationSearchMatchSourceJsonConverter))]
public sealed record ConversationSearchMatchSource
{
    public static ConversationSearchMatchSource TenantScope { get; } = new(nameof(TenantScope));

    public static ConversationSearchMatchSource BusinessReference { get; } = new(nameof(BusinessReference));

    public static ConversationSearchMatchSource ProjectReference { get; } = new(nameof(ProjectReference));

    public static ConversationSearchMatchSource FolderReference { get; } = new(nameof(FolderReference));

    public static ConversationSearchMatchSource ParticipantReference { get; } = new(nameof(ParticipantReference));

    public static ConversationSearchMatchSource LifecycleState { get; } = new(nameof(LifecycleState));

    public static ConversationSearchMatchSource DateRange { get; } = new(nameof(DateRange));

    public static ConversationSearchMatchSource RedactionState { get; } = new(nameof(RedactionState));

    public static ConversationSearchMatchSource FreshnessState { get; } = new(nameof(FreshnessState));

    public static ConversationSearchMatchSource AuditReadiness { get; } = new(nameof(AuditReadiness));

    public static ConversationSearchMatchSource VerificationState { get; } = new(nameof(VerificationState));

    public static ConversationSearchMatchSource Unknown { get; } = new(nameof(Unknown));

    private static readonly IReadOnlyDictionary<string, ConversationSearchMatchSource> KnownValues =
        new Dictionary<string, ConversationSearchMatchSource>(StringComparer.Ordinal)
        {
            [nameof(TenantScope)] = TenantScope,
            [nameof(BusinessReference)] = BusinessReference,
            [nameof(ProjectReference)] = ProjectReference,
            [nameof(FolderReference)] = FolderReference,
            [nameof(ParticipantReference)] = ParticipantReference,
            [nameof(LifecycleState)] = LifecycleState,
            [nameof(DateRange)] = DateRange,
            [nameof(RedactionState)] = RedactionState,
            [nameof(FreshnessState)] = FreshnessState,
            [nameof(AuditReadiness)] = AuditReadiness,
            [nameof(VerificationState)] = VerificationState,
            [nameof(Unknown)] = Unknown,
        };

    private ConversationSearchMatchSource(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public static ConversationSearchMatchSource Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownValues.TryGetValue(value, out ConversationSearchMatchSource? parsed)
            ? parsed
            : throw new ArgumentException($"Unsupported search match source '{value}'.", nameof(value));
    }
}
