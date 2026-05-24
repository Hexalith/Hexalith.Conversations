// <copyright file="PlaywrightFixture.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Microsoft.Playwright;

namespace Hexalith.Conversations.Admin.Web.Tests.Fixtures;

/// <summary>
/// Owns a single Playwright browser for the responsive evidence test lane.
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowser Browser =>
        _browser ?? throw new InvalidOperationException("PlaywrightFixture has not completed InitializeAsync.");

    public async ValueTask InitializeAsync()
    {
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
        try
        {
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            }).ConfigureAwait(false);
        }
        catch (PlaywrightException ex)
        {
            throw new InvalidOperationException(
                "Playwright Chromium browser is not installed. Run 'pwsh tests/install-playwright.ps1' once per machine before invoking the Admin Web E2E lane.",
                ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();
    }
}
