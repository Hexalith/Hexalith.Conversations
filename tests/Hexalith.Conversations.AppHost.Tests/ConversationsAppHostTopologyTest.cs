// <copyright file="ConversationsAppHostTopologyTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Conversations.AppHost;
using Hexalith.EventStore.Aspire;

using Microsoft.Extensions.Configuration;

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Hexalith.Conversations.AppHost.Tests;

public sealed class ConversationsAppHostTopologyTest
{
    /// <summary>
    /// Asserts the non-shipping boundary from the values MSBuild actually evaluates.
    /// </summary>
    /// <remarks>
    /// Reading the raw <c>&lt;IsPackable&gt;</c> and <c>&lt;IsPublishable&gt;</c> elements out of the project XML
    /// is a shape check, not a mechanical one: an imported <c>.props</c> or <c>.targets</c> could set either
    /// property to <c>true</c> with the project XML unchanged, and the assertion would stay green while the
    /// harness became packable. Evaluating the properties measures what the build does.
    /// </remarks>
    [Fact]
    public async Task ConversationsAppHostShouldBeMechanicallyNonShipping()
    {
        IReadOnlyDictionary<string, string> evaluated = await EvaluateProjectPropertiesAsync(
            Path.Combine(
                FindRepositoryRoot(),
                "src",
                "Hexalith.Conversations.AppHost",
                "Hexalith.Conversations.AppHost.csproj"),
            "IsPackable",
            "IsPublishable");

        evaluated.Keys.Order(StringComparer.Ordinal).ShouldBe(["IsPackable", "IsPublishable"]);
        evaluated["IsPackable"].ShouldBe("false", StringCompareShould.IgnoreCase);
        evaluated["IsPublishable"].ShouldBe("false", StringCompareShould.IgnoreCase);
    }

    /// <summary>
    /// Evaluates MSBuild properties for one project and returns the values the build would use.
    /// </summary>
    /// <param name="projectPath">The absolute project path to evaluate.</param>
    /// <param name="propertyNames">The properties to read.</param>
    /// <returns>The evaluated property values keyed by property name.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when evaluation cannot run or does not return every requested property. Failing here rather than
    /// skipping is deliberate: an unevaluated guard proves nothing about the shipping boundary.
    /// </exception>
    private static async Task<IReadOnlyDictionary<string, string>> EvaluateProjectPropertiesAsync(
        string projectPath,
        params string[] propertyNames)
    {
        if (propertyNames.Length < 2)
        {
            throw new ArgumentException(
                "Evaluate at least two properties so MSBuild prints the JSON envelope rather than a bare value.",
                nameof(propertyNames));
        }

        ProcessStartInfo startInfo = new("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = FindRepositoryRoot(),
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        foreach (string propertyName in propertyNames)
        {
            startInfo.ArgumentList.Add($"-getProperty:{propertyName}");
        }

        startInfo.ArgumentList.Add("-nologo");

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("MSBuild evaluation could not be started.");
        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException("MSBuild evaluation did not complete within 180 seconds.");
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"MSBuild evaluation failed with exit code {process.ExitCode}.{Environment.NewLine}{standardError}");
        }

        // A single -getProperty prints the bare value; several print a JSON envelope (enforced above).
        using JsonDocument document = ParseMsBuildJsonEnvelope(standardOutput);
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        JsonElement properties = document.RootElement.GetProperty("Properties");
        foreach (string propertyName in propertyNames)
        {
            if (!properties.TryGetProperty(propertyName, out JsonElement value))
            {
                throw new InvalidOperationException(
                    $"MSBuild evaluation did not return the '{propertyName}' property.");
            }

            values[propertyName] = value.GetString() ?? string.Empty;
        }

