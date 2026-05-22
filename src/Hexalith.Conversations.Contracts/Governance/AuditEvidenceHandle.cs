// <copyright file="AuditEvidenceHandle.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Carries an opaque Conversations-owned audit evidence reference.
/// </summary>
/// <param name="value">The content-safe opaque evidence reference.</param>
public sealed record AuditEvidenceHandle(string Value)
{
    public string Value { get; } = GovernanceContractValidation.RequiredSafeToken(Value, nameof(Value));
}
