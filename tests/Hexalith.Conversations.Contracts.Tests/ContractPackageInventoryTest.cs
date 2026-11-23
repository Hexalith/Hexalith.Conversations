// <copyright file="ContractPackageInventoryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

using Hexalith.Conversations.Contracts.Versioning;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Contracts.Tests;

/// <summary>
/// Verifies the packable contracts project publishes adopter-safe package contents.
/// </summary>
public sealed class ContractPackageInventoryTest
{
    private const string CommonsSerializationPackageId = "Hexalith.Commons.Serialization";
    private const string PackageModeCondition = "'$(UseHexalithProjectReferences)' == 'false'";
    private const string SourceModeCondition = "'$(UseHexalithProjectReferences)' == 'true'";

    private static readonly string[] ForbiddenInventoryFragments =
    [
        "Hexalith.Conversations.Server",
        "Hexalith.Conversations.CommandApi",
        "Hexalith.Conversations.AppHost",
        "Hexalith.Conversations.Web",
        "Hexalith.EventStore.Client",
        "Hexalith.EventStore.DomainService",
        "Hexalith.EventStore.Server",
        "Hexalith.Tenants",
        "Hexalith.Parties",
        "Hexalith.FrontComposer",
        "Dapr",
        "Microsoft.AspNetCore",
        "obj/",
        "tests/",
        ".Tests",
    ];

    [Fact]
    public void ContractsProjectShouldDeclareAdopterPackageMetadata()
    {
        string projectPath = Path.Combine(FindRepositoryRoot(), "src", "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Contracts.csproj");
        XDocument project = XDocument.Load(projectPath);

        string? packageId = GetProjectProperty(project, "PackageId");
        string? description = GetProjectProperty(project, "Description");
        string? tags = GetProjectProperty(project, "PackageTags");

        packageId.ShouldBe(ConversationContractCompatibility.Current.ContractsPackage.PackageId);
        description.ShouldNotBeNullOrWhiteSpace();
        tags.ShouldNotBeNullOrWhiteSpace();
        tags.ShouldContain("contracts");
        GetProjectProperty(project, "PackageVersion").ShouldBeNull("Package versions are inherited from the root build configuration.");

        XElement[] projectReferences = project.Descendants().Where(e => e.Name.LocalName == "ProjectReference").ToArray();
        projectReferences.Length.ShouldBe(2);
        XElement eventStoreReference = projectReferences.Single(reference => reference.Attribute("Include")!.Value.Contains("Hexalith.EventStore.Contracts", StringComparison.Ordinal));
        GetItemGroupCondition(eventStoreReference).ShouldBe(SourceModeCondition);
        XElement mappedReference = projectReferences.Single(reference => reference != eventStoreReference);
        GetItemGroupCondition(mappedReference).ShouldBe(SourceModeCondition);
        mappedReference.Attribute("Include")?.Value.ShouldBe(
            @"$(HexalithCommonsRoot)\src\libraries\Hexalith.Commons.Serialization\Hexalith.Commons.Serialization.csproj");
        GetDependencyCondition(mappedReference).ShouldBeNull("the tool mapping supplies the sole selected dependency");

        XElement packageReference = project.Descendants()
            .Single(e => e.Name.LocalName == "PackageReference"
                && e.Attribute("Include")?.Value == CommonsSerializationPackageId);
        GetItemGroupCondition(packageReference).ShouldBe(PackageModeCondition);
    }

