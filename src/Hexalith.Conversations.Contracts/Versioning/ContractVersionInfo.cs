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
    SchemaVersion MinimumSupportedSchemaVersion);

/// <summary>
/// Reports an unsupported schema version without exposing runtime details.
/// </summary>
/// <param name="requestedSchemaVersion">The requested unsupported schema version.</param>
/// <param name="activeSchemaVersion">The active schema version.</param>
/// <param name="minimumSupportedSchemaVersion">The minimum supported schema version.</param>
public sealed record UnsupportedSchemaVersion(
    SchemaVersion RequestedSchemaVersion,
    SchemaVersion ActiveSchemaVersion,
    SchemaVersion MinimumSupportedSchemaVersion);
