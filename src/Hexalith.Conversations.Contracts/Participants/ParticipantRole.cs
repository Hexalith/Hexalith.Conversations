// <copyright file="ParticipantRole.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.Json.Serialization;

using Hexalith.Conversations.Contracts.Serialization;

namespace Hexalith.Conversations.Contracts.Participants;

/// <summary>
/// Defines the supported participant role vocabulary.
/// </summary>
[JsonConverter(typeof(ParticipantRoleJsonConverter))]
public sealed record ParticipantRole
{
    /// <summary>
    /// Gets the default member role.
    /// </summary>
    public static ParticipantRole Member { get; } = new("Member");

    /// <summary>
    /// Gets the facilitator role.
    /// </summary>
    public static ParticipantRole Facilitator { get; } = new("Facilitator");

    /// <summary>
    /// Gets the observer role.
    /// </summary>
    public static ParticipantRole Observer { get; } = new("Observer");

    private static readonly IReadOnlyDictionary<string, ParticipantRole> KnownRoles =
        new[] { Member, Facilitator, Observer }.ToDictionary(role => role.Value, StringComparer.Ordinal);

    private ParticipantRole(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the canonical participant role value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Resolves a supported participant role.
    /// </summary>
    /// <param name="value">The participant role value.</param>
    /// <returns>The supported participant role.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is unsupported.</exception>
    public static ParticipantRole Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return KnownRoles.TryGetValue(value, out ParticipantRole? role)
            ? role
            : throw new ArgumentException($"Unsupported participant role '{value}'.", nameof(value));
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
