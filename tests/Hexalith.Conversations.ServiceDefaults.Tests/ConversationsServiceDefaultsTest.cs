// <copyright file="ConversationsServiceDefaultsTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Hexalith.Commons.ServiceDefaults;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.ServiceDefaults.Tests;

/// <summary>
/// Story 3.4 (AC-2, AC-3, AC-4, AC-5) — proves the Conversations-owned thin wrapper over the shared
/// <see cref="HexalithServiceDefaults"/> base configures Conversations-specific names, preserves the
/// existing <c>/health</c>, <c>/alive</c>, <c>/ready</c> endpoint contract, registers the shared
/// observability/discovery/resilience side effects, and adopts the base exactly once (no double
/// registration of the shared self health check).
/// </summary>
public sealed class ConversationsServiceDefaultsTest
{
    /// <summary>
    /// AC-3 — the module configuration delegate fails closed on a null options instance.
    /// </summary>
    [Fact]
    public void ConfigureConversationsDefaultsShouldThrowOnNullOptions()
        => _ = Should.Throw<ArgumentNullException>(
            static () => ConversationsServiceDefaults.ConfigureConversationsDefaults(null!));

    /// <summary>
    /// AC-3 — Conversations keeps its own service/resource name on the shared base.
    /// </summary>
    [Fact]
    public void ConfigureConversationsDefaultsShouldSetConversationsServiceName()
    {
        HexalithServiceDefaultsOptions options =
            HexalithServiceDefaultsOptions.Create(ConversationsServiceDefaults.ConfigureConversationsDefaults);

        options.ServiceName.ShouldBe("Hexalith.Conversations");
        options.ServiceName.ShouldBe(ConversationsServiceDefaults.ServiceName);
    }

    /// <summary>
    /// AC-3 / AC-5 — the Conversations meter source is registered so its instrumentation is not dropped.
    /// </summary>
    [Fact]
    public void ConfigureConversationsDefaultsShouldRegisterConversationsMeter()
    {
        HexalithServiceDefaultsOptions options =
            HexalithServiceDefaultsOptions.Create(ConversationsServiceDefaults.ConfigureConversationsDefaults);

        options.MeterNames.ShouldContain("Hexalith.Conversations");
    }

    /// <summary>
    /// AC-4 — adopting the shared base must keep the Conversations-visible three-endpoint health contract.
    /// </summary>
    [Fact]
    public void ConfigureConversationsDefaultsShouldPreserveDefaultHealthEndpointContract()
    {
        HexalithServiceDefaultsOptions options =
            HexalithServiceDefaultsOptions.Create(ConversationsServiceDefaults.ConfigureConversationsDefaults);

        options.HealthEndpointPath.ShouldBe("/health");
        options.LivenessEndpointPath.ShouldBe("/alive");
        options.ReadinessEndpointPath.ShouldBe("/ready");
    }

    /// <summary>
    /// AC-2 / AC-3 — the wrapper fails closed on a null builder rather than registering against nothing.
    /// </summary>
    [Fact]
    public void AddConversationsServiceDefaultsShouldThrowOnNullBuilder()
        => _ = Should.Throw<ArgumentNullException>(
            static () => ConversationsServiceDefaults.AddConversationsServiceDefaults<IHostApplicationBuilder>(null!));

    /// <summary>
    /// AC-5 — the wrapper still wires service discovery, HTTP resilience, and OpenTelemetry through the base.
    /// </summary>
    [Fact]
    public void AddConversationsServiceDefaultsShouldRegisterDiscoveryResilienceAndOpenTelemetry()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        _ = builder.AddConversationsServiceDefaults();

        string descriptorText = string.Join(
            Environment.NewLine,
            builder.Services.Select(static descriptor => descriptor.ToString()));
        descriptorText.ShouldContain("ServiceDiscovery");
        descriptorText.ShouldContain("Resilience");
        descriptorText.ShouldContain("OpenTelemetry");
    }

    /// <summary>
    /// AC-2 / AC-3 — adopting the shared base registers the liveness self check exactly once (no duplicate).
    /// </summary>
    [Fact]
    public void AddConversationsServiceDefaultsShouldRegisterSingleLiveSelfHealthCheck()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        _ = builder.AddConversationsServiceDefaults();

        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        HealthCheckServiceOptions healthOptions =
            provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;

        HealthCheckRegistration self = healthOptions.Registrations.Single(static r => r.Name == "self");
        self.Tags.ShouldContain("live");
        self.Tags.ShouldNotContain("ready");
    }
}
