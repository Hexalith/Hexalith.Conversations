using System.Text.Json;

using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.TestSupport.Runner;

/// <summary>Internal, non-shipping JSON transport for the neutral evidence helpers.</summary>
internal static class Program
{
    private const string Story102RecordSha256 = "7bca69ff979b5ead9819da913933c13efbcb6e2076368202be92f256ef39d409";
    private const string Story102CandidateCommit = "5a119ff283a8f81906a52d983e50b04b76fae0ed";

    private const string Story103RecordSha256 = "013416c7f819aafcad2528d4b6892a05707b5f3056830ee550f174048a9929d1";
    private const string Story103CandidateCommit = "6908c5264c388cca1b094400c5abfa320482c5e3";

    // Independent compiled pins for the sole approved Story 10.4 Git transition set.
    private static readonly (string Path, string Before, string After)[] _story104GitlinkTransitions =
    [
        ("references/Hexalith.Builds", "abdfca89ed82f388b54664825a8a79fed73e7ccf", "4f53d9f37c5b86cfd43fbcc8fa5dd4df19b84c77"),
        ("references/Hexalith.EventStore", "6cc9d6114fd055dac7a1d86130208b52271e3740", "77d9af18a54d92506e310fe5a45021a395e66f60"),
        ("references/Hexalith.Folders", "92e87359dd50dc9644e490a04de98adaaade9a86", "42a2308113e1bdb9b1d07a778b4405dde797ebd3"),
        ("references/Hexalith.FrontComposer", "0129bd39095892283371f97836ade5768fef3809", "b102b85b148f21fb26d5742e120c344109df7084"),
        ("references/Hexalith.McpCli", "0b96dfdf2ff6037be88f1fb952d6581a78930e67", "b35b0975a234f1f97e2e35e9dad381d0a988f4af"),
        ("references/Hexalith.Memories", "f7c8ce84a8a765667beafc31154ee2d6981552f8", "f6b60d5a2e9b81d84fddad789c7d9bed40d3a533"),
        ("references/Hexalith.Parties", "c0946ef17b3641fb23f6f17614913bfbab687373", "90af3047b50046cb4f6f91e01f182f562110aab3"),
        ("references/Hexalith.Platform", "b2890d20575356239555df5b113e3100c1d594c1", "b4b6d4a1efcd2f13c6d03270f8e952f27ed8148b"),
        ("references/Hexalith.Tenants", "e44ac7f537e9f192605e24efc3e61a97d00d44f3", "e667d5391dae306e631dec23acd191863e54130c"),
    ];

