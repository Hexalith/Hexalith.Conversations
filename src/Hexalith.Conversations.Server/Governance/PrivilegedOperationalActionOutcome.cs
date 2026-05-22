// <copyright file="PrivilegedOperationalActionOutcome.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Governance;

namespace Hexalith.Conversations.Server.Governance;

/// <summary>
/// Carries the bounded result of a privileged operation delegate after preconditions pass.
/// </summary>
/// <param name="Outcome">The public governance outcome.</param>
/// <param name="SafeNextAction">The content-safe next action.</param>
public sealed record PrivilegedOperationalActionOutcome(
    GovernanceOutcome Outcome,
    string SafeNextAction)
{
    public GovernanceOutcome Outcome { get; } = Outcome ?? throw new ArgumentNullException(nameof(Outcome));

    public string SafeNextAction { get; } = ValidateSafeNextAction(SafeNextAction);

    public static PrivilegedOperationalActionOutcome Succeeded()
        => new(GovernanceOutcome.Succeeded, "Use the returned audit handle as governed evidence.");

    public static PrivilegedOperationalActionOutcome Denied(string safeNextAction)
        => new(GovernanceOutcome.Denied, safeNextAction);

    public static PrivilegedOperationalActionOutcome Partial(string safeNextAction)
        => new(GovernanceOutcome.PolicyBlocked, safeNextAction);

    public static PrivilegedOperationalActionOutcome PolicyBlocked(string safeNextAction)
        => new(GovernanceOutcome.PolicyBlocked, safeNextAction);

    private static string ValidateSafeNextAction(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || value.Contains("storage", StringComparison.OrdinalIgnoreCase)
            || value.Contains("raw", StringComparison.OrdinalIgnoreCase)
            || value.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Value must remain content-safe.", nameof(value));
        }

        return value;
    }
}
