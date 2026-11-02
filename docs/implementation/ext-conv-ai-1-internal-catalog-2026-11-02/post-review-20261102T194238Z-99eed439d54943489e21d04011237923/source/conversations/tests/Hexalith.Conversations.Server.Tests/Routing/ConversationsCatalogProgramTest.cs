using System.Text.Json;
using Hexalith.Conversations.Contracts.Routing;
using Hexalith.Conversations.Server.Tests.Agents;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Hexalith.Conversations.Server.Tests.Routing;

/// <summary>Exercises the actual Conversations Program's enabled SDK bootstrap observation without a sidecar.</summary>
public sealed class ConversationsCatalogProgramTest
{
    [Fact]
    public async Task ActualProgramRegistersAndCapturesAllTwentyTwoOwnerRoutesSchemasAndTargets()
    {
        string directory = Path.Combine(Path.GetTempPath(), "conversations-program-observation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string acl = Path.Combine(directory, "acl.json");
        File.WriteAllText(acl, """{"spec":{"accessControl":{"policies":[{"operations":[{"name":"/process","action":"allow"},{"name":"/query","action":"allow"},{"name":"/project","action":"allow"}]}]}}}""");
        string component = Path.Combine(directory, "component.json");
        File.WriteAllText(component, """{"metadata":{"name":"observation-fixture"}}""");
        var state = Substitute.For<IRouteCatalogStateStore>();
        state.Coordinates.Returns(new RouteCatalogStateCoordinates("observation-fixture", "catalog", "http://127.0.0.1:3500/"));
        state.ReadAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<(RouteCatalogActivationState, string)>(new InvalidOperationException("observation-captured")));
        ServiceDescriptor? bootstrap = null;
        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                var settings = DeletionRuntimeTestSettings.Configuration(enabled: false);
                const string prefix = "EventStore:RouteCatalog:DevelopmentBootstrap:";
                settings[prefix + "Directory"] = directory;
                settings[prefix + "HostId"] = "conversations";
                settings[prefix + "CoordinatorHostId"] = "eventstore";
                settings[prefix + "AttemptId"] = "observation-test";
                settings[prefix + "RequiredHostIds:0"] = "conversations";
                settings[prefix + "RequiredHostIds:1"] = "eventstore";
                settings[prefix + "ComponentPath"] = component;
                settings[prefix + "AccessControlPath"] = acl;
                settings["EventStore:DomainService:AppId"] = "conversation";
                settings["EventStore:DomainService:ServiceVersion"] = "v1";
                settings["APP_API_TOKEN"] = "observation-only-channel";
                foreach (var (key, value) in settings) builder.UseSetting(key, value);
                builder.ConfigureServices(services =>
                {
                    bootstrap = services.Single(descriptor => descriptor.ServiceType == typeof(IHostedService)
                        && descriptor.ImplementationFactory?.Method.DeclaringType?.FullName?.StartsWith(
                            typeof(DevelopmentRouteCatalogBootstrap).FullName + "+", StringComparison.Ordinal) == true);
                    services.RemoveAll<IHostedService>();
                    services.RemoveAll<IRouteCatalogStateStore>(); services.AddSingleton(state);
                });
            });
            using var client = factory.CreateClient();
            bootstrap.ShouldNotBeNull("The actual Program must enable the SDK observer registration.");
            using var worker = (BackgroundService)bootstrap.ImplementationFactory!(factory.Services);
            var stopped = await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await worker.StartAsync(TestContext.Current.CancellationToken);
                await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            });
            stopped.Message.ShouldBe("observation-captured");
            var actual = JsonSerializer.Deserialize<RouteCatalogHostObservation>(File.ReadAllBytes(Path.Combine(directory, "conversations.observation.json")))!;
            actual.AppId.ShouldBe("conversation");
            actual.Declarations.Count.ShouldBe(22); actual.Targets.Count.ShouldBe(22); actual.Operations!.Count.ShouldBe(22);
            actual.Declarations.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order()
                .ShouldBe(ConversationsCatalogDeclarations.Routes.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order());
            actual.Operations.Select(operation => operation.Digest).Order().ShouldBe(ConversationsCatalogDeclarations.Operations.Select(operation => operation.Digest).Order());
            actual.Targets.All(target => target.AppId == "conversation").ShouldBeTrue();
            await state.Received(1).ReadAsync(Arg.Any<CancellationToken>());
            await state.DidNotReceive().CompareExchangeAsync(Arg.Any<RouteCatalogActivationState>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
