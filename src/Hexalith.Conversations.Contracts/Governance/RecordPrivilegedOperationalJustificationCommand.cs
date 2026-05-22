// <copyright file="RecordPrivilegedOperationalJustificationCommand.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Contracts.Governance;

/// <summary>
/// Requests audit recording for a privileged operational justification.
/// </summary>
/// <param name="justification">The structured privileged operational justification.</param>
public sealed record RecordPrivilegedOperationalJustificationCommand(
    PrivilegedOperationalJustificationV1 Justification)
{
    public PrivilegedOperationalJustificationV1 Justification { get; } =
        GovernanceContractValidation.RequireNonNull(Justification, nameof(Justification));
}
