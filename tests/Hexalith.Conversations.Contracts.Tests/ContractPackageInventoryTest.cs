// <copyright file="ContractPackageInventoryTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

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
    private const string CommonsVersion = "2.30.0";
    private const string PackageModeCondition = "'$(UseHexalithProjectReferences)' != 'true'";
    private const string SourceModeCondition = "'$(UseHexalithProjectReferences)' == 'true'";

    private static readonly string[] ForbiddenInventoryFragments =
    [
        "Hexalith.Conversations.Server",
        "Hexalith.Conversations.CommandApi",
        "Hexalith.Conversations.AppHost",
        "Hexalith.Conversations.Web",
        "Hexalith.EventStore",
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
        projectReferences.Length.ShouldBe(2, "both supported source layouts must remain available in Debug source mode");
        projectReferences.ShouldAllBe(reference => GetItemGroupCondition(reference) == SourceModeCondition);
        projectReferences.Select(GetDependencyCondition).ShouldBe(
            ["'$(HexalithCommonsRoot)' != ''", "'$(HexalithCommonsRoot)' == ''"],
            ignoreOrder: true);

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
    public void PackedContractsPackageShouldContainOnlyPublicContractsAndSafeMetadata()
    {
        string repositoryRoot = FindRepositoryRoot();
        string packageOutput = Path.Combine(Path.GetTempPath(), "hexalith-conversations-contracts-package-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(packageOutput);

        try
        {
            RunDotNetPack(repositoryRoot, packageOutput);

            string packagePath = Directory.GetFiles(packageOutput, "Hexalith.Conversations.Contracts.*.nupkg").Single();
            using ZipArchive package = ZipFile.OpenRead(packagePath);
            string[] entries = package.Entries.Select(e => e.FullName.Replace('\\', '/')).ToArray();

            entries.ShouldContain("README.md");
            entries.ShouldContain("lib/net10.0/Hexalith.Conversations.Contracts.dll");
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
            commonsDependency.Attribute("version")?.Value.ShouldBe(CommonsVersion);

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

    private static void RunDotNetPack(string repositoryRoot, string packageOutput)
    {
        string commonsRoot = ResolveCommonsRoot(repositoryRoot);
        string commonsProjectPath = Path.Combine(
            commonsRoot,
            "src",
            "libraries",
            CommonsSerializationPackageId,
            $"{CommonsSerializationPackageId}.csproj");
        RunDotNet(
            repositoryRoot,
            $"pack \"{commonsProjectPath}\" -c Release -o \"{packageOutput}\" "
            + $"-p:GeneratePackageOnBuild=false -p:Version={CommonsVersion} -p:PackageVersion={CommonsVersion}");

        string projectPath = Path.Combine(repositoryRoot, "src", "Hexalith.Conversations.Contracts", "Hexalith.Conversations.Contracts.csproj");
        RunDotNet(
            repositoryRoot,
            $"pack \"{projectPath}\" -c Release -o \"{packageOutput}\" "
            + $"-p:RestoreAdditionalProjectSources=\"{packageOutput}\"");
    }

    private static void RunDotNet(string repositoryRoot, string arguments)
    {
        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = repositoryRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException("Unable to start dotnet pack.");

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.ShouldBe(0, $"dotnet pack failed.{Environment.NewLine}{output}{Environment.NewLine}{error}");
    }

    private static string ResolveCommonsRoot(string repositoryRoot)
    {
        string[] candidates =
        [
            Path.Combine(repositoryRoot, "references", "Hexalith.Commons"),
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "Hexalith.Commons")),
        ];

        return candidates.Single(candidate => File.Exists(Path.Combine(
            candidate,
            "src",
            "libraries",
            CommonsSerializationPackageId,
            $"{CommonsSerializationPackageId}.csproj")));
    }

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
