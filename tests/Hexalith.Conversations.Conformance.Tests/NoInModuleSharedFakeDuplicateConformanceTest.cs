// <copyright file="NoInModuleSharedFakeDuplicateConformanceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text.RegularExpressions;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 2.7 (FR-9) QA gap-coverage guard. The story's central AC-1 finding — that the module's test tree holds
/// ZERO in-module re-implementations of the three named shared test-support types (the shared actor-state-manager
/// fake, the shared gateway-client fake, and the shared <c>DomainResult</c> assertion helper) — was established at
/// the recorded commit by one-off greps in the Dev Agent Record, but left UNGUARDED: nothing prevented a later
/// change from re-introducing an in-module duplicate and silently re-opening the FR-9 gap the consume sweep closed.
/// </summary>
/// <remarks>
/// <para>
/// This guard codifies those greps as durable, declaration-anchored source scans over the module's own
/// <c>src/</c> and <c>tests/</c> trees (sibling platform submodules live outside both and are not scanned). Each
/// detector is anchored to real C# declaration syntax (a class base-list, or an extension-method receiver) so it
/// matches a genuine duplicate DECLARATION, never a prose mention of the type name in a register/disposition string
/// — the at-risk register legitimately names all three in its recorded findings.
/// </para>
/// <para>
/// A failure here means a duplicate of a shared test-support type was re-introduced in-module: adopt the shared
/// equivalent instead (the genuine read-model-store consume landed in Story 2.4), or, if it is genuinely a domain
/// double of a Conversations-owned interface, it would not match these substrate-interface anchors in the first place.
/// </para>
/// </remarks>
public sealed class NoInModuleSharedFakeDuplicateConformanceTest
{
    // A class declaration whose base-list implements the shared actor-state-manager interface — the signature an
    // in-module InMemoryStateManager duplicate would carry. The base-list window excludes block/paren/statement
    // delimiters so it cannot span past the class header into an unrelated body.
    private static readonly Regex ActorStateManagerImplementation =
        new(@"\bclass\s+[A-Za-z_]\w*\b[^{}();]*:[^{}();]*\bIActorStateManager\b", RegexOptions.Singleline | RegexOptions.Compiled);

    // A class NAMED *StateManager (the heuristic name an InMemoryStateManager duplicate would adopt).
    private static readonly Regex StateManagerNamedClass =
        new(@"\bclass\s+\w*StateManager\b", RegexOptions.Compiled);

    // A class declaration whose base-list implements the shared event-store gateway-client interface.
    private static readonly Regex GatewayClientImplementation =
        new(@"\bclass\s+[A-Za-z_]\w*\b[^{}();]*:[^{}();]*\bIEventStoreGatewayClient\b", RegexOptions.Singleline | RegexOptions.Compiled);

    // A class NAMED *GatewayClient (the heuristic name a FakeEventStoreGatewayClient duplicate would adopt).
    private static readonly Regex GatewayClientNamedClass =
        new(@"\bclass\s+\w*GatewayClient\b", RegexOptions.Compiled);

    // An extension method whose receiver is DomainResult — the precise signature an in-module DomainResultAssertions
    // helper would carry. Anchored to the '(this DomainResult' receiver so a prose mention never matches.
    private static readonly Regex DomainResultExtensionReceiver =
        new(@"\(\s*this\s+DomainResult\b", RegexOptions.Compiled);

    [Fact]
    public void NoInModuleActorStateManagerDuplicateShouldExist()
    {
        IReadOnlyList<string> hits = ScanModuleSource(ActorStateManagerImplementation, StateManagerNamedClass);

        hits.ShouldBeEmpty(
            "FR-9 / AC-1: no in-module InMemoryStateManager duplicate may exist (the module's tests exercise "
            + "aggregates, handlers, projections and read stores, never Dapr actor state management). Re-introduced "
            + $"duplicate(s) found at:{FormatHits(hits)}");
    }

    [Fact]
    public void NoInModuleEventStoreGatewayClientDuplicateShouldExist()
    {
        IReadOnlyList<string> hits = ScanModuleSource(GatewayClientImplementation, GatewayClientNamedClass);

        hits.ShouldBeEmpty(
            "FR-9 / AC-1: no in-module gateway-client fake may exist (the module's tests never operate at the "
            + $"gateway-transport level). Re-introduced duplicate(s) found at:{FormatHits(hits)}");
    }

    [Fact]
    public void NoInModuleDomainResultAssertionsDuplicateShouldExist()
    {
        IReadOnlyList<string> hits = ScanModuleSource(DomainResultExtensionReceiver);

        hits.ShouldBeEmpty(
            "FR-9 / AC-1: no in-module DomainResult assertion helper may exist. The aggregate tests assert "
            + "DomainResult outcomes directly (result.IsSuccess + result.Events.Single().ShouldBeOfType<TEvent>() + "
            + "rejection-field checks), which is strictly stronger than the shared coarse helpers; a re-introduced "
            + $"in-module helper risks silently weakening that. Found at:{FormatHits(hits)}");
    }

    private static IReadOnlyList<string> ScanModuleSource(params Regex[] detectors)
    {
        string root = FindRepositoryRoot();
        var hits = new List<string>();

        foreach (string moduleTree in new[] { "src", "tests" })
        {
            string treeRoot = Path.Combine(root, moduleTree);
            if (!Directory.Exists(treeRoot))
            {
                continue;
            }

            foreach (string file in EnumerateSourceFiles(treeRoot))
            {
                string text = File.ReadAllText(file);
                foreach (Regex detector in detectors)
                {
                    if (detector.IsMatch(text))
                    {
                        hits.Add($"{Path.GetRelativePath(root, file)} [{detector}]");
                        break;
                    }
                }
            }
        }

        return hits;
    }

    private static IEnumerable<string> EnumerateSourceFiles(string treeRoot)
        => Directory.EnumerateFiles(treeRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                // This guard legitimately NAMES the forbidden declaration shapes in its detectors and prose (as the
                // at-risk register does in its recorded findings); a genuine duplicate would never live here, so the
                // meta-file exempts itself to avoid matching its own pattern literals.
                && !string.Equals(Path.GetFileName(f), ThisFileName, StringComparison.Ordinal));

    private static readonly string ThisFileName = $"{nameof(NoInModuleSharedFakeDuplicateConformanceTest)}.cs";

    private static string FormatHits(IReadOnlyList<string> hits)
        => hits.Count == 0 ? string.Empty : " " + string.Join("; ", hits);

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
}
