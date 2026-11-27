// <copyright file="DerivedKeyPriorBinaryRollbackHarness.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Reflection;
using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Queries;
using Hexalith.Conversations.Server;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Conversations.IntegrationTests.Projections;

/// <summary>
/// Compiles and loads the pre-Story-12.3 <c>Hexalith.Conversations.Server</c> binary from Git commit
/// <see cref="PriorBaselineCommit"/> into an isolated <see cref="DerivedKeyPriorBinaryLoadContext"/>
/// so Story 12.3 can rehearse a real <c>redeploy-then-rebuild</c> rollback and cross-binary query/cursor check.
/// </summary>
internal sealed class DerivedKeyPriorBinaryRollbackHarness : IDisposable
{
    /// <summary>The committed parent revision immediately preceding Story 12.3.</summary>
    internal const string PriorBaselineCommit = "b7c12907b4833ab1ade68641346c583dfebf1b18";

    private readonly string _workDirectory;
    private readonly DerivedKeyPriorBinaryLoadContext _loadContext;

    private DerivedKeyPriorBinaryRollbackHarness(string workDirectory, DerivedKeyPriorBinaryLoadContext loadContext, Assembly serverAssembly)
    {
        _workDirectory = workDirectory;
        _loadContext = loadContext;
        ServerAssembly = serverAssembly;
    }

    /// <summary>Gets the loaded prior-baseline <c>Hexalith.Conversations.Server</c> assembly.</summary>
    internal Assembly ServerAssembly { get; }

    /// <summary>
    /// Extracts the prior-baseline <c>src/Hexalith.Conversations.Server</c> tree from Git, builds it against the
    /// test output's dependency assemblies, and loads it in an isolated <see cref="DerivedKeyPriorBinaryLoadContext"/>.
    /// </summary>
    /// <returns>An initialized rollback harness.</returns>
    internal static DerivedKeyPriorBinaryRollbackHarness LoadPriorBinary()
    {
        string repoRoot = LocateRepositoryRoot();
        string workDir = Path.Combine(Path.GetTempPath(), "story123-prior-server-" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(workDir);
        try
        {
            ExtractPriorServerSources(repoRoot, workDir);
            string projectPath = WriteTemporaryProject(workDir);
            RunProcess("dotnet", ["build", projectPath, "--configuration", "Debug", "-v", "quiet"], workDir);

            string dllPath = Path.Combine(workDir, "bin", "Debug", "net11.0", "Hexalith.Conversations.Server.dll");
            if (!File.Exists(dllPath))
            {
                throw new InvalidOperationException($"Expected prior server binary was not produced at '{dllPath}'.");
            }

            DerivedKeyPriorBinaryLoadContext loadContext = new();
            using MemoryStream assemblyStream = new(File.ReadAllBytes(dllPath));
            Assembly priorAssembly = loadContext.LoadFromStream(assemblyStream);
            return new DerivedKeyPriorBinaryRollbackHarness(workDir, loadContext, priorAssembly);
        }
        catch
        {
            TryDeleteDirectory(workDir);
            throw;
        }
    }

    /// <summary>
    /// Composes a runnable <see cref="WebApplication"/> host wired with the prior-baseline server binary.
    /// </summary>
    /// <param name="store">The read-model store instance.</param>
    /// <param name="componentConfiguration">The store configuration.</param>
    /// <param name="stateStoreName">The configured state-store name.</param>
    /// <param name="timeProvider">The wall clock.</param>
    /// <param name="dataProtectionProvider">Optional shared data-protection provider for cross-binary cursor tests.</param>
    /// <param name="allowedTenants">Authorized tenants.</param>
    /// <returns>The built prior-binary host.</returns>
    internal WebApplication ComposePriorHost(
        InMemoryReadModelStore store,
        IConfiguration componentConfiguration,
        string stateStoreName,
        TimeProvider timeProvider,
        IDataProtectionProvider? dataProtectionProvider,
        params TenantId[] allowedTenants)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(componentConfiguration);
        builder.Configuration["EventStore:DataProtection:StateStoreName"] = stateStoreName;
        builder.Configuration["EventStore:DataProtection:PersistToStateStore"] = "false";
        builder.Configuration["Hexalith:Conversations:Queries:Cursor:MaxOffset"] = "100000";

        builder.AddEventStoreDomainService(
            typeof(ConversationsAssemblyMarker).Assembly,
            ServerAssembly);

        builder.Services.AddSingleton<IReadModelStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStore>(store);
        builder.Services.AddSingleton<IReadModelBatchStagingStore>(store);
        builder.Services.AddSingleton(timeProvider);

        Type tenantAccessInterface = ServerAssembly.GetType("Hexalith.Conversations.Server.TenantAccess.IConversationTenantAccessService", throwOnError: true)!;
        Type decisionType = ServerAssembly.GetType("Hexalith.Conversations.Server.TenantAccess.ConversationTenantAccessDecision", throwOnError: true)!;
        Type denialReasonType = ServerAssembly.GetType("Hexalith.Conversations.Server.TenantAccess.ConversationTenantAccessDenialReason", throwOnError: true)!;
        object tenantAccessProxy = DerivedKeyPriorTenantAccessProxy.Create(tenantAccessInterface, decisionType, denialReasonType, allowedTenants);
        builder.Services.AddSingleton(tenantAccessInterface, tenantAccessProxy);

        Type tenantAccessExtensions = ServerAssembly.GetType(
            "Hexalith.Conversations.Server.TenantAccess.ConversationTenantAccessServiceCollectionExtensions",
            throwOnError: true)!;
        MethodInfo addTenantAccess = tenantAccessExtensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == "AddConversationTenantAccess" && m.GetParameters().Length == 1);
        _ = addTenantAccess.Invoke(null, [builder.Services]);

