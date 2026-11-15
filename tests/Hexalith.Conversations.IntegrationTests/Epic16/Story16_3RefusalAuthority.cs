// <copyright file="Story16_3RefusalAuthority.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.Conversations.IntegrationTests.Epic16;

/// <summary>A real loopback HTTP refusal for the predecessor's ungranted worker authority; issues no tokens.</summary>
internal sealed class Story16_3RefusalAuthority : IAsyncDisposable
{
    private WebApplication? _host;
    private int _requests;

    /// <summary>Gets the actual endpoint the SDK worker transport will contact.</summary>
    public string BaseAddress { get; private set; } = string.Empty;

    /// <summary>Gets the number of actual authority requests refused.</summary>
    public int Requests => Volatile.Read(ref _requests);

    /// <summary>Starts an isolated authority that refuses every request immediately without transport retries.</summary>
    public async Task StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
        _host = builder.Build();
        _host.Run(context =>
        {
            Interlocked.Increment(ref _requests);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        });
        await _host.StartAsync();
        BaseAddress = _host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            await _host.DisposeAsync();
            _host = null;
        }
    }
}
