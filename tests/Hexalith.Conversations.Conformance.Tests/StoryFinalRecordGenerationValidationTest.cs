// <copyright file="StoryFinalRecordGenerationValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Text;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 6.8 — proves the final-record generator cannot be silently removed from a completion workflow.
/// </summary>
/// <remarks>
/// Ordering alone only proves a heading sits in the right place: it passes just as happily when the gate
/// body has been replaced with "the record is optional". So each surface is bounded to its own gate span,
/// the enforcement language and its order are asserted inside that span, the completion transition is
/// bound to the record gate, and mutations are asserted to fail. A guard that cannot be demonstrated
/// failing has not been demonstrated.
/// </remarks>
public sealed class StoryFinalRecordGenerationValidationTest
{
    private const string Generator = "_bmad/scripts/generate_story_record.py";
    private const string GeneratorInvocation = "generate_story_record.py";
    private const string StoryPath =
        "_bmad-output/implementation-artifacts/6-8-generate-the-final-story-record-mechanically-from-measured-state.md";

    private const string ClaudeTree = ".claude/skills";
    private const string AgentTree = ".agents/skills";

    /// <summary>
    /// Per gated surface: the structural gate marker, the marker that bounds its span, the enforcement
    /// clauses in required execution order, and the success dependency that must follow the gate. The
    /// follower marker is load-bearing — without it the span runs to the end of the file and a displaced
    /// clause would count as "inside the gate".
    /// </summary>
    private static readonly (string Path, string GateMarker, string FollowerMarker, string[] OrderedClauses, string SuccessClause)[] Contracts =
    [
        (
            "bmad-build/step-05-present.md",
            "### Final Record Generation Gate",
            "### Mark Spec Done",
            [
                "-t:Rebuild",
                "SourceRevisionId",
                "--candidate {candidate_revision}",
                "--format bundle",
                "TEST_BUILD_NOT_BOUND",
                "RECORD_NOT_DERIVED",
                "in-progress",
                "Never write `done`",
                "HALT",
                "VERBATIM",
                "markdown_sha256",
                "--verify-record-sha256",
                "RECORD_CONTENT_DRIFT",
                "HALTs",
            ],
            "Change `{spec_file}` status to `done`"),
        (
            "bmad-build/step-oneshot.md",
            "### Final Record Generation Gate",
            "### Finalize Spec",
            [
                "-t:Rebuild",
                "SourceRevisionId",
                "--candidate {candidate_revision}",
                "--format bundle",
                "TEST_BUILD_NOT_BOUND",
                "RECORD_NOT_DERIVED",
                "in-progress",
                "Never write `done`",
                "HALT",
                "VERBATIM",
                "markdown_sha256",
                "--verify-record-sha256",
                "RECORD_CONTENT_DRIFT",
                "HALTs",
            ],
            "Set `status: 'done'`"),
        (
            "bmad-build-auto/step-04-review.md",
            "### Final Record Generation Gate",
            "## Finalize",
            [
                "-t:Rebuild",
                "SourceRevisionId",
                "--candidate {candidate_revision}",
                "--format bundle",
                "TEST_BUILD_NOT_BOUND",
                "RECORD_NOT_DERIVED",
                "in-progress",
                "Never write `done`",
                "HALT",
                "VERBATIM",
                "markdown_sha256",
                "--verify-record-sha256",
                "RECORD_CONTENT_DRIFT",
                "HALTs",
            ],
            "write `status: done`"),
        (
            "bmad-code-review/steps/step-04-present.md",
            "#### Final record generation gate",
            "#### Determine new status based on review outcome",
            [
                "-t:Rebuild",
                "SourceRevisionId",
                "--candidate {candidate_revision}",
                "--format bundle",
                "TEST_BUILD_NOT_BOUND",
                "RECORD_NOT_DERIVED",
                "sets `record_gate_failed`",
                "never write or synchronize `done`",
                "HALT",
                "VERBATIM",
                "markdown_sha256",
                "--verify-record-sha256",
                "RECORD_CONTENT_DRIFT",
            ],
            "`record_gate_failed` is not true"),
    ];