        return values;
    }

    /// <summary>
    /// Extracts the MSBuild JSON envelope from process output that may carry noise on either side.
    /// Taking the first <c>{</c> and parsing to end-of-stream failed whenever a preceding SDK resolver or
    /// NuGet notice contained a brace, and whenever any line followed the envelope (an audit warning or a
    /// restore summary), replacing the intended diagnostic with an opaque parse error (pass-10 review).
    /// </summary>
    /// <param name="output">The raw process standard output.</param>
    /// <returns>The parsed JSON envelope.</returns>
    private static JsonDocument ParseMsBuildJsonEnvelope(string output)
    {
        int lastBrace = output.LastIndexOf('}');

        for (int start = output.IndexOf('{'); start >= 0 && start < lastBrace; start = output.IndexOf('{', start + 1))
        {
            try
            {
                return JsonDocument.Parse(output[start..(lastBrace + 1)]);
            }
            catch (JsonException)
            {
                // A brace inside a preceding notice is not the envelope; try the next candidate.
            }
        }

        throw new InvalidOperationException(
            $"MSBuild evaluation printed no parseable JSON envelope.{Environment.NewLine}{output}");
    }

    [Fact]
    public void ConversationsAppHostShouldExposeStableResourceNames()
    {
        ConversationsAppHostTopology.EventStoreResourceName.ShouldBe("eventstore");
        ConversationsAppHostTopology.ConversationsResourceName.ShouldBe("conversations");
        ConversationsAppHostTopology.ConversationsDaprAppId.ShouldBe("conversation");
        ConversationsAppHostTopology.AdminWebResourceName.ShouldBe("conversations-admin-web");
        ConversationsAppHostTopology.StateStoreComponentName.ShouldBe("statestore");
        ConversationsAppHostTopology.PubSubComponentName.ShouldBe("pubsub");
    }

    [Fact]
    public void ConversationsAppHostShouldModelEventStoreServerAdminAndSharedDaprResources()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.EventStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        resources.ConversationsServer.Resource.Name.ShouldBe(ConversationsAppHostTopology.ConversationsResourceName);
        resources.AdminWeb.Resource.Name.ShouldBe(ConversationsAppHostTopology.AdminWebResourceName);
        resources.StateStore.Resource.Name.ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        resources.PubSub.Resource.Name.ShouldBe(ConversationsAppHostTopology.PubSubComponentName);
        resources.Security.ShouldNotBeNull();
        resources.Security!.Keycloak.Resource.Name.ShouldBe(HexalithEventStoreSecurityOptions.DefaultResourceName);

        string[] projectNames = [.. builder.Resources.OfType<ProjectResource>().Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        projectNames.ShouldBe(
        [
            ConversationsAppHostTopology.ConversationsResourceName,
            ConversationsAppHostTopology.AdminWebResourceName,
            ConversationsAppHostTopology.EventStoreResourceName,
        ]);

        string[] componentNames = [.. builder.Resources.OfType<IDaprComponentResource>().Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        componentNames.ShouldBe(
        [
            ConversationsAppHostTopology.PubSubComponentName,
            ConversationsAppHostTopology.StateStoreComponentName,
        ]);

        string[] resourceNames = [.. builder.Resources.Select(static resource => resource.Name).Order(StringComparer.Ordinal)];
        resourceNames.ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    [Fact]
    public async Task ConversationsServerShouldUseSharedDaprSidecarAndWaitForEventStore()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        DaprSidecarOptions options = GetSidecarOptions(resources.ConversationsServer.Resource);
        options.AppId.ShouldBe(ConversationsAppHostTopology.ConversationsDaprAppId);
        options.EnableAppHealthCheck.ShouldBe(true);
        options.AppHealthCheckPath.ShouldBe("/alive");
        resources.ConversationsServer.Resource.Annotations
            .OfType<EndpointAnnotation>()
            .Select(static endpoint => endpoint.Name)
            .ShouldContain("http");

        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext environmentContext = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resources.ConversationsServer.Resource,
            environment,
            TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resources.ConversationsServer.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(environmentContext);
        }

        environment["EventStore__DomainService__AppId"].ShouldBe(ConversationsAppHostTopology.ConversationsDaprAppId);
        environment["EventStore__DomainService__ServiceVersion"].ShouldBe("v1");
        environment["EventStore__CommandStatus__StateStoreName"].ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        environment["EventStore__DataProtection__StateStoreName"].ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        environment["EventStore__DataProtection__PersistToStateStore"].ShouldBe("true");
        environment["EventStore__Projections__ReadModelStateStoreName"].ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        environment["EventStore__Projections__CheckpointStateStoreName"].ShouldBe(ConversationsAppHostTopology.StateStoreComponentName);
        environment["EventStore__ProjectionChanges__PubSubName"].ShouldBe(ConversationsAppHostTopology.PubSubComponentName);
        environment["Tenants__PubSubName"].ShouldBe(ConversationsAppHostTopology.PubSubComponentName);
        environment["Tenants__TopicName"].ShouldBe("tenants.events");
        environment["Dapr__DeclarativeSubscriptions"].ShouldBe("true");

        // The enrolled tenant access reads Tenants history through the gateway with its own worker authority; the
        // host refuses to start without these values.
        EndpointReference gateway = environment[ConversationsAppHostTopology.TenantAccessGatewayBaseAddressVariable]
            .ShouldBeOfType<EndpointReference>();
        gateway.Resource.Name.ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        gateway.EndpointName.ShouldBe("http");
        environment["EventStore__Authentication__Authority"].ShouldBeOfType<ReferenceExpression>();
        environment["EventStore__Authentication__TokenEndpoint"].ShouldBeOfType<ReferenceExpression>()
            .Format.ShouldEndWith("/protocol/openid-connect/token");
        environment["EventStore__Authentication__GrantType"].ShouldBe("client_credentials");
        environment["EventStore__Authentication__ClientId"].ShouldBe(ConversationsAppHostTopology.TenantAccessWorkerClientId);
        ParameterResource secret = environment["EventStore__Authentication__ClientSecret"].ShouldBeOfType<ParameterResource>();
        secret.Secret.ShouldBeTrue();
        secret.Name.ShouldBe("conversations-tenant-access-client-secret");
        Dictionary<string, object> keycloak = await EnvironmentOfAsync(resources.Security.ShouldNotBeNull().Keycloak.Resource);
        keycloak["CONVERSATIONS_TENANT_ACCESS_CLIENT_SECRET"].ShouldBeSameAs(secret);

        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.StateStoreComponentName);
        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.PubSubComponentName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    /// <summary>The local realm declares the dedicated tenant-access worker client without a literal secret.</summary>
    [Fact]
    public void RealmShouldDeclareTheTenantAccessWorkerClientWithAnInjectedSecret()
    {
        string realm = Path.Combine(FindRepositoryRoot(), "src", "Hexalith.Conversations.AppHost", "KeycloakRealms", "hexalith-realm.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(realm));

        JsonElement client = document.RootElement.GetProperty("clients").EnumerateArray()
            .Single(static element => element.GetProperty("clientId").GetString() == ConversationsAppHostTopology.TenantAccessWorkerClientId);

        client.GetProperty("publicClient").GetBoolean().ShouldBeFalse();
        client.GetProperty("serviceAccountsEnabled").GetBoolean().ShouldBeTrue();
        client.GetProperty("directAccessGrantsEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("standardFlowEnabled").GetBoolean().ShouldBeFalse();
        client.GetProperty("secret").GetString().ShouldBe("${CONVERSATIONS_TENANT_ACCESS_CLIENT_SECRET}");

        // EventStore's platform-scope stream read (X-Hexalith-Scope: platform:tenants): the gateway audience plus
        // exactly one tenants:read permission and no tenant grant, because the reserved system tenant is never a grant.
        JsonElement[] mappers = [.. client.GetProperty("protocolMappers").EnumerateArray()];
        JsonElement audience = mappers.Single(static mapper => mapper.GetProperty("protocolMapper").GetString() == "oidc-audience-mapper");
        audience.GetProperty("config").GetProperty("included.client.audience").GetString().ShouldBe(HexalithEventStoreSecurityOptions.DefaultAudience);
        audience.GetProperty("config").GetProperty("access.token.claim").GetString().ShouldBe("true");
        Dictionary<string, string?> claims = mappers
            .Where(static mapper => mapper.GetProperty("protocolMapper").GetString() == "oidc-hardcoded-claim-mapper")
            .ToDictionary(
                static mapper => mapper.GetProperty("config").GetProperty("claim.name").GetString()!,
                static mapper => mapper.GetProperty("config").GetProperty("claim.value").GetString(),
                StringComparer.Ordinal);
        claims.ShouldBe(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["eventstore:permission"] = "tenants:read",
        });
    }

    /// <summary>
    /// With Keycloak disabled, the host still receives a complete development-only worker authority so it starts;
    /// the loopback authority issues nothing, so every tenant-history read fails and denies.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task KeycloakDisabledTopologyInjectsADevelopmentOnlyWorkerAuthority()
    {
        IDistributedApplicationBuilder builder = CreateBuilder(enableKeycloak: false);

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);
        Dictionary<string, object> environment = await EnvironmentOfAsync(resources.ConversationsServer.Resource);

        environment[ConversationsAppHostTopology.TenantAccessGatewayBaseAddressVariable].ShouldBeOfType<EndpointReference>()
            .Resource.Name.ShouldBe(ConversationsAppHostTopology.EventStoreResourceName);
        environment["EventStore__Authentication__Authority"].ShouldBe(ConversationsAppHostTopology.DevelopmentWorkerAuthority);
        environment["EventStore__Authentication__TokenEndpoint"].ShouldBe(ConversationsAppHostTopology.DevelopmentWorkerAuthority + "/token");
        new Uri(ConversationsAppHostTopology.DevelopmentWorkerAuthority).IsLoopback.ShouldBeTrue();
        environment["EventStore__Authentication__GrantType"].ShouldBe("client_credentials");
        environment["EventStore__Authentication__ClientId"].ShouldBe(ConversationsAppHostTopology.TenantAccessWorkerClientId);
        ParameterResource secret = environment["EventStore__Authentication__ClientSecret"].ShouldBeOfType<ParameterResource>();
        secret.Secret.ShouldBeTrue();
        secret.Name.ShouldBe("conversations-tenant-access-development-client-secret");
    }

    /// <summary>
    /// Outside run mode the Keycloak-disabled topology injects no worker authority and no development secret, so a
    /// published Conversations host never starts on the development-only loopback authority.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    public async Task KeycloakDisabledPublishTopologyInjectsNoWorkerAuthority()
    {
        IDistributedApplicationBuilder builder = CreateBuilder(enableKeycloak: false, args: ["--operation", "publish"]);
        builder.ExecutionContext.IsPublishMode.ShouldBeTrue();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);
        Dictionary<string, object> environment = await EnvironmentOfAsync(resources.ConversationsServer.Resource);

        environment.ShouldContainKey(ConversationsAppHostTopology.TenantAccessGatewayBaseAddressVariable);
        environment.Keys.ShouldNotContain(key => key.StartsWith("EventStore__Authentication__", StringComparison.Ordinal));
        builder.Resources.OfType<ParameterResource>()
            .ShouldNotContain(parameter => parameter.Name == "conversations-tenant-access-development-client-secret");
    }

    [Fact]
    public void EventStoreShouldUseSharedSecurityWhenKeycloakIsEnabled()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.Security.ShouldNotBeNull();
        ResourceNamesReferencedBy(resources.EventStore.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.EventStore.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    [Fact]
    public void AdminWebShouldReferenceAndWaitForConversationsServer()
    {
        IDistributedApplicationBuilder builder = CreateBuilder();

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        ResourceNamesReferencedBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);
        ResourceNamesWaitedOnBy(resources.AdminWeb.Resource).ShouldContain(ConversationsAppHostTopology.ConversationsResourceName);

        EndpointAnnotation endpoint = resources.AdminWeb.Resource.Annotations.OfType<EndpointAnnotation>().ShouldHaveSingleItem();
        endpoint.Name.ShouldBe("http");
        endpoint.UriScheme.ShouldBe("http");
        endpoint.Port.ShouldBeNull();
        endpoint.TargetPort.ShouldBeNull();
        endpoint.IsProxied.ShouldBeTrue();
    }

    [Fact]
    public void AddConversationsShouldFailClosedAgainstNullBuilder()
        => Should.Throw<ArgumentNullException>(() => ConversationsAppHostTopology.AddConversations(null!));

    [Fact]
    public void AddConversationsShouldOmitSecurityWhenKeycloakIsDisabled()
    {
        IDistributedApplicationBuilder builder = CreateBuilder(enableKeycloak: false);

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);

        resources.Security.ShouldBeNull();
        builder.Resources.Select(static resource => resource.Name).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesReferencedBy(resources.EventStore.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.EventStore.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldNotContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
    }

    /// <summary>Package-mode testing uses the existing owner host without rebuilding it during launch.</summary>
    [Fact]
    public void TestingGatewayShouldUsePrebuiltBuildsOwnedProject()
    {
        IDistributedApplicationBuilder builder = CreateBuilder(enableKeycloak: false);
        _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ConversationsAppHostTopology.TestingUseBuildsGatewayConfigurationKey] = "true",
        });

        ConversationsAppHostResources resources = ConversationsAppHostTopology.AddConversations(builder);
        IProjectMetadata metadata = resources.EventStore.Resource.GetProjectMetadata();
        string? buildsRoot = typeof(ConversationsAppHostTopology).Assembly
            .GetCustomAttributes<System.Reflection.AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "EnrolledBuildsRoot").Value;
        buildsRoot.ShouldNotBeNullOrWhiteSpace();
        metadata.ProjectPath.ShouldBe(Path.Combine(buildsRoot!, "src", "hosts", "Hexalith.Builds.Module.EventStoreHost", "Hexalith.Builds.Module.EventStoreHost.csproj"));
        metadata.SuppressBuild.ShouldBeTrue();
        EndpointAnnotation endpoint = resources.EventStore.Resource.Annotations.OfType<EndpointAnnotation>().ShouldHaveSingleItem();
        endpoint.Name.ShouldBe("http");
        endpoint.UriScheme.ShouldBe("http");
    }

    private static IDistributedApplicationBuilder CreateBuilder(bool enableKeycloak = true, string[]? args = null)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args ?? []);
        if (!enableKeycloak)
        {
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [HexalithEventStoreSecurityOptions.DefaultEnableKeycloakConfigurationKey] = "false",
            });
        }

        return builder;
    }

    private static DaprSidecarOptions GetSidecarOptions(ProjectResource resource)
        => resource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .SelectMany(static sidecar => sidecar.Sidecar.Annotations.OfType<DaprSidecarOptionsAnnotation>())
            .Select(static annotation => annotation.Options)
            .Single();

    private static async Task<Dictionary<string, object>> EnvironmentOfAsync(IResource resource)
    {
        Dictionary<string, object> environment = new(StringComparer.Ordinal);
        EnvironmentCallbackContext context = new(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource,
            environment,
            TestContext.Current.CancellationToken);
        foreach (EnvironmentCallbackAnnotation annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
        {
            await annotation.Callback(context);
        }

        return environment;
    }

    private static string[] ResourceNamesReferencedBySidecar(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<DaprSidecarAnnotation>()
            .SelectMany(static annotation => annotation.Sidecar.Annotations.OfType<DaprComponentReferenceAnnotation>())
            .Select(static annotation => annotation.Component.Name)
            .Order(StringComparer.Ordinal)];

    private static string[] ResourceNamesReferencedBy(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<ResourceRelationshipAnnotation>()
            .Select(static annotation => annotation.Resource.Name)
            .Order(StringComparer.Ordinal)];

    private static string[] ResourceNamesWaitedOnBy(ProjectResource resource)
        => [.. resource.Annotations
            .OfType<WaitAnnotation>()
            .Select(static annotation => annotation.Resource.Name)
            .Order(StringComparer.Ordinal)];

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }
}
