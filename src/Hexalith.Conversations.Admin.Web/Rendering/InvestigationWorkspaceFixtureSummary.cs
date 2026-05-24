// <copyright file="InvestigationWorkspaceFixtureSummary.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Admin.Web.Rendering;

/// <summary>
/// Describes one rendered responsive evidence fixture.
/// </summary>
public sealed record InvestigationWorkspaceFixtureSummary(
    string FixtureId,
    string SafeLabel,
    string SafeTelemetryLabel,
    bool MobileReadOnlyTriage);