    /// <summary>
    /// The generator the four surfaces invoke must exist and expose the flags they pass it.
    /// </summary>
    [Fact]
    public void GeneratorMustExistAndExposeTheContractedInvocationSurface()
    {
        string generator = ReadRepositoryFile(Generator);

        generator.ShouldContain("\"story-final-record-v1\"", Case.Sensitive, "The document schema is the record's contract.");
        generator.ShouldContain("TEST_BUILD_NOT_BOUND", Case.Sensitive, "The generator must reject test binaries not built from the candidate.");
        foreach (string flag in new[] { "--story", "--baseline", "--candidate", "--test-results", "--submodule", "--require-remote", "--historical", "--verify-record-sha256" })
        {
            generator.ShouldContain($"\"{flag}\"", Case.Sensitive, $"The generator must accept {flag}.");
        }

    }

    /// <summary>
    /// Every completion surface invokes the generator, in both published skill trees, with its
    /// enforcement language inside its own bounded gate span.
    /// </summary>
    [Fact]
    public void EveryCompletionSurfaceMustInvokeTheGeneratorAndEnforceItsBlockers()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            foreach (string tree in new[] { ClaudeTree, AgentTree })
            {
                string content = ReadRepositoryFile($"{tree}/{path}");
                GateContractViolations(content, gateMarker, followerMarker, orderedClauses, successClause)
                    .ShouldBeEmpty($"{tree}/{path} must invoke the final-record generator and enforce its blockers.");
                content.ShouldContain(GeneratorInvocation, Case.Sensitive, $"{tree}/{path}");
                content.ShouldContain("candidate_revision", Case.Sensitive, $"{tree}/{path} must capture an immutable candidate.");
                content.ShouldContain("explicit authorization", Case.Sensitive, $"{tree}/{path} must require commit authorization.");
                content.ShouldNotContain("--candidate HEAD", Case.Sensitive, $"{tree}/{path} must not re-resolve a moving candidate.");
                TerminalFlowViolations(content, path).ShouldBeEmpty($"{tree}/{path} must fail closed on every authorization branch.");
            }
        }
    }

    /// <summary>
    /// Review patches must not be committed merely because the user chose to apply them.
    /// </summary>
    [Fact]
    public void CodeReviewMustRequireExplicitCommitAuthorizationBeforeGating()
    {
        foreach (string tree in new[] { ClaudeTree, AgentTree })
        {
            string content = ReadRepositoryFile($"{tree}/bmad-code-review/steps/step-04-present.md");

            content.ShouldContain("#### Prepare committed review candidate", Case.Sensitive);
            content.ShouldContain("explicit authorization", Case.Sensitive);
            content.ShouldContain("choosing \"Apply every patch\" did not itself authorize a commit", Case.Sensitive);
            content.ShouldContain("{candidate_revision}", Case.Sensitive);
            content.ShouldContain("Authorization absent or declined is terminal for this run: HALT immediately", Case.Sensitive);
            content.ShouldContain("HALT immediately without falling through to status determination", Case.Sensitive);

            string missingCandidateTerminal = ReplaceFirst(content, "Authorization absent or declined is terminal for this run: HALT immediately", "Authorization absent or declined may continue");
            TerminalFlowViolations(missingCandidateTerminal, "bmad-code-review/steps/step-04-present.md")
                .ShouldContain("candidate denial can fall through");
            string missingCompletionTerminal = ReplaceFirst(content, "HALT immediately without falling through to status determination", "continue to status determination");
            TerminalFlowViolations(missingCompletionTerminal, "bmad-code-review/steps/step-04-present.md")
                .ShouldContain("completion denial can fall through");
        }
    }

    /// <summary>
    /// Removing the gate heading must be detected. This is the mutation that deletes the invocation.
    /// </summary>
    [Fact]
    public void RemovingTheGateHeadingMustBeDetected()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            string content = ReadRepositoryFile($"{AgentTree}/{path}");
            string mutated = ReplaceFirst(content, gateMarker, "### Removed Gate");

            GateContractViolations(mutated, gateMarker, followerMarker, orderedClauses, successClause)
                .ShouldContain($"missing marker: {gateMarker}", $"{path}: removing the gate heading was not detected.");
        }
    }

    /// <summary>
    /// Keeping the heading while gutting the body must be detected — the failure ordering alone cannot see.
    /// </summary>
    [Fact]
    public void GuttingTheGateBodyWhileKeepingTheHeadingMustBeDetected()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            string content = ReadRepositoryFile($"{AgentTree}/{path}");

            foreach (string clause in orderedClauses)
            {
                string gutted = content.Replace(clause, "the record is optional", StringComparison.Ordinal);
                gutted.ShouldNotContain(clause, Case.Sensitive, $"{path}: fixture clause was not fully removed.");

                GateContractViolations(gutted, gateMarker, followerMarker, orderedClauses, successClause)
                    .ShouldContain($"missing enforcement clause: {clause}", $"{path}: gutting {clause} was not detected.");
            }
        }
    }

    /// <summary>
    /// Matching prose elsewhere in the file must not satisfy the contract; the span is what makes the
    /// enforcement binding rather than merely present.
    /// </summary>
    [Fact]
    public void AnEnforcementClauseDisplacedOutsideTheGateSpanMustNotSatisfyTheContract()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            string content = ReadRepositoryFile($"{AgentTree}/{path}");
            (int start, int end) = GateSpan(content, gateMarker, followerMarker);
            start.ShouldBeGreaterThanOrEqualTo(0, $"{path}: the gate span must resolve.");

            foreach (string clause in orderedClauses)
            {
                string span = content[start..end];
                span.ShouldContain(clause, Case.Sensitive, $"{path}: fixture clause is outside the gate span.");

                string mutatedSpan = span.Replace(clause, "the record is optional", StringComparison.Ordinal);
                string mutated = string.Concat(content.AsSpan(0, start), mutatedSpan, content.AsSpan(end), $"\n{clause}\n");

                GateContractViolations(mutated, gateMarker, followerMarker, orderedClauses, successClause)
                    .ShouldContain($"missing enforcement clause: {clause}", $"{path}: a displaced {clause} satisfied the contract.");
            }
        }
    }

    /// <summary>
    /// Presence is insufficient: moving verification before insertion must fail the contract.
    /// </summary>
    [Fact]
    public void ReorderingEnforcementClausesMustBeDetected()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            string content = ReadRepositoryFile($"{AgentTree}/{path}");
            (int start, int end) = GateSpan(content, gateMarker, followerMarker);
            string span = content[start..end];
            const string placeholder = "__FINAL_RECORD_ORDER_MUTATION__";
            const string insertionClause = "VERBATIM";
            const string verificationClause = "--verify-record-sha256";
            string mutatedSpan = ReplaceFirst(span, insertionClause, placeholder);
            mutatedSpan = ReplaceFirst(mutatedSpan, verificationClause, insertionClause);
            mutatedSpan = ReplaceFirst(mutatedSpan, placeholder, verificationClause);
            string mutated = string.Concat(content.AsSpan(0, start), mutatedSpan, content.AsSpan(end));

            List<string> violations = GateContractViolations(
                mutated,
                gateMarker,
                followerMarker,
                orderedClauses,
                successClause);
            violations.ShouldContain(
                static violation => violation.StartsWith("out-of-order enforcement clause:", StringComparison.Ordinal),
                $"{path}: digest verification moved before record insertion without detection.");

            string completionMarker = CompletionAuthorizationMarker(path);
            string statusMarker = StatusMutationMarker(path);
            string orderMutation = ReplaceFirst(content, completionMarker, "__COMPLETION_AUTH_ORDER_MUTATION__");
            orderMutation = ReplaceFirst(orderMutation, statusMarker, completionMarker);
            orderMutation = ReplaceFirst(orderMutation, "__COMPLETION_AUTH_ORDER_MUTATION__", statusMarker);
            TerminalFlowViolations(orderMutation, path).ShouldContain(
                static violation => violation.StartsWith("out-of-order terminal marker:", StringComparison.Ordinal),
                $"{path}: lifecycle status moved before completion authorization without detection.");
        }
    }

    /// <summary>
    /// A surviving gate body cannot protect completion when the success transition stops depending on it.
    /// </summary>
    [Fact]
    public void RemovingFinalRecordDependencyFromCompletionMustBeDetected()
    {
        foreach ((string path, string gateMarker, string followerMarker, string[] orderedClauses, string successClause) in Contracts)
        {
            string content = ReadRepositoryFile($"{AgentTree}/{path}");
            string mutated = ReplaceFirst(content, successClause, "completion ignores the final record gate");

            GateContractViolations(mutated, gateMarker, followerMarker, orderedClauses, successClause)
                .ShouldContain($"missing completion dependency: {successClause}", $"{path}: completion no longer depended on the gate.");
        }
    }

    /// <summary>
    /// The two published skill trees must stay byte-identical for every skill file this story changed —
    /// every one, not only the four gated surfaces.
    /// </summary>
    [Fact]
    public void BothSkillTreesMustStayByteIdenticalForEverySkillFileThisStoryChanged()
    {
        foreach ((string relative, _, _, _, _) in Contracts)
        {
            byte[] claude = File.ReadAllBytes(RepositoryPath($"{ClaudeTree}/{relative}"));
            byte[] agent = File.ReadAllBytes(RepositoryPath($"{AgentTree}/{relative}"));
            agent.ShouldBe(claude, $"{relative} must be byte-identical across both skill trees.");
        }
    }

    /// <summary>
    /// The story's own record must satisfy the boundary rule the generator enforces for every record.
    /// </summary>
    [Fact]
    public void TheStoryRecordMustCarryExactlyOneFileListAndNoSubmoduleInternalPath()
    {
        string story = ReadRepositoryFile(StoryPath);

        CountOccurrences(story, "\n### File List\n").ShouldBe(1, "A record has exactly one File List.");
        StoryFileList()
            .Where(static path => path.StartsWith("references/", StringComparison.Ordinal))
            .ShouldBeEmpty("A path inside a root-declared submodule belongs to that repository's own record.");
    }

    private static List<string> TerminalFlowViolations(string content, string path)
    {
        string candidateMarker = path == "bmad-code-review/steps/step-04-present.md"
            ? "#### Prepare committed review candidate"
            : "### Prepare Committed Candidate";
        string gateMarker = path == "bmad-code-review/steps/step-04-present.md"
            ? "#### Final record generation gate"
            : "### Final Record Generation Gate";
        string completionMarker = CompletionAuthorizationMarker(path);
        string statusMarker = StatusMutationMarker(path);
        string[] orderedMarkers = path == "bmad-build-auto/step-04-review.md"
            ? [candidateMarker, completionMarker, gateMarker, statusMarker]
            : [candidateMarker, gateMarker, completionMarker, statusMarker];
        var violations = new List<string>();
        int cursor = -1;
        foreach (string marker in orderedMarkers)
        {
            int index = content.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                violations.Add($"missing terminal marker: {marker}");
            }
            else if (index <= cursor)
            {
                violations.Add($"out-of-order terminal marker: {marker}");
            }

            cursor = Math.Max(cursor, index);
        }

        string candidateSpan = SectionSpan(content, candidateMarker, orderedMarkers[1]);
        foreach (string required in new[] { "authorization", "in-progress", "uncommitted", "HALT" })
        {
            if (!candidateSpan.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"candidate denial missing fail-closed clause: {required}");
            }
        }

        string completionFollower = path == "bmad-build-auto/step-04-review.md" ? gateMarker : statusMarker;
        string completionSpan = SectionSpan(content, completionMarker, completionFollower);
        foreach (string required in new[] { "separate", "exact", "in-progress", "done", "HALT" })
        {
            if (!completionSpan.Contains(required, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"completion denial missing fail-closed clause: {required}");
            }
        }

        if (path == "bmad-code-review/steps/step-04-present.md")
        {
            if (!candidateSpan.Contains("Authorization absent or declined is terminal for this run: HALT immediately", StringComparison.Ordinal))
            {
                violations.Add("candidate denial can fall through");
            }

            if (!completionSpan.Contains("HALT immediately without falling through to status determination", StringComparison.Ordinal))
            {
                violations.Add("completion denial can fall through");
            }
        }

        if (path == "bmad-build-auto/step-04-review.md")
        {
            if (!completionSpan.Contains("second, separate preauthorization", StringComparison.Ordinal)
                || !completionSpan.Contains("HALT without asking a question", StringComparison.Ordinal))
            {
                violations.Add("auto completion commit is not separately preauthorized");
            }

            string finalization = content[content.IndexOf(statusMarker, StringComparison.Ordinal)..];
            if (!finalization.Contains("completion commit unavailable", StringComparison.Ordinal)
                || !finalization.Contains("preserve `{spec_file}` and sprint lifecycle as `in-progress`", StringComparison.Ordinal))
            {
                violations.Add("auto no-vcs branch can claim completion");
            }
        }

        string terminalSpan = content[content.IndexOf(statusMarker, StringComparison.Ordinal)..];
        bool hasSeparateAuthorization = terminalSpan.Contains("separately authorized", StringComparison.Ordinal)
            || terminalSpan.Contains("separately preauthorized", StringComparison.Ordinal);
        if (!hasSeparateAuthorization
            || !terminalSpan.Contains("commit failure", StringComparison.OrdinalIgnoreCase)
            || !terminalSpan.Contains("in-progress", StringComparison.Ordinal)
            || !terminalSpan.Contains("HALT", StringComparison.Ordinal))
        {
            violations.Add("completion commit failure is not terminal");
        }

        return violations;
    }

    private static string CompletionAuthorizationMarker(string path)
        => path switch
        {
            "bmad-build-auto/step-04-review.md" => "### Require Separately Preauthorized Completion Commit",
            "bmad-code-review/steps/step-04-present.md" => "#### Authorize completion record and lifecycle mutation",
            _ => "### Authorize Completion Record and Lifecycle Mutation",
        };

    private static string StatusMutationMarker(string path)
        => path switch
        {
            "bmad-build/step-05-present.md" => "### Mark Spec Done",
            "bmad-build/step-oneshot.md" => "### Finalize Spec",
            "bmad-build-auto/step-04-review.md" => "## Finalize",
            "bmad-code-review/steps/step-04-present.md" => "#### Determine new status based on review outcome",
            _ => throw new ArgumentOutOfRangeException(nameof(path)),
        };

    private static string SectionSpan(string content, string marker, string follower)
    {
        int start = content.IndexOf(marker, StringComparison.Ordinal);
        int end = start < 0 ? -1 : content.IndexOf(follower, start + marker.Length, StringComparison.Ordinal);
        return start >= 0 && end > start ? content[start..end] : string.Empty;
    }

    private static List<string> GateContractViolations(
        string content,
        string gateMarker,
        string followerMarker,
        string[] orderedClauses,
        string successClause)
    {
        List<string> violations = [];

        int start = content.IndexOf(gateMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            violations.Add($"missing marker: {gateMarker}");
        }

        int follower = content.IndexOf(followerMarker, StringComparison.Ordinal);
        if (follower < 0)
        {
            violations.Add($"missing marker: {followerMarker}");
        }
        else if (start >= 0 && follower <= start)
        {
            violations.Add($"out-of-order marker: {followerMarker}");
        }

        (int spanStart, int spanEnd) = GateSpan(content, gateMarker, followerMarker);
        string span = spanStart >= 0 ? content[spanStart..spanEnd] : string.Empty;
        int clauseCursor = 0;
        foreach (string clause in orderedClauses)
        {
            int clauseIndex = span.IndexOf(clause, clauseCursor, StringComparison.Ordinal);
            if (clauseIndex >= 0)
            {
                clauseCursor = clauseIndex + clause.Length;
            }
            else if (span.Contains(clause, StringComparison.Ordinal))
            {
                violations.Add($"out-of-order enforcement clause: {clause}");
            }
            else
            {
                violations.Add($"missing enforcement clause: {clause}");
            }
        }

        int successIndex = follower >= 0
            ? content.IndexOf(successClause, follower, StringComparison.Ordinal)
            : -1;
        if (successIndex < 0)
        {
            violations.Add($"missing completion dependency: {successClause}");
        }

        return violations;
    }

    private static (int Start, int End) GateSpan(string content, string gateMarker, string followerMarker)
    {
        int start = content.IndexOf(gateMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return (-1, -1);
        }

        int end = content.IndexOf(followerMarker, start + gateMarker.Length, StringComparison.Ordinal);
        return end >= 0 ? (start, end) : (-1, -1);
    }

    /// <summary>
    /// Reads the story's File List the same way the generator does: enter on the heading, exit on the next
    /// heading, so the promotions and test-result sections that follow are never read as paths.
    /// </summary>
    private static List<string> StoryFileList()
    {
        string[] lines = ReadRepositoryFile(StoryPath).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        List<string> paths = [];
        bool inside = false;

        foreach (string line in lines)
        {
            if (line.Trim() == "### File List")
            {
                inside = true;
                continue;
            }

            if (!inside)
            {
                continue;
            }

            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                break;
            }

            if (!line.StartsWith("- `", StringComparison.Ordinal))
            {
                continue;
            }

            int closing = line.IndexOf('`', 3);
            if (closing > 3)
            {
                paths.Add(line[3..closing]);
            }
        }

        return paths;
    }

    private static string ReplaceFirst(string content, string value, string replacement)
    {
        int index = content.IndexOf(value, StringComparison.Ordinal);
        return index < 0
            ? content
            : string.Concat(content.AsSpan(0, index), replacement, content.AsSpan(index + value.Length));
    }

    private static int CountOccurrences(string content, string value)
    {
        int count = 0;
        int index = content.IndexOf(value, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = content.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static string ReadRepositoryFile(string relativePath)
        => Encoding.UTF8.GetString(File.ReadAllBytes(RepositoryPath(relativePath)));

    private static string RepositoryPath(string relativePath)
        => Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));

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
