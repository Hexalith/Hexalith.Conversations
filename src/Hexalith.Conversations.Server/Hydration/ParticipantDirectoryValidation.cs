// <copyright file="ParticipantDirectoryValidation.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Represents a content-safe command-time participant validation result.
/// </summary>
/// <param name="Status">The participant directory validation status.</param>
public sealed record ParticipantDirectoryValidation(ParticipantDirectoryValidationStatus Status)
{
    /// <summary>
    /// Creates a successful participant validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ParticipantDirectoryValidation Valid() => new(ParticipantDirectoryValidationStatus.Valid);
}
