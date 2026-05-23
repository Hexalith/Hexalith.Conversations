// <copyright file="ConversationPrivilegedAccessClass.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Server.Diagnostics;

/// <summary>
/// Bounded vocabulary for privileged access attempt signal classification.
/// </summary>
public enum ConversationPrivilegedAccessClass
{
    /// <summary>
    /// No privileged access (default guard value — must not be recorded as a signal).
    /// </summary>
    None = 0,

    /// <summary>
    /// Authorized privileged operation was performed.
    /// </summary>
    AuthorizedPrivilegedOperation = 1,

    /// <summary>
    /// Unauthorized attempt to perform a privileged operation.
    /// </summary>
    UnauthorizedPrivilegedAttempt = 2,
}
