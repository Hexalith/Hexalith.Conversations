// <copyright file="ParticipantDirectoryValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Represents a content-safe command-time participant validation result.
/// </summary>
/// <param name="Status">The participant directory validation status.</param>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="Status"/> is cast from an undefined enum value. Guarding here keeps
/// undefined integer casts from reaching the rejection-code surface where they would otherwise
/// produce non-vocabulary strings such as <c>participant_validation_999</c>.
/// </exception>
public sealed record ParticipantDirectoryValidation(ParticipantDirectoryValidationStatus Status)
{
    /// <summary>
    /// Gets the participant directory validation status.
    /// </summary>
    public ParticipantDirectoryValidationStatus Status { get; } = ValidateStatus(Status);

    /// <summary>
    /// Creates a successful participant validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ParticipantDirectoryValidation Valid() => new(ParticipantDirectoryValidationStatus.Valid);

    private static ParticipantDirectoryValidationStatus ValidateStatus(ParticipantDirectoryValidationStatus value)
        => Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(Status), value, "Undefined ParticipantDirectoryValidationStatus value.");
}