    /// <summary>Reads one request and emits one helper-derived response.</summary>
    private static async Task<int> Main()
    {
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        try
        {
            using JsonDocument request = JsonDocument.Parse(await Console.In.ReadToEndAsync().ConfigureAwait(false));
            JsonElement input = request.RootElement;
            string root = input.GetProperty("repository").GetString()!;
            GitFacts git = new(input.TryGetProperty("gitExecutable", out JsonElement executable) ? executable.GetString()! : "git");
            AssertionLedger ledger = new();
            GitFact<string> repository = await git.ResolveRepositoryAsync(root).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "repository", repository);
            GitFact<string> candidate = await git.ResolveRevisionAsync(root, input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
            GitFact<string> baseline = await git.ResolveRevisionAsync(root, input.GetProperty("baseline").GetString()!).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "candidate", candidate);
            BoundaryAssertions.HistoryAvailable(ledger, "baseline", baseline);
            GitFact<IReadOnlyList<GitDiffEntry>> diff = await git.ReadDiffAsync(root,
                baseline.Value ?? input.GetProperty("baseline").GetString()!, candidate.Value ?? input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
            BoundaryAssertions.HistoryAvailable(ledger, "diff", diff);
            if (input.TryGetProperty("expectedPaths", out JsonElement paths))
                BoundaryAssertions.ChangedPaths(ledger, "changed-paths", paths.EnumerateArray().Select(path => path.GetString()!), diff);
            if (input.TryGetProperty("story104GitlinkAmendment", out JsonElement amendment))
                Story104Gitlinks(ledger, amendment, baseline, diff);
            else
                BoundaryAssertions.NoGitlinks(ledger, "gitlinks", diff);
            List<object> blobs = [];
            if (input.TryGetProperty("blobPaths", out JsonElement blobPaths))
            {
                foreach (JsonElement path in blobPaths.EnumerateArray())
                {
                    string relative = path.GetString()!;
                    GitFact<GitBlob> blob = await git.ReadBlobAsync(root, candidate.Value ?? input.GetProperty("candidate").GetString()!, relative).ConfigureAwait(false);
                    blobs.Add(new { path = relative, code = blob.Code, sha256 = blob.Value?.Sha256,
                        content = blob.IsSuccess ? Convert.ToBase64String(blob.Value!.Content) : null });
                }
            }
            if (input.TryGetProperty("manifest", out JsonElement rows))
            {
                EvidenceManifest manifest = new();
                foreach (JsonElement row in rows.EnumerateArray())
                    manifest.Add(row.GetProperty("path").GetString()!, row.GetProperty("sha256").GetString()!, row.GetProperty("role").GetString()!);
                manifest.Verify(root, input.GetProperty("signablePayloadHash").GetString()!, ledger);
            }
            if (input.TryGetProperty("roots", out JsonElement roots))
            {
                Dictionary<string, string> constants = roots.GetProperty("constants").EnumerateObject()
                    .ToDictionary(row => row.Name, row => row.Value.GetString()!, StringComparer.Ordinal);
                if (roots.GetProperty("sourcePath").GetString() == "tests/Hexalith.Conversations.TestSupport.Runner/Program.cs")
                {
                    bool compiledPins = constants.Count == 2 && (constants.GetValueOrDefault("Story102RecordSha256") == Story102RecordSha256
                        && constants.GetValueOrDefault("Story102CandidateCommit") == Story102CandidateCommit
                        || constants.GetValueOrDefault("Story103RecordSha256") == Story103RecordSha256
                        && constants.GetValueOrDefault("Story103CandidateCommit") == Story103CandidateCommit);
                    if (!compiledPins) ledger.Block("ROOT_OF_TRUST_NOT_PINNED", "roots:compiled", "The request must retain the consuming assembly's literal pins.");
                    ledger.Record("roots:compiled", compiledPins);
                }
                GitFact<string> path = RepositoryLocator.ResolveContainedPath(root, roots.GetProperty("sourcePath").GetString()!);
                if (path.IsSuccess && File.Exists(path.Value))
                    BoundaryAssertions.PinnedRoots(ledger, "roots", await File.ReadAllTextAsync(path.Value!).ConfigureAwait(false), constants);
                else
                {
                    ledger.Block("ROOT_OF_TRUST_NOT_PINNED", "roots", path.Message ?? "The consuming source is unavailable.");
                    ledger.Record("roots", false);
                }
                BoundaryAssertions.SignedEvidenceIsNotAllowlisted(ledger, "allowlist", root,
                    roots.GetProperty("allowlist").EnumerateArray().Select(row => row.GetString()!));
            }
            List<object> rootGitlinks = [];
            if (input.TryGetProperty("includeRootGitlinks", out JsonElement includeLinks) && includeLinks.GetBoolean())
            {
                GitFact<IReadOnlyList<GitTreeEntry>> tree = await git.ReadTreeAsync(root, candidate.Value ?? input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
                BoundaryAssertions.HistoryAvailable(ledger, "roots:tree", tree);
                if (tree.IsSuccess) rootGitlinks.AddRange(tree.Value!.Where(entry => entry.Mode == "160000")
                    .Select(entry => (object)new { path = entry.Path, mode = entry.Mode, commit = entry.ObjectId }));
            }
            List<string> treePaths = [];
            List<string> headPaths = [];
            List<string> indexPaths = [];
            if (input.TryGetProperty("includeTreePaths", out JsonElement includeTree) && includeTree.GetBoolean())
            {
                GitFact<IReadOnlyList<GitTreeEntry>> tree = await git.ReadTreeAsync(root, candidate.Value ?? input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
                BoundaryAssertions.HistoryAvailable(ledger, "output:tree", tree);
                if (tree.IsSuccess) treePaths.AddRange(tree.Value!.Select(entry => entry.Path));
                GitFact<string> head = await git.ResolveRevisionAsync(root, "HEAD").ConfigureAwait(false);
                BoundaryAssertions.HistoryAvailable(ledger, "output:head", head);
                if (head.IsSuccess)
                {
                    GitFact<IReadOnlyList<GitTreeEntry>> headTree = await git.ReadTreeAsync(root, head.Value!).ConfigureAwait(false);
                    BoundaryAssertions.HistoryAvailable(ledger, "output:head-tree", headTree);
                    if (headTree.IsSuccess) headPaths.AddRange(headTree.Value!.Select(entry => entry.Path));
                }
                GitFact<GitCommandResult> index = await git.RunAsync(root, ["ls-files", "--cached", "-z", "--"]).ConfigureAwait(false);
                BoundaryAssertions.HistoryAvailable(ledger, "output:index", index);
                if (index.IsSuccess)
                {
                    bool completed = index.Value!.ExitCode == 0;
                    if (!completed) ledger.Block("HELPER_FAILURE", "output:index", "Git could not read current index ownership.");
                    ledger.Record("output:index-completed", completed);
                    if (completed)
                    {
                        string names = new System.Text.UTF8Encoding(false, true).GetString(index.Value.Stdout);
                        if (names.Length != 0 && (!names.EndsWith('\0') || names[..^1].Split('\0').Any(path =>
                                path.Length == 0 || !RepositoryLocator.ResolveHistoricalPath(root, path).IsSuccess)))
                            throw new InvalidOperationException("Malformed NUL-delimited index ownership.");
                        indexPaths.AddRange(names.Split('\0', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal));
                    }
                }
            }
            List<string> readerPaths = [];
            List<string> evidenceReaders = [];
            if (input.TryGetProperty("readerPaths", out JsonElement declaredReaders))
                readerPaths.AddRange(declaredReaders.EnumerateArray().Select(item => item.GetString()!));
            if (input.TryGetProperty("inspectChangedReaders", out JsonElement inspectReaders) && inspectReaders.GetBoolean() && diff.IsSuccess)
                readerPaths.AddRange(diff.Value!.SelectMany(entry => entry.SourcePath is null ? new[] { entry.Path } : new[] { entry.Path, entry.SourcePath })
                    .Where(path => path.StartsWith("tests/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal)
                        && !path.Contains("Hexalith.Conversations.TestSupport", StringComparison.Ordinal)));
            if (readerPaths.Count > 0)
            {
                GitFact<IReadOnlyList<GitTreeEntry>> oldTree = await git.ReadTreeAsync(root, baseline.Value ?? input.GetProperty("baseline").GetString()!).ConfigureAwait(false);
                GitFact<IReadOnlyList<GitTreeEntry>> currentTree = await git.ReadTreeAsync(root, candidate.Value ?? input.GetProperty("candidate").GetString()!).ConfigureAwait(false);
                BoundaryAssertions.HistoryAvailable(ledger, "reader:old-tree", oldTree);
                BoundaryAssertions.HistoryAvailable(ledger, "reader:current-tree", currentTree);
                foreach (string relative in readerPaths.Distinct(StringComparer.Ordinal))
                {
                    if (!oldTree.IsSuccess || !currentTree.IsSuccess)
                    {
                        evidenceReaders.Add(relative);
                        continue;
                    }
                    bool existed = oldTree.Value!.Any(entry => entry.Path == relative);
                    bool exists = currentTree.Value!.Any(entry => entry.Path == relative);
                    GitFact<GitBlob>? old = existed ? await git.ReadBlobAsync(root, baseline.Value!, relative).ConfigureAwait(false) : null;
                    GitFact<GitBlob>? current = exists ? await git.ReadBlobAsync(root, candidate.Value!, relative).ConfigureAwait(false) : null;
                    if (old is { IsSuccess: false } || current is { IsSuccess: false })
                    {
                        evidenceReaders.Add(relative);
                        foreach ((string side, GitFact<GitBlob>? blob) in new[] { ("old", old), ("current", current) })
                        {
                            if (blob is { IsSuccess: false })
                            {
                                string identity = "reader:blob:" + side + ":" + relative;
                                if (blob.Status is GitFactStatus.Unavailable or GitFactStatus.Cancelled or GitFactStatus.Timeout)
                                    ledger.Skip(identity, relative, blob.Code!, blob.Message ?? "An existing reader blob is unavailable.");
                                else
                                {
                                    ledger.Block("HELPER_FAILURE", relative, blob.Message ?? "An existing reader blob is invalid.");
                                    ledger.Record(identity, relative, false);
                                }
                            }
                        }
                        continue;
                    }
                    string oldSource = old is null ? string.Empty : System.Text.Encoding.UTF8.GetString(old.Value!.Content);
                    string source = current is null ? string.Empty : System.Text.Encoding.UTF8.GetString(current.Value!.Content);
                    IReadOnlyList<string> tokens = CSharpSourceDeclarations.UnconditionalTokens(source, includeStringSpelling: true);
                    IReadOnlyList<string> oldTokens = CSharpSourceDeclarations.UnconditionalTokens(oldSource, includeStringSpelling: true);
                    if (!IsEvidenceReader(tokens) && !IsEvidenceReader(oldTokens)) continue;
                    evidenceReaders.Add(relative);
                    bool shared = new[] { "GitFacts", "EvidenceManifest", "AssertionLedger" }.All(name =>
                        tokens.Select((token, index) => (token, index)).Any(row => row.token == name
                            && ((row.index > 0 && tokens[row.index - 1] == "new")
                                || tokens.Skip(row.index).TakeWhile(token => token != ";").Contains("new"))))
                        && CSharpSourceDeclarations.ContainsSequence(tokens, ["BoundaryAssertions", "."]);
                    if (!shared) ledger.Block("EVIDENCE_READER_NOT_SHARED", relative, "Changed evidence readers must invoke the shared neutral helpers.");
                    ledger.Record("reader:shared:" + relative, relative, shared);
                    string[] assertions = ["Equal", "True", "ChangedPaths", "NoGitlinks", "PinnedRoots", "SignedEvidenceIsNotAllowlisted", "Record", "ShouldBe", "ShouldBeTrue"];
                    IReadOnlyList<(string Method, string Identity, bool Vacuous)> calls = AssertionCalls(tokens);
                    bool strength = source.Length != 0 && assertions.All(name => tokens.Count(token => token == name) >= oldTokens.Count(token => token == name))
                        && !CSharpSourceDeclarations.ContainsSequence(tokens, ["Skip", "="])
                        && !CSharpSourceDeclarations.ContainsSequence(tokens, ["Assert", ".", "True", "(", "true", ")"])
                        && !calls.Any(call => call.Vacuous)
                        && AssertionCalls(oldTokens).All(oldCall => calls.Any(call => call.Method == oldCall.Method && call.Identity == oldCall.Identity));
                    for (int index = 0; index + 5 < oldTokens.Count; index++)
                    {
                        if (oldTokens[index] == "const" && oldTokens[index + 1] == "string")
                            strength &= CSharpSourceDeclarations.ContainsSequence(tokens, oldTokens.Skip(index).Take(6).ToArray());
                    }
                    if (!strength) ledger.Block("EVIDENCE_READER_STRENGTH_REDUCED", relative, "Assertions or pinned roots were removed or replaced by vacuous assertions.");
                    ledger.Record("reader:strength:" + relative, relative, strength);
                }
            }
            if (input.TryGetProperty("chains", out JsonElement chains))
            {
                foreach (JsonElement chain in chains.EnumerateArray())
                    BoundaryAssertions.ChainTable(ledger, chain.GetProperty("id").GetString()!, chain.GetProperty("source").GetString()!,
                        chain.GetProperty("begin").GetString()!, chain.GetProperty("end").GetString()!,
                        chain.GetProperty("rows").EnumerateArray().Select(row => row.GetString()!));
            }
            List<object> containedPaths = [];
            if (input.TryGetProperty("containedPaths", out JsonElement requestedPaths))
            {
                foreach (JsonElement item in requestedPaths.EnumerateArray())
                {
                    string relative = item.GetString()!;
                    GitFact<string> contained = RepositoryLocator.ResolveContainedPath(root, relative);
                    containedPaths.Add(new { path = relative, fullPath = contained.Value, code = contained.Code });
                    if (!contained.IsSuccess) ledger.Block("EVIDENCE_PATH_ESCAPE", relative, contained.Message!);
                    ledger.Record("containment:" + relative, relative, contained.IsSuccess);
                }
            }
            BoundaryEvaluation evaluation = ledger.Evaluate();
            string state = ledger.Rows.Any(row => row.State == "SKIPPED")
                && !ledger.Rows.Any(row => row.State == "FAIL") && !evaluation.Blockers.Any(row => row.Code == "UNAVAILABLE_HISTORY_PASSED") ? "BLOCKED" : evaluation.Result;
            Console.WriteLine(JsonSerializer.Serialize(new {
                result = state, candidate = candidate.Value, baseline = baseline.Value, diff = diff.Value,
                blobs, containedPaths, rootGitlinks, treePaths, headPaths, indexPaths, evidenceReaders, assertionLedger = ledger.Rows, blockers = evaluation.Blockers.Select(row => row.Code).Distinct().Order(StringComparer.Ordinal)
            }, options));
            return 0;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException or IOException or KeyNotFoundException)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "FAIL", blockers = new[] { "HELPER_REQUEST_INVALID" }, message = exception.Message }, options));
            return 1;
        }
    }

    /// <summary>Accepts only the complete independently pinned owner amendment at its original baseline.</summary>
    private static void Story104Gitlinks(AssertionLedger ledger, JsonElement amendment, GitFact<string> baseline,
        GitFact<IReadOnlyList<GitDiffEntry>> diff)
    {
        // Availability is established before comparing immutable pins: absence is BLOCKED,
        // while an available but different baseline remains a proven failure.
        if (!baseline.IsSuccess)
        {
            BoundaryAssertions.HistoryAvailable(ledger, "gitlinks:story104-baseline", baseline);
            return;
        }
        if (!diff.IsSuccess)
        {
            BoundaryAssertions.HistoryAvailable(ledger, "gitlinks:story104-diff", diff);
            return;
        }
        JsonElement[] declared = amendment.GetProperty("transitions").EnumerateArray().ToArray();
        bool pinned = baseline.Value == "1915a47123737859e3ac31872fa787fa8c0bd1e3"
            && amendment.GetProperty("decisionDate").GetString() == "2026-10-17"
            && amendment.GetProperty("approvalCommit").GetString() == "9cbc71b63be3fa1ce4d89d98ef1771c54c6603f5"
            && amendment.EnumerateObject().Count() == 3 && declared.Length == _story104GitlinkTransitions.Length;
        for (int index = 0; index < declared.Length && index < _story104GitlinkTransitions.Length; index++)
        {
            (string path, string before, string after) = _story104GitlinkTransitions[index];
            JsonElement row = declared[index];
            pinned &= row.EnumerateObject().Count() == 5 && row.GetProperty("path").GetString() == path
                && row.GetProperty("sourceMode").GetString() == "160000" && row.GetProperty("destinationMode").GetString() == "160000"
                && row.GetProperty("sourceObject").GetString() == before && row.GetProperty("destinationObject").GetString() == after;
        }
        ledger.Record("gitlinks:story104-pins", pinned);
        if (!pinned) ledger.Block("EVIDENCE_BOUNDARY_GITLINK", "story104-amendment", "The complete compiled owner amendment must match exactly.");
        GitDiffEntry[] links = diff.Value!.Where(entry => entry.SourceMode == "160000" || entry.DestinationMode == "160000")
            .OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray();
        // With no raw gitlink changes, preserve the neutral helper's ordinary no-gitlink path.
        // The applicable Story 10.4 consumer separately requires all nine transitions.
        if (links.Length == 0)
        {
            BoundaryAssertions.NoGitlinks(ledger, "gitlinks", diff);
            return;
        }
        bool exact = links.Length == _story104GitlinkTransitions.Length;
        for (int index = 0; index < links.Length && index < _story104GitlinkTransitions.Length; index++)
        {
            (string path, string before, string after) = _story104GitlinkTransitions[index];
            GitDiffEntry row = links[index];
            exact &= row.Path == path && row.SourcePath is null && row.Status == "M"
                && row.SourceMode == "160000" && row.DestinationMode == "160000"
                && row.SourceObject == before && row.DestinationObject == after;
        }
        ledger.Record("gitlinks:story104-transitions", exact);
        if (!exact) ledger.Block("EVIDENCE_BOUNDARY_GITLINK", "story104-transitions", "The raw Git diff must contain exactly nine approved mode-160000 transitions.");
    }

    private static bool IsEvidenceReader(IReadOnlyList<string> tokens)
    {
        string Normalize(string token) => string.Join('/', token.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries));
        if (tokens.Any(token => token == "EvidenceManifest" || Normalize(token).Contains("docs/release-evidence/", StringComparison.Ordinal))) return true;
        for (int index = 0; index + 4 < tokens.Count; index++)
        {
            if (CSharpSourceDeclarations.ContainsSequence(tokens.Skip(index).Take(4).ToArray(), ["Path", ".", "Combine", "("]))
            {
                string[] literals = tokens.Skip(index + 4).TakeWhile(token => token != ")").Where(token => token.Contains('"')).ToArray();
                if (literals.Length >= 2 && literals[0].Trim('$', '@', '"') == "docs" && literals[1].Trim('$', '@', '"') == "release-evidence") return true;
            }
        }
        return false;
    }

    private static IReadOnlyList<(string Method, string Identity, bool Vacuous)> AssertionCalls(IReadOnlyList<string> tokens)
    {
        List<(string Method, string Identity, bool Vacuous)> calls = [];
        for (int index = 0; index + 3 < tokens.Count; index++)
        {
            if (tokens[index] != "BoundaryAssertions" || tokens[index + 1] != "." || tokens[index + 3] != "(") continue;
            List<List<string>> arguments = [[]];
            int depth = 0;
            for (int cursor = index + 4; cursor < tokens.Count; cursor++)
            {
                string token = tokens[cursor];
                if (token == ")" && depth == 0) break;
                if (token == "," && depth == 0) { arguments.Add([]); continue; }
                if (token is "(" or "[" or "{") depth++;
                else if (token is ")" or "]" or "}") depth--;
                arguments[^1].Add(token);
            }
            if (arguments.Count < 2) continue;
            bool vacuous = tokens[index + 2] == "True" && arguments.Count >= 3
                && arguments[2].Where(token => token is not "(" and not ")").SequenceEqual(["true"]);
            calls.Add((tokens[index + 2], string.Join(" ", arguments[1]), vacuous));
        }
        return calls;
    }
}
