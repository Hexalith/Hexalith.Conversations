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

        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.StateStoreComponentName);
        ResourceNamesReferencedBySidecar(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.PubSubComponentName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(ConversationsAppHostTopology.EventStoreResourceName);
        ResourceNamesReferencedBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
        ResourceNamesWaitedOnBy(resources.ConversationsServer.Resource).ShouldContain(HexalithEventStoreSecurityOptions.DefaultResourceName);
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

    private static IDistributedApplicationBuilder CreateBuilder(bool enableKeycloak = true)
    {
        IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder();
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
