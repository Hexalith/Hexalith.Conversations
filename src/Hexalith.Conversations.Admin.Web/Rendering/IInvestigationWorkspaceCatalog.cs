// <copyright file="IInvestigationWorkspaceCatalog.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Admin.Web.Rendering;

/// <summary>
/// Provides permission-safe rendered workspace fixtures.
/// </summary>
public interface IInvestigationWorkspaceCatalog
{
    /// <summary>
    /// Gets the workspace fixture selected by the caller.
    /// </summary>
    /// <param name="fixtureId">The fixture identifier, or <see langword="null" /> for the default fixture.</param>
    /// <returns>The selected workspace view model.</returns>
    InvestigationWorkspaceViewModel Get(string? fixtureId);

    /// <summary>
    /// Lists all rendered fixture identifiers.
    /// </summary>
    /// <returns>The ordered fixture identifiers.</returns>
    IReadOnlyList<InvestigationWorkspaceFixtureSummary> List();
}
