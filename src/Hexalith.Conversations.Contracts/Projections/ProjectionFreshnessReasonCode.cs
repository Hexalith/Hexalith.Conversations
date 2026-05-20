// <copyright file="ProjectionFreshnessReasonCode.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Projections;

/// <summary>
/// Defines the public allowlist of safe freshness reason codes.
/// </summary>
[JsonConverter(typeof(ProjectionFreshnessReasonCodeJsonConverter))]
public sealed record ProjectionFreshnessReasonCode
{
    /// <summary>
    /// Gets the reason code for current projection metadata.
    /// </summary>
    public static ProjectionFreshnessReasonCode Current { get; } = new("current");

    /// <summary>
    /// Gets the reason code for metadata older than the accepted freshness threshold.
    /// </summary>
    public static ProjectionFreshnessReasonCode StaleThresholdExceeded { get; } = new("stale_threshold_exceeded");

    /// <summary>
    /// Gets the reason code for active or required rebuild work.
    /// </summary>
    public static ProjectionFreshnessReasonCode Rebuilding { get; } = new("rebuilding");

    /// <summary>
    /// Gets the reason code for unavailable projection evidence.
    /// </summary>
    public static ProjectionFreshnessReasonCode Unavailable { get; } = new("unavailable");

    /// <summary>
    /// Gets the reason code for hidden tenant-isolation results.
    /// </summary>
    public static ProjectionFreshnessReasonCode Forbidden { get; } = new("forbidden");

    /// <summary>
    /// Gets the reason code for policy-redacted content.
    /// </summary>
    public static ProjectionFreshnessReasonCode Redacted { get; } = new("redacted");

    /// <summary>
    /// Gets the reason code for contradictory freshness metadata.
    /// </summary>
    public static ProjectionFreshnessReasonCode MetadataContradictory { get; } = new("metadata_contradictory");

    /// <summary>
    /// Gets the reason code for a detected source-position gap.
    /// </summary>
    public static ProjectionFreshnessReasonCode GapDetected { get; } = new("gap_detected");

    /// <summary>
    /// Gets the reason code for an event observed before required predecessor evidence.
    /// </summary>
    public static ProjectionFreshnessReasonCode OutOfOrderEvent { get; } = new("out_of_order_event");

    /// <summary>
    /// Gets the reason code for summary/detail data from different generations.
    /// </summary>
    public static ProjectionFreshnessReasonCode MixedGeneration { get; } = new("mixed_generation");

    /// <summary>
    /// Gets the reason code for a mixed-tenant or mismatched-conversation poison event.
    /// </summary>
    public static ProjectionFreshnessReasonCode PoisonEvent { get; } = new("poison_event");

    /// <summary>
    /// Gets the reason code for a failed metadata write after projection mutation.
    /// </summary>
    public static ProjectionFreshnessReasonCode MetadataWriteFailed { get; } = new("metadata_write_failed");

    private static readonly IReadOnlyDictionary<string, ProjectionFreshnessReasonCode> KnownCodes =
        new Dictionary<string, ProjectionFreshnessReasonCode>(StringComparer.Ordinal)
        {
            [Current.Value] = Current,
            [StaleThresholdExceeded.Value] = StaleThresholdExceeded,
            [Rebuilding.Value] = Rebuilding,
            [Unavailable.Value] = Unavailable,
            [Forbidden.Value] = Forbidden,
            [Redacted.Value] = Redacted,
            [MetadataContradictory.Value] = MetadataContradictory,
            [GapDetected.Value] = GapDetected,
            [OutOfOrderEvent.Value] = OutOfOrderEvent,
            [MixedGeneration.Value] = MixedGeneration,
            [PoisonEvent.Value] = PoisonEvent,
            [MetadataWriteFailed.Value] = MetadataWriteFailed,
        };

    private ProjectionFreshnessReasonCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the safe reason-code token.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a public reason-code token.
    /// </summary>
    /// <param name="value">The reason-code token.</param>
    /// <returns>The matching public reason code.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is empty or unsupported.</exception>
    public static ProjectionFreshnessReasonCode Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownCodes.TryGetValue(value, out ProjectionFreshnessReasonCode? code)
            ? code
            : throw new ArgumentException($"Unsupported projection freshness reason code '{value}'.", nameof(value));
    }
}
