// <copyright file="FileReferenceHydrationV1.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.TrustStates;

namespace Hexalith.Conversations.Contracts.Queries;

/// <summary>
/// Carries response-scoped file reference hydration.
/// </summary>
public sealed record FileReferenceHydrationV1(
    FileId FileId,
    ProjectionTrustState HydrationState,
    bool Resolved,
    string SafeLabel,
    string SafeToken,
    string SafeStatus)
{
    /// <summary>
    /// Gets the stable file identity.
    /// </summary>
    public FileId FileId { get; } = FileId ?? throw new ArgumentNullException(nameof(FileId));

    /// <summary>
    /// Gets the public hydration state.
    /// </summary>
    public ProjectionTrustState HydrationState { get; } = HydrationState ?? throw new ArgumentNullException(nameof(HydrationState));

    /// <summary>
    /// Gets the policy-approved label or fallback text.
    /// </summary>
    public string SafeLabel { get; } = HydrationContractValidation.RequiredSafeText(SafeLabel, nameof(SafeLabel));

    /// <summary>
    /// Gets the policy-approved visual token or fallback token.
    /// </summary>
    public string SafeToken { get; } = HydrationContractValidation.RequiredSafeText(SafeToken, nameof(SafeToken));

    /// <summary>
    /// Gets the policy-approved status or fallback status.
    /// </summary>
    public string SafeStatus { get; } = HydrationContractValidation.RequiredSafeText(SafeStatus, nameof(SafeStatus));
}
