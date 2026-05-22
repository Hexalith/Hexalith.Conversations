// <copyright file="ContractVersionInfo.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Versioning;

/// <summary>
/// Describes active contract and schema versions exposed to adopters.
/// </summary>
/// <param name="contractName">The public contract family name.</param>
/// <param name="activeSchemaVersion">The active schema version.</param>
/// <param name="minimumSupportedSchemaVersion">The minimum supported schema version.</param>
public sealed record ContractVersionInfo(
    string ContractName,
    SchemaVersion ActiveSchemaVersion,
    SchemaVersion MinimumSupportedSchemaVersion)
{
    private ContractCompatibilityStatus _status = ContractCompatibilityStatus.Supported;

    /// <summary>
    /// Gets the public contract family name.
    /// </summary>
    public string ContractName { get; } = EnsureRequiredText(ContractName, nameof(ContractName));

    /// <summary>
    /// Gets the active schema version.
    /// </summary>
    public SchemaVersion ActiveSchemaVersion { get; } = ActiveSchemaVersion ?? throw new ArgumentNullException(nameof(ActiveSchemaVersion));

    /// <summary>
    /// Gets the minimum supported schema version.
    /// </summary>
    public SchemaVersion MinimumSupportedSchemaVersion { get; } = ValidateMinimum(ActiveSchemaVersion, MinimumSupportedSchemaVersion);

    /// <summary>
    /// Gets the adopter-facing compatibility status for this contract family.
    /// </summary>
    public ContractCompatibilityStatus Status
    {
        get => _status;
        init => _status = value ?? throw new ArgumentNullException(nameof(value));
    }

    private static string EnsureRequiredText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }

    private static SchemaVersion ValidateMinimum(SchemaVersion activeSchemaVersion, SchemaVersion minimumSupportedSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(activeSchemaVersion);
        ArgumentNullException.ThrowIfNull(minimumSupportedSchemaVersion);
        return minimumSupportedSchemaVersion.Value > activeSchemaVersion.Value
            ? throw new ArgumentOutOfRangeException(nameof(minimumSupportedSchemaVersion), "Minimum supported schema version cannot exceed the active schema version.")
            : minimumSupportedSchemaVersion;
    }
}

/// <summary>
/// Reports an unsupported schema version without exposing runtime details.
/// </summary>
/// <param name="requestedSchemaVersion">The requested unsupported schema version.</param>
/// <param name="activeSchemaVersion">The active schema version.</param>
/// <param name="minimumSupportedSchemaVersion">The minimum supported schema version.</param>
public sealed record UnsupportedSchemaVersion(
    SchemaVersion RequestedSchemaVersion,
    SchemaVersion ActiveSchemaVersion,
    SchemaVersion MinimumSupportedSchemaVersion)
{
    /// <summary>
    /// Gets the minimum supported schema version.
    /// </summary>
    public SchemaVersion MinimumSupportedSchemaVersion { get; } = ValidateInvariant(RequestedSchemaVersion, ActiveSchemaVersion, MinimumSupportedSchemaVersion);

    private static SchemaVersion ValidateInvariant(SchemaVersion requestedSchemaVersion, SchemaVersion activeSchemaVersion, SchemaVersion minimumSupportedSchemaVersion)
    {
        ArgumentNullException.ThrowIfNull(requestedSchemaVersion);
        ArgumentNullException.ThrowIfNull(activeSchemaVersion);
        ArgumentNullException.ThrowIfNull(minimumSupportedSchemaVersion);

        if (minimumSupportedSchemaVersion.Value > activeSchemaVersion.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSupportedSchemaVersion), "Minimum supported schema version cannot exceed the active schema version.");
        }

        if (requestedSchemaVersion.Value >= minimumSupportedSchemaVersion.Value && requestedSchemaVersion.Value <= activeSchemaVersion.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedSchemaVersion), "Requested schema version must fall outside the [minimum, active] supported range.");
        }

        return minimumSupportedSchemaVersion;
    }
}
