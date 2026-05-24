// <copyright file="InvestigationWorkspaceViewModel.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Contracts.Projections;
using Hexalith.Conversations.Contracts.Queries;

namespace Hexalith.Conversations.Admin.Web.Rendering;

/// <summary>
/// Permission-safe data for the rendered investigation workspace.
/// </summary>
public sealed record InvestigationWorkspaceViewModel(
    string FixtureId,
    string SafeLabel,
    string SafeTenantScopeLabel,
    string SafeRecordIdentityLabel,
    string SafeTrustPostureLabel,
    string SafeEvidenceCompletenessLabel,
    string SafeCommandEligibilityLabel,
    string SafeTelemetryLabel,
    bool MobileReadOnlyTriage,
    IReadOnlyList<string> SafeFixtureTags,
    ConversationSummaryProjectionV1? Summary,
    ConversationDetailProjectionV1? Detail,
    IReadOnlyList<ConversationEvidenceEntryV1> EvidenceEntries,
    IReadOnlyList<ConversationCommandAvailabilityV1> CommandEligibility)
{
    /// <summary>
    /// Gets a value indicating whether the fixture intentionally represents an indistinguishable hidden read.
    /// </summary>
    public bool IsHiddenRead => Summary is null || Detail is null;
}
