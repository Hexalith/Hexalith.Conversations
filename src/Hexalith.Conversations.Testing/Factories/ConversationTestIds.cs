// <copyright file="ConversationTestIds.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Testing.Factories;

/// <summary>
/// Creates deterministic, tenant-scoped identifiers for conversation tests.
/// </summary>
public static class ConversationTestIds
{
    /// <summary>
    /// Creates a tenant identifier for a test case.
    /// </summary>
    /// <param name="scenario">The scenario name used to make the identifier readable.</param>
    /// <returns>A stable tenant identifier.</returns>
    public static string Tenant(string scenario) => Create("tenant", scenario);

    /// <summary>
    /// Creates a conversation identifier for a test case.
    /// </summary>
    /// <param name="scenario">The scenario name used to make the identifier readable.</param>
    /// <returns>A stable conversation identifier.</returns>
    public static string Conversation(string scenario) => Create("conversation", scenario);

    /// <summary>
    /// Creates a party identifier for a test case.
    /// </summary>
    /// <param name="scenario">The scenario name used to make the identifier readable.</param>
    /// <returns>A stable party identifier.</returns>
    public static string Party(string scenario) => Create("party", scenario);

    private static string Create(string prefix, string scenario)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenario);

        return $"{prefix}-{Normalize(scenario)}";
    }

    private static string Normalize(string value)
    {
        char[] characters = value
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        string normalized = new(characters);
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        return normalized.Trim('-');
    }
}

