// <copyright file="Program.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Conversations.Admin.Web.Rendering;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IInvestigationWorkspaceCatalog, BuyerAcceptanceInvestigationWorkspaceCatalog>();
builder.Services.AddSingleton<InvestigationWorkspaceRenderer>();

WebApplication app = builder.Build();

app.MapGet("/", () => Results.Redirect("/investigations"));
app.MapGet("/favicon.ico", () => Results.NoContent());
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/investigations/fixtures", (IInvestigationWorkspaceCatalog catalog) => Results.Json(catalog.List()));
app.MapGet(
    "/investigations",
    (string? fixture, IInvestigationWorkspaceCatalog catalog, InvestigationWorkspaceRenderer renderer) =>
    {
        InvestigationWorkspaceViewModel workspace = catalog.Get(fixture);
        return Results.Content(renderer.Render(workspace), "text/html; charset=utf-8");
    });

app.Run();

/// <summary>
/// Exposes the generated top-level program type to browser host tests.
/// </summary>
public partial class Program;