        Type queryExtensions = ServerAssembly.GetType(
            "Hexalith.Conversations.Server.Queries.ConversationQueryServiceCollectionExtensions",
            throwOnError: true)!;
        MethodInfo addQueries = queryExtensions
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == "AddConversationQueries"
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType == typeof(IConfiguration));
        _ = addQueries.Invoke(null, [builder.Services, builder.Configuration]);
        if (dataProtectionProvider is not null)
        {
            builder.Services.AddSingleton(dataProtectionProvider);
        }

        return builder.Build();
    }

    /// <summary>Executes <c>ConversationQueryHandler.GetAsync</c> on the prior-binary service provider.</summary>
    /// <param name="services">The prior-binary scoped service provider.</param>
    /// <param name="query">The retrieve query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The detail query result.</returns>
    internal ValueTask<ConversationDetailResult> GetAsync(
        IServiceProvider services,
        GetConversationQuery query,
        CancellationToken cancellationToken)
    {
        Type handlerType = ServerAssembly.GetType("Hexalith.Conversations.Server.Queries.ConversationQueryHandler", throwOnError: true)!;
        object handler = services.GetRequiredService(handlerType);
        MethodInfo method = handlerType.GetMethod("GetAsync", BindingFlags.Public | BindingFlags.Instance)!;
        return (ValueTask<ConversationDetailResult>)method.Invoke(handler, [query, cancellationToken])!;
    }

    /// <summary>Executes <c>ConversationQueryHandler.ListAsync</c> on the prior-binary service provider.</summary>
    /// <param name="services">The prior-binary scoped service provider.</param>
    /// <param name="query">The list query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list query result.</returns>
    internal ValueTask<ConversationListResult> ListAsync(
        IServiceProvider services,
        ListConversationsQuery query,
        CancellationToken cancellationToken)
    {
        Type handlerType = ServerAssembly.GetType("Hexalith.Conversations.Server.Queries.ConversationQueryHandler", throwOnError: true)!;
        object handler = services.GetRequiredService(handlerType);
        MethodInfo method = handlerType.GetMethod("ListAsync", BindingFlags.Public | BindingFlags.Instance)!;
        return (ValueTask<ConversationListResult>)method.Invoke(handler, [query, cancellationToken])!;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.KeepAlive(_loadContext);
        TryDeleteDirectory(_workDirectory);
    }

    private static string LocateRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Conversations.slnx"))
                && Path.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate the Hexalith.Conversations repository root for prior-binary rollback rehearsal.");
    }

    private static void ExtractPriorServerSources(string repoRoot, string workDir)
    {
        string tarPath = Path.Combine(workDir, "prior-server.tar");
        RunProcess(
            "git",
            ["-C", repoRoot, "archive", "--format=tar", "-o", tarPath, PriorBaselineCommit, "--", "src/Hexalith.Conversations.Server"],
            repoRoot);
        RunProcess("tar", ["-xf", tarPath, "-C", workDir], workDir);
        File.Delete(tarPath);
    }

    private static string WriteTemporaryProject(string workDir)
    {
        string baseDir = AppContext.BaseDirectory;
        StringBuilder references = new();
        foreach (string dll in Directory.GetFiles(baseDir, "*.dll").OrderBy(static p => p, StringComparer.Ordinal))
        {
            string fileName = Path.GetFileName(dll);
            if (string.Equals(fileName, "Hexalith.Conversations.Server.dll", StringComparison.OrdinalIgnoreCase)
                || fileName.Contains(".Tests.", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("xunit.", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("Microsoft.Testing.", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("testhost", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("Shouldly", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("NSubstitute", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string escaped = dll.Replace("&", "&amp;", StringComparison.Ordinal).Replace("\"", "&quot;", StringComparison.Ordinal);
            _ = references.AppendLine($"    <Reference Include=\"{escaped}\" Private=\"false\" />");
        }

        string csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net11.0</TargetFramework>
                <OutputType>Library</OutputType>
                <AssemblyName>Hexalith.Conversations.Server</AssemblyName>
                <RootNamespace>Hexalith.Conversations.Server</RootNamespace>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <LangVersion>latest</LangVersion>
                <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
                <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
                <GenerateAssemblyInfo>true</GenerateAssemblyInfo>
              </PropertyGroup>
              <ItemGroup>
                <Compile Include="src/Hexalith.Conversations.Server/**/*.cs" Exclude="src/Hexalith.Conversations.Server/Program.cs" />
              </ItemGroup>
              <ItemGroup>
            {references}  </ItemGroup>
            </Project>
            """;
        string projectPath = Path.Combine(workDir, "PriorServer.csproj");
        File.WriteAllText(projectPath, csproj, Encoding.UTF8);
        return projectPath;
    }

    private static void RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string envVar in new[]
                 {
                     "CustomBeforeDirectoryBuildProps",
                     "CustomAfterMicrosoftCommonProps",
                     "CustomBeforeDirectoryBuildTargets",
                     "CustomAfterDirectoryBuildTargets",
                     "AfterMicrosoftNETSdkTargets",
                     "HEXALITH_SOURCE_MAPPING_HASH",
                 })
        {
            _ = startInfo.Environment.Remove(envVar);
        }

        foreach (string arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(120)))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            _ = process.WaitForExit(TimeSpan.FromSeconds(10));
            throw new TimeoutException($"Command '{fileName} {string.Join(' ', arguments)}' timed out.");
        }

        string output = stdout.GetAwaiter().GetResult();
        string error = stderr.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{fileName} {string.Join(' ', arguments)}' exited with code {process.ExitCode}:{Environment.NewLine}{output}{Environment.NewLine}{error}");
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
