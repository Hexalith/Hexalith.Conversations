namespace Hexalith.Conversations.TestSupport;

/// <summary>Small neutral assertion helpers that record their evaluation.</summary>
public static class BoundaryAssertions
{
    /// <summary>Validates a single bounded marker region and its exact ordered chain-table rows.</summary>
    public static bool ChainTable(AssertionLedger ledger, string identity, string source, string begin, string end, IEnumerable<string> expectedRows)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        int first = source.IndexOf(begin, StringComparison.Ordinal);
        int last = source.IndexOf(end, StringComparison.Ordinal);
        bool markers = first >= 0 && last > first
            && source.IndexOf(begin, first + begin.Length, StringComparison.Ordinal) < 0
            && source.IndexOf(end, last + end.Length, StringComparison.Ordinal) < 0;
        if (!markers) ledger.Block("EVIDENCE_CHAIN_MARKER_INVALID", identity, "Exactly one ordered, nonnested marker pair is required.");
        ledger.Record(identity + ":markers", identity, markers);
        if (!markers) return false;
        string[] rows = source[(first + begin.Length)..last].Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(row => row.Trim()).Where(row => row.StartsWith('|')).ToArray();
        string[] expected = expectedRows.ToArray();
        bool equal = expected.Length > 0 && rows.SequenceEqual(expected, StringComparer.Ordinal)
            && rows.Distinct(StringComparer.Ordinal).Count() == rows.Length;
        if (!equal) ledger.Block("EVIDENCE_CHAIN_TABLE_INVALID", identity, "Chain rows must equal the frozen ordered inventory without duplicates.");
        ledger.Record(identity + ":rows", identity, equal);
        return equal;
    }

    /// <summary>Validates the frozen amendment identities, nonnested append order and final document boundary.</summary>
    public static bool AuthorityChain(AssertionLedger ledger, string identity, string source,
        IEnumerable<(string MarkerFamily, string Version, string Supersedes)> expectedRows)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var expected = expectedRows.ToArray();
        var markers = System.Text.RegularExpressions.Regex.Matches(source,
            @"<!-- (?<family>EPIC-6-AUTHORITY-OVERLAY[^:\s]*):(?<kind>BEGIN|END) (?<attributes>[^>]*?) -->").ToArray();
        bool bounded = true;
        string? open = null;
        foreach (var marker in markers)
        {
            string family = marker.Groups["family"].Value;
            if (marker.Groups["kind"].Value == "BEGIN")
            {
                if (open is not null) bounded = false;
                open = family;
            }
            else
            {
                if (open != family) bounded = false;
                open = null;
            }
        }
        bounded &= open is null;
        foreach (var row in expected)
        {
            int count = System.Text.RegularExpressions.Regex.Matches(source,
                "<!-- " + System.Text.RegularExpressions.Regex.Escape(row.MarkerFamily) + ":").Count;
            bounded &= count is 0 or 2;
        }
        if (markers.Length > 0)
            bounded &= source[(markers[^1].Index + markers[^1].Length)..].Trim().Length == 0;
        if (!bounded) ledger.Block("EVIDENCE_CHAIN_MARKER_INVALID", identity, "Each family requires one nonnested pair and the final END must end the document.");
        ledger.Record(identity + ":markers", identity, bounded);
        if (!bounded) return false;
        bool equal = expected.Length > 0 && expected.Select(row => row.MarkerFamily).Distinct(StringComparer.Ordinal).Count() == expected.Length
            && markers.Length == expected.Length * 2;
        for (int index = 0; index < expected.Length; index++)
        {
            var row = expected[index];
            if (index > 0) equal &= row.Supersedes == expected[index - 1].Version;
            if (index * 2 + 1 >= markers.Length) { equal = false; continue; }
            var begin = markers[index * 2];
            var end = markers[index * 2 + 1];
            string[] beginAttributes = begin.Groups["attributes"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] endAttributes = end.Groups["attributes"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            equal &= begin.Groups["family"].Value == row.MarkerFamily && end.Groups["family"].Value == row.MarkerFamily
                && begin.Groups["kind"].Value == "BEGIN" && end.Groups["kind"].Value == "END"
                && beginAttributes.Where(value => value.StartsWith("version=", StringComparison.Ordinal)).SequenceEqual(["version=" + row.Version])
                && beginAttributes.Where(value => value.StartsWith("supersedes=", StringComparison.Ordinal)).SequenceEqual(["supersedes=" + row.Supersedes])
                && endAttributes.Where(value => value.StartsWith("version=", StringComparison.Ordinal)).SequenceEqual(["version=" + row.Version]);
        }
        if (!equal) ledger.Block("EVIDENCE_CHAIN_TABLE_INVALID", identity, "The ordered markerFamily/version/supersedes identities must equal the independently frozen chain.");
        ledger.Record(identity + ":rows", identity, equal);
        return equal;
    }

    /// <summary>Records whether an expected value equals an observed value.</summary>
    public static bool Equal<T>(AssertionLedger ledger, string identity, T expected, T observed)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        bool passed = EqualityComparer<T>.Default.Equals(expected, observed);
        ledger.Record(identity, passed);
        return passed;
    }

    /// <summary>Records a predicate that the caller has evaluated.</summary>
    public static bool True(AssertionLedger ledger, string identity, bool condition)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ledger.Record(identity, condition);
        return condition;
    }

    /// <summary>Evaluates a successful Git fact, or visibly skips every non-success outcome.</summary>
    public static bool HistoryAvailable<T>(AssertionLedger ledger, string identity, GitFact<T> fact)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(fact);
        if (!fact.IsSuccess)
        {
            ledger.Skip(identity, identity, fact.Code ?? "HISTORY_UNAVAILABLE", fact.Message ?? fact.Status.ToString());
            return false;
        }
        ledger.Record(identity, true);
        return true;
    }

    /// <summary>Requires exact ordinal equality against all raw-diff paths, including rename sources.</summary>
    public static bool ChangedPaths(AssertionLedger ledger, string identity, IEnumerable<string> expected,
        GitFact<IReadOnlyList<GitDiffEntry>> diff)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(diff);
        if (!diff.IsSuccess) return HistoryAvailable(ledger, identity, diff);
        HashSet<string> declared = new(expected, StringComparer.Ordinal);
        HashSet<string> observed = new(StringComparer.Ordinal);
        foreach (GitDiffEntry entry in diff.Value!)
        {
            observed.Add(entry.Path);
            if (entry.SourcePath is not null) observed.Add(entry.SourcePath);
        }
        string[] missing = declared.Except(observed).Order(StringComparer.Ordinal).ToArray();
        string[] unexpected = observed.Except(declared).Order(StringComparer.Ordinal).ToArray();
        bool passed = missing.Length == 0 && unexpected.Length == 0;
        if (!passed) ledger.Block("CHANGED_PATH_SET_MISMATCH", identity,
            $"Missing: [{string.Join(", ", missing)}]; unexpected: [{string.Join(", ", unexpected)}].");
        ledger.Record(identity, passed);
        return passed;
    }

    /// <summary>Rejects only raw source or destination mode 160000; text and object IDs are irrelevant.</summary>
    public static bool NoGitlinks(AssertionLedger ledger, string identity, GitFact<IReadOnlyList<GitDiffEntry>> diff)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(diff);
        if (!diff.IsSuccess) return HistoryAvailable(ledger, identity, diff);
        GitDiffEntry[] links = diff.Value!.Where(entry => entry.SourceMode == "160000" || entry.DestinationMode == "160000").ToArray();
        foreach (GitDiffEntry entry in links)
            ledger.Block("EVIDENCE_BOUNDARY_GITLINK", entry.Path, $"Raw modes {entry.SourceMode} -> {entry.DestinationMode}.");
        ledger.Record(identity, links.Length == 0);
        return links.Length == 0;
    }

    /// <summary>Requires named literal string constants in the consuming C# source.</summary>
    public static bool PinnedRoots(AssertionLedger ledger, string identity, string consumingSource,
        IReadOnlyDictionary<string, string> roots)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(consumingSource);
        ArgumentNullException.ThrowIfNull(roots);
        IReadOnlyList<string> tokens = CSharpSourceDeclarations.UnconditionalTokens(consumingSource);
        bool passed = roots.Count > 0;
        foreach (KeyValuePair<string, string> root in roots)
        {
            if (!CSharpSourceDeclarations.ContainsSequence(tokens, ["const", "string", root.Key, "=", "\"" + root.Value + "\"", ";"]))
            {
                ledger.Block("ROOT_OF_TRUST_NOT_PINNED", root.Key, "The consuming source does not declare the expected literal root.");
                passed = false;
            }
        }
        if (roots.Count == 0) ledger.Block("ROOT_OF_TRUST_NOT_PINNED", identity, "No literal roots were supplied.");
        ledger.Record(identity, passed);
        return passed;
    }

    /// <summary>Rejects signed release evidence after separator and lexical path normalization.</summary>
    public static bool SignedEvidenceIsNotAllowlisted(AssertionLedger ledger, string identity, string root,
        IEnumerable<string> allowlist)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        ArgumentNullException.ThrowIfNull(allowlist);
        bool passed = true;
        foreach (string entry in allowlist)
        {
            if (entry.IndexOfAny(['*', '?', '[']) >= 0)
            {
                ledger.Block("SIGNED_EVIDENCE_ALLOWLISTED", entry, "Glob entries cannot exempt signed release evidence.");
                passed = false;
                continue;
            }
            string path = entry.Replace('\\', '/');
            try
            {
                string normalized = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(Path.Combine(root, path))).Replace('\\', '/');
                if (normalized.StartsWith("docs/release-evidence/", StringComparison.OrdinalIgnoreCase)
                    || normalized.Equals("docs/release-evidence", StringComparison.OrdinalIgnoreCase)
                    || normalized.Equals("docs", StringComparison.OrdinalIgnoreCase) || normalized == ".")
                {
                    ledger.Block("SIGNED_EVIDENCE_ALLOWLISTED", entry, "Signed release evidence cannot be exempted.");
                    passed = false;
                    continue;
                }
                GitFact<string> contained = RepositoryLocator.ResolveContainedPath(root, path);
                if (!contained.IsSuccess)
                {
                    ledger.Block("EVIDENCE_PATH_ESCAPE", entry, contained.Message!);
                    passed = false;
                }
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
            {
                ledger.Block("EVIDENCE_PATH_ESCAPE", entry, exception.Message);
                passed = false;
            }
        }
        ledger.Record(identity, passed);
        return passed;
    }
}
