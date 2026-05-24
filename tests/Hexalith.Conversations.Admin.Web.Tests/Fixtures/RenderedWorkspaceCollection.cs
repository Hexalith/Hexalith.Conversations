// <copyright file="RenderedWorkspaceCollection.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Hexalith.Conversations.Admin.Web.Tests.Fixtures;

/// <summary>
/// Shares the rendered host and browser across responsive evidence tests.
/// </summary>
[CollectionDefinition(Name)]
public sealed class RenderedWorkspaceCollection :
    ICollectionFixture<AdminWebHostFixture>,
    ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "RenderedWorkspace";
}