    [Fact]
    public void ClientProjectShouldDeclareAlignedAdopterPackageMetadataAndSupportedClientSurface()
    {
        string projectPath = Path.Combine(FindRepositoryRoot(), "src", "Hexalith.Conversations.Client", "Hexalith.Conversations.Client.csproj");
        XDocument project = XDocument.Load(projectPath);
        string? tags = GetProjectProperty(project, "PackageTags");

        GetProjectProperty(project, "IsPackable").ShouldBe("true");
        GetProjectProperty(project, "PackageId").ShouldBe(ConversationContractCompatibility.Current.ClientPackage.PackageId);
        GetProjectProperty(project, "Description").ShouldNotBeNullOrWhiteSpace();
        tags.ShouldNotBeNullOrWhiteSpace();
        tags.ShouldContain("client");
        GetProjectProperty(project, "PackageVersion").ShouldBeNull("Package versions are inherited from the root build configuration.");
        project.Descendants().Where(e => e.Name.LocalName == "ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .ShouldContain(@"..\Hexalith.Conversations.Contracts\Hexalith.Conversations.Contracts.csproj");

        Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .ShouldBe(
                [
                    "BoundedConversationDetailResponse.cs",
                    "ClientAssemblyMarker.cs",
                    "ConversationClient.cs",
                    "ConversationClientContext.cs",
                    "ConversationClientOptions.cs",
                    "ConversationClientResult.cs",
                    "ConversationClientServiceCollectionExtensions.cs",
                    "IConversationClient.cs",
                ],
                ignoreOrder: true);
    }

    [Fact]
    public async Task PackedContractsPackageShouldContainOnlyPublicContractsAndSafeMetadata()
    {
        string repositoryRoot = FindRepositoryRoot();
        string packageOutput = Path.Combine(Path.GetTempPath(), "hexalith-conversations-contracts-package-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageOutput);

        try
        {
            string sourceDependencyVersion = await RunDotNetPackAsync(repositoryRoot, packageOutput);

            string packagePath = Directory.GetFiles(packageOutput, "Hexalith.Conversations.Contracts.*.nupkg").Single();
            using ZipArchive package = ZipFile.OpenRead(packagePath);
            string[] entries = package.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();

            entries.ShouldContain("README.md");
            entries.ShouldContain("lib/net11.0/Hexalith.Conversations.Contracts.dll");
            entries.ShouldContain(entry => entry.EndsWith(".nuspec", StringComparison.Ordinal));

            foreach (string entry in entries)
            {
                foreach (string forbidden in ForbiddenInventoryFragments)
                {
                    entry.ShouldNotContain(forbidden, Case.Insensitive);
                }
            }

            ZipArchiveEntry nuspecEntry = package.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal));
            using Stream nuspecStream = nuspecEntry.Open();
            XDocument nuspec = XDocument.Load(nuspecStream);

            XElement metadata = nuspec.Descendants().Single(e => e.Name.LocalName == "metadata");
            metadata.Elements().Single(e => e.Name.LocalName == "id").Value.ShouldBe(ConversationContractCompatibility.Current.ContractsPackage.PackageId);
            metadata.Elements().Single(e => e.Name.LocalName == "version").Value.ShouldBe(ConversationContractCompatibility.Current.ContractsPackage.Version);
            metadata.Elements().Single(e => e.Name.LocalName == "description").Value.ShouldNotBeNullOrWhiteSpace();
            metadata.Elements().Single(e => e.Name.LocalName == "license").Attribute("type")?.Value.ShouldBe("expression");
            metadata.Descendants().Single(e => e.Name.LocalName == "repository").Attribute("type")?.Value.ShouldBe("git");
            metadata.Elements().Single(e => e.Name.LocalName == "readme").Value.ShouldBe("README.md");
            XElement commonsDependency = metadata.Descendants()
                .Single(e => e.Name.LocalName == "dependency"
                    && e.Attribute("id")?.Value == CommonsSerializationPackageId);
            commonsDependency.Attribute("version")?.Value.ShouldBe(sourceDependencyVersion);

            metadata.Descendants().Single(element => element.Name.LocalName == "dependency"
                && element.Attribute("id")?.Value == "Hexalith.EventStore.Contracts").Attribute("version")?.Value.ShouldNotBeNullOrWhiteSpace();
            string nuspecText = nuspec.ToString(SaveOptions.DisableFormatting);
            foreach (string forbidden in ForbiddenInventoryFragments)
            {
                nuspecText.ShouldNotContain(forbidden, Case.Insensitive);
            }
        }
        finally
        {
            if (Directory.Exists(packageOutput))
            {
                Directory.Delete(packageOutput, recursive: true);
            }
        }
    }

    private static async Task<string> RunDotNetPackAsync(string repositoryRoot, string packageOutput)
    {
#if CONVERSATIONS_SOURCE_MAPPING_TESTS
        return await PlatformEnrollmentMappingTest.PackSourceInventoryAsync(repositoryRoot, packageOutput);
#else
        // The package/Release runner must already supply the tool-selected imports and published packages.
        string projectPath = Path.Combine(repositoryRoot, "src", "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Contracts.csproj");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await RunDotNetAsync(repositoryRoot,
            ["pack", projectPath, "-c", "Release", "-o", packageOutput, "--disable-build-servers", "-p:GeneratePackageOnBuild=false"],
            cancellationToken);
        string selectedVersion = (await RunDotNetAsync(repositoryRoot,
            ["msbuild", projectPath, "-nologo", "-nr:false", "-getProperty:HexalithCommonsVersion"],
            cancellationToken)).Trim();
        selectedVersion.ShouldNotBeNullOrWhiteSpace("the package/Release project must resolve its version from the selected catalog");
        return selectedVersion;
#endif
    }

#if !CONVERSATIONS_SOURCE_MAPPING_TESTS
    private static async Task<string> RunDotNetAsync(string repositoryRoot, string[] arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        ProcessStartInfo startInfo = new("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start dotnet {arguments[0]}.");
        Task<string> output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(timeout.Token), output, error).WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            Exception? stopFailure = null;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or TimeoutException or AggregateException)
            {
                stopFailure = exception;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException($"dotnet {arguments[0]} was cancelled.", stopFailure, cancellationToken);
            }

            throw new TimeoutException($"dotnet {arguments[0]} exceeded its three-minute budget.", stopFailure);
        }

        string standardOutput = await output;
        process.ExitCode.ShouldBe(0, standardOutput + await error);
        return standardOutput;
    }
#endif

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Conversations.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static string? GetProjectProperty(XDocument project, string propertyName)
        => project.Descendants().SingleOrDefault(e => e.Name.LocalName == propertyName)?.Value;

    private static string? GetDependencyCondition(XElement dependency)
        => dependency.Attribute("Condition")?.Value;

    private static string? GetItemGroupCondition(XElement dependency)
        => dependency.Ancestors().FirstOrDefault(e => e.Name.LocalName == "ItemGroup")?.Attribute("Condition")?.Value;
}
