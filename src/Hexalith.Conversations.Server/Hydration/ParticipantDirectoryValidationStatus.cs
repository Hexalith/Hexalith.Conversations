// <copyright file="ParticipantDirectoryValidationStatus.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Hydration;

/// <summary>
/// Describes content-safe command-time Party validation outcomes.
/// </summary>
public enum ParticipantDirectoryValidationStatus
{
    /// <summary>
    /// The participant Party reference is valid for the command tenant.
    /// </summary>
    Valid = 0,

    /// <summary>
    /// Party validation is unavailable.
    /// </summary>
    Unavailable = 1,

    /// <summary>
    /// The validation outcome is unknown.
    /// </summary>
    Unknown = 2,

    /// <summary>
    /// The Party reference cannot be accessed by the command context.
    /// </summary>
    Inaccessible = 3,

    /// <summary>
    /// Party validation timed out.
    /// </summary>
    Timeout = 4,

    /// <summary>
    /// Party validation failed with a safe error classification.
    /// </summary>
    Error = 5,

    /// <summary>
    /// The Party reference was not found.
    /// </summary>
    NotFound = 6,

    /// <summary>
    /// The Party reference could not be proven for the command tenant.
    /// </summary>
    TenantMismatch = 7,

    /// <summary>
    /// The Party reference is disabled.
    /// </summary>
    Disabled = 8,

    /// <summary>
    /// The Party reference is malformed.
    /// </summary>
    Malformed = 9,

    /// <summary>
    /// The Party validation outcome is indeterminate.
    /// </summary>
    Indeterminate = 10,
}
