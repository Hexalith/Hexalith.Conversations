// <copyright file="ConformanceOracleTieringValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Hexalith.Conversations.Client;

using Shouldly;

using Xunit;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Validates the Story 9.1 conformance oracle tiering disposition read-only and independently of its generator.
/// </summary>
/// <remarks>
/// Every check reads committed evidence, the reflected test assembly, its compiled IL, or Git objects. Nothing is
/// written. Failure messages start with the Story 9.1 blocker code that the observed defect maps to.
/// </remarks>
public sealed class ConformanceOracleTieringValidationTest
{
    private const string DispositionPath = "docs/release-evidence/conformance-oracle-tiering-disposition-v2.json";
    private const string SchemaPath = "docs/release-evidence/conformance-oracle-tiering-disposition-v2.schema.json";
    private const string MarkdownPath = "docs/release-evidence/conformance-oracle-tiering-disposition-v2.md";
    private const string ApprovalsPath = "docs/release-evidence/conformance-oracle-tiering-approvals-v2.json";
    private const string DecisionPath = "docs/release-evidence/conformance-oracle-tiering-decision-v2.json";
    private const string BaselinePath = "docs/release-evidence/release-baseline-v1.json";
    private const string AccumulatedManifestPath = "docs/release-evidence/preservation-traceability-manifest-v3-rc2.json";
    private const string ProtectedInventoryPath = "docs/release-evidence/preservation-traceability-manifest-v2.json";
    private const string ContractsBaselinePath = "docs/release-evidence/public-contract-shape-baseline-v1.json";
    private const string ClientBaselinePath = "docs/release-evidence/client-public-api-baseline-v1.json";
    private const string TestNamespace = "Hexalith.Conversations.Conformance.Tests";
    private const string ServerAssemblyName = "Hexalith.Conversations.Server";
    private const string OwnerRole = "Quality owner";

    private static readonly string[] ReclassifiedSuites =
    [
        "TelemetryCardinalityConformanceSuiteTest",
        "TelemetryRedactionConformanceSuiteTest",
        "ConformanceStatusConformanceSuiteTest",
    ];

    private static readonly string[] RowProposalFields =
    [
        "id", "sourcePath", "sourceSha256", "preSplitResultIdentity", "strengthMaterial", "strengthSha256",
        "serverBindings", "tier", "publicReplacement", "internalTypeAndReason", "rationale",
    ];

    private static readonly string[] SuiteProposalFields =
    [
        "suite", "class", "sourcePath", "sourceSha256", "releaseGateBehavior", "v1FloorTestIds",
        "v1FloorTestIdsSha256", "currentIdentities", "currentIdentitiesSha256", "membershipChanged", "tier",
        "rowTiers", "historicalApproval", "rationale", "manifestUpdate",
    ];

    /// <summary>
    /// AC-9.1-02: every pre-split test case occurs exactly once, bound to its exact source, its machine result
    /// identity, and its strength digest, and the reflected assembly declares no unlisted assertion.
    /// </summary>
    [Fact]
    public void AssertionInventoryShouldMatchPreSplitResultAndSource()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement root = disposition.RootElement;
        root.GetProperty("schemaVersion").GetString().ShouldBe("hexalith.conversations.conformance-oracle-tiering-disposition.v2");
        JsonElement[] rows = root.GetProperty("assertions").EnumerateArray().ToArray();
        JsonElement[] additions = root.GetProperty("validationAdditions").EnumerateArray().ToArray();
        rows.Length.ShouldBeGreaterThan(0, "CONFORMANCE_ASSERTION_MISSING: the frozen inventory is empty.");
        string[] identifiers = rows.Select(row => row.GetProperty("id").GetString()!).ToArray();
        string[] additionIdentifiers = additions.Select(row => row.GetProperty("id").GetString()!).ToArray();
        string[] duplicated = identifiers.Concat(additionIdentifiers).GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
        duplicated.ShouldBeEmpty($"CONFORMANCE_ASSERTION_DUPLICATE: {string.Join(", ", duplicated)}");
        identifiers.ShouldBe(identifiers.Order(StringComparer.Ordinal).ToArray(), "CONFORMANCE_ASSERTION_DUPLICATE: the inventory order is not canonical.");

        HashSet<string> reflected = ReflectedTestMethods().Select(method => TestIdentity(method)).ToHashSet(StringComparer.Ordinal);
        HashSet<string> recorded = identifiers.Concat(additionIdentifiers).ToHashSet(StringComparer.Ordinal);
        string[] missing = recorded.Except(reflected).Order(StringComparer.Ordinal).ToArray();
        string[] unknown = reflected.Except(recorded).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length > 0 && unknown.Length > 0)
        {
            throw new ShouldAssertException($"CONFORMANCE_ASSERTION_RENAMED: {missing[0]} is absent while {unknown[0]} is unlisted.");
        }

        missing.ShouldBeEmpty($"CONFORMANCE_ASSERTION_MISSING: {string.Join(", ", missing.Take(5))}");
        unknown.ShouldBeEmpty($"CONFORMANCE_ASSERTION_RENAMED: unlisted assertion(s) {string.Join(", ", unknown.Take(5))}");
        additions.ShouldAllBe(row => row.GetProperty("sourcePath").GetString() == $"tests/{TestNamespace}/{nameof(ConformanceOracleTieringValidationTest)}.cs");

        JsonElement result = root.GetProperty("preSplitResult");
        JsonElement exclusions = result.GetProperty("lane").GetProperty("exclusions");
        HashSet<string> excludedClasses = exclusions.GetProperty("classes").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
        HashSet<string> excludedMethods = exclusions.GetProperty("methods").EnumerateArray().Select(item => item.GetString()!).ToHashSet(StringComparer.Ordinal);
        int executed = 0;
        int passed = 0;
        List<string> failedTests = [];
        Dictionary<string, MethodInfo> methods = ReflectedTestMethods().ToDictionary(method => TestIdentity(method), StringComparer.Ordinal);
        foreach (JsonElement row in rows)
        {
            string id = row.GetProperty("id").GetString()!;
            JsonElement identity = row.GetProperty("preSplitResultIdentity");
            identity.GetProperty("discovered").GetBoolean().ShouldBeTrue($"CONFORMANCE_ASSERTION_MISSING: {id} was not discovered.");
            identity.GetProperty("displayName").GetString().ShouldBe(id);
            string owner = id[..id.LastIndexOf('.')];
            string expectedLane = excludedClasses.Contains(owner)
                ? "excluded-historical-class"
                : excludedMethods.Contains(id) ? "excluded-historical-method" : "executed";
            identity.GetProperty("lane").GetString().ShouldBe(expectedLane, $"CONFORMANCE_ASSERTION_RENAMED: {id} lane identity changed.");
            JsonElement[] results = identity.GetProperty("results").EnumerateArray().ToArray();
            bool theory = methods[id].GetCustomAttribute<TheoryAttribute>() is not null;
            row.GetProperty("kind").GetString().ShouldBe(theory ? "theory" : "fact");
            (expectedLane == "executed").ShouldBe(results.Length > 0, $"CONFORMANCE_ASSERTION_MISSING: {id} lacks its machine result.");
            foreach (JsonElement item in results)
            {
                string testName = item.GetProperty("testName").GetString()!;
                (theory ? testName.StartsWith(id + "(", StringComparison.Ordinal) : testName == id)
                    .ShouldBeTrue($"CONFORMANCE_ASSERTION_RENAMED: result {testName} does not identify {id}.");
                if (item.GetProperty("outcome").GetString() == "Passed")
                {
                    passed++;
                }
                else
                {
                    failedTests.Add(testName);
                }
            }

            identity.GetProperty("executed").GetInt32().ShouldBe(results.Length);
            executed += results.Length;
            AssertSourceBinding(row, id);
            Sha256(StrengthJson(row.GetProperty("strengthMaterial"))).ShouldBe(row.GetProperty("strengthSha256").GetString(),
                $"ASSERTION_STRENGTH_WEAKENED: {id} strength digest does not bind its material.");
        }

        foreach (JsonElement row in additions)
        {
            AssertSourceBinding(row, row.GetProperty("id").GetString()!);
        }

        JsonElement summary = result.GetProperty("result").GetProperty("summary");
        summary.GetProperty("executed").GetInt32().ShouldBe(executed, "CONFORMANCE_ASSERTION_MISSING: result rows do not sum to the machine total.");
        summary.GetProperty("passed").GetInt32().ShouldBe(passed);
        summary.GetProperty("failedTests").EnumerateArray().Select(item => item.GetString()!).ShouldBe(failedTests.Order(StringComparer.Ordinal));
        summary.GetProperty("skipped").GetInt32().ShouldBe(0);
        result.GetProperty("counts").GetProperty("discoveredMethods").GetInt32().ShouldBe(rows.Length);
        result.GetProperty("identitySha256").GetString().ShouldBe(Sha256(Canonical(identifiers.Select(id => (object?)id).ToList())));
        HashSet<string> frozenSources = result.GetProperty("sourceFiles").EnumerateArray()
            .Select(item => item.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal);
        rows.Select(row => row.GetProperty("sourcePath").GetString()!).Where(path => !frozenSources.Contains(path))
            .ShouldBeEmpty("CONFORMANCE_ASSERTION_MISSING: a row source is not a frozen source file.");
        string freeze = result.GetProperty("sourceCommit").GetString()!;
        foreach (JsonElement file in result.GetProperty("sourceFiles").EnumerateArray())
        {
            string path = file.GetProperty("path").GetString()!;
            Sha256(GitBlob(freeze, path)).ShouldBe(file.GetProperty("sha256").GetString(), $"CONFORMANCE_ASSERTION_RENAMED: frozen source {path} does not match the freeze commit.");
        }

        string retained = Path.Combine(FindRoot(), result.GetProperty("result").GetProperty("path").GetString()!);
        if (File.Exists(retained))
        {
            Sha256(File.ReadAllBytes(retained)).ShouldBe(result.GetProperty("result").GetProperty("sha256").GetString(),
                "CONFORMANCE_ASSERTION_RENAMED: the retained pre-split TRX differs from its bound digest.");
        }
    }

    /// <summary>
    /// AC-9.1-03: every row has an exact disposition, and every test whose compiled IL independently reaches the
    /// non-packable Server assembly is module-internal with its exact Server types and reason.
    /// </summary>
    /// <remarks>
    /// Exact Server types are checked against the Server project's declared source types, so this class binds no
    /// Server type and stays outside the guarded residual-coupling inventory.
    /// </remarks>
    [Fact]
    public void ServerBoundAssertionsShouldHaveExactDisposition()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement root = disposition.RootElement;
        HashSet<string> serverTypes = ServerSourceTypes();
        serverTypes.ShouldNotBeEmpty("TIER_REASON_MISSING: no Server source type was found.");
        Dictionary<string, JsonElement> rows = AllRows(root).ToDictionary(row => row.GetProperty("id").GetString()!, StringComparer.Ordinal);
        int moduleInternal = 0;
        foreach ((string id, JsonElement row) in rows)
        {
            row.TryGetProperty("tier", out JsonElement tier).ShouldBeTrue($"TIER_UNASSIGNED: {id}");
            string tierValue = tier.GetString() ?? string.Empty;
            new[] { "portable", "module-internal" }.ShouldContain(tierValue, $"TIER_UNASSIGNED: {id}");
            NonEmpty(row, "rationale").ShouldBeTrue($"TIER_REASON_MISSING: {id} has no rationale.");
            string[] bindings = row.GetProperty("serverBindings").EnumerateArray().Select(item => item.GetString()!).ToArray();
            if (tierValue == "module-internal")
            {
                moduleInternal++;
                row.TryGetProperty("internalTypeAndReason", out JsonElement detail).ShouldBeTrue($"TIER_REASON_MISSING: {id}");
                NonEmpty(detail, "reason").ShouldBeTrue($"TIER_REASON_MISSING: {id} has no internal reason.");
                string[] types = detail.GetProperty("types").EnumerateArray().Select(item => item.GetString()!).ToArray();
                types.ShouldNotBeEmpty($"TIER_REASON_MISSING: {id} names no exact internal type.");
                foreach (string type in types)
                {
                    bindings.ShouldContain(type, $"TIER_REASON_MISSING: {id} names {type} outside its bindings.");
                    serverTypes.ShouldContain(type, $"TIER_REASON_MISSING: {id} names {type}, which the Server project does not declare.");
                }

                row.TryGetProperty("publicReplacement", out _).ShouldBeFalse($"TIER_REASON_MISSING: {id} has two dispositions.");
            }
            else
            {
                bindings.ShouldBeEmpty($"TIER_REASON_MISSING: portable {id} still binds Server types.");
                row.TryGetProperty("publicReplacement", out JsonElement replacement).ShouldBeTrue($"TIER_REASON_MISSING: {id}");
                replacement.GetProperty("equalStrength").GetBoolean().ShouldBeTrue($"TIER_REASON_MISSING: {id}");
                replacement.GetProperty("strengthSha256").GetString().ShouldBe(row.GetProperty("strengthSha256").GetString(), $"TIER_REASON_MISSING: {id} replacement strength differs.");
                row.GetProperty("strengthMaterial").GetProperty("boundAssemblies").EnumerateArray()
                    .Select(item => item.GetString()).ShouldNotContain(ServerAssemblyName, $"TIER_REASON_MISSING: {id}");
            }
        }

        moduleInternal.ShouldBeGreaterThan(0, "TIER_UNASSIGNED: no Server-bound assertion was dispositioned.");
        int ilBound = 0;
        using ConformanceTieringIlReferenceReader reader = new(typeof(ConformanceOracleTieringValidationTest).Assembly.Location);
        foreach (MethodInfo method in ReflectedTestMethods())
        {
            string id = TestIdentity(method);
            bool reachesServer = reader.ReachableReferences(method).Any(reference => reference.StartsWith(ServerAssemblyName + "|", StringComparison.Ordinal));
            if (!reachesServer)
            {
                continue;
            }

            ilBound++;
            rows.ShouldContainKey(id, $"TIER_UNASSIGNED: {id}");
            rows[id].GetProperty("tier").GetString().ShouldBe("module-internal",
                $"TIER_REASON_MISSING: {id} reaches {ServerAssemblyName} in IL but is not module-internal with an exact type and reason.");
        }

        ilBound.ShouldBeGreaterThan(0, "TIER_UNASSIGNED: the IL check observed no Server-bound assertion; the check is vacuous.");
    }

    /// <summary>
    /// AC-9.1-04: each row's canonical strength triple, its digest, the Quality-approved before-inventory digest,
    /// and every frozen closure source stay equal; the IL-observed Server binding is part of the bound assemblies.
    /// </summary>
    [Fact]
    public void StrengthDigestsShouldRemainEqual()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        using JsonDocument approvals = LoadJson(ApprovalsPath);
        JsonElement root = disposition.RootElement;
        root.GetProperty("strengthDefinition").GetProperty("material").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["boundAssemblies", "behaviorIdentity", "negativeCaseCount"]);
        JsonElement proposal = approvals.RootElement.GetProperty("proposal");
        Dictionary<string, string> approved = proposal.GetProperty("assertions").EnumerateArray()
            .Concat(proposal.GetProperty("validationAdditions").EnumerateArray())
            .ToDictionary(row => row.GetProperty("id").GetString()!, row => row.GetProperty("strengthSha256").GetString()!, StringComparer.Ordinal);
        string freeze = root.GetProperty("preSplitResult").GetProperty("sourceCommit").GetString()!;
        Dictionary<string, MethodInfo> methods = ReflectedTestMethods().ToDictionary(method => TestIdentity(method), StringComparer.Ordinal);
        using ConformanceTieringIlReferenceReader reader = new(typeof(ConformanceOracleTieringValidationTest).Assembly.Location);
        int checkedRows = 0;
        foreach (JsonElement row in AllRows(root))
        {
            string id = row.GetProperty("id").GetString()!;
            JsonElement material = row.GetProperty("strengthMaterial");
            material.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .ShouldBe(["behaviorIdentity", "boundAssemblies", "negativeCaseCount"], Case.Sensitive, $"ASSERTION_STRENGTH_WEAKENED: {id} material is not the canonical triple.");
            string digest = Sha256(StrengthJson(material));
            row.GetProperty("strengthSha256").GetString().ShouldBe(digest, $"ASSERTION_STRENGTH_WEAKENED: {id} digest differs from its material.");
            approved.ShouldContainKey(id, $"ASSERTION_STRENGTH_WEAKENED: {id} has no approved before-strength.");
            approved[id].ShouldBe(digest, $"ASSERTION_STRENGTH_WEAKENED: {id} differs from the approved before-inventory.");
            material.GetProperty("negativeCaseCount").GetInt32().ShouldBeLessThanOrEqualTo(row.GetProperty("assertionSiteCount").GetInt32());
            bool frozenRow = row.GetProperty("preSplitResultIdentity").GetProperty("discovered").GetBoolean();
            foreach (JsonElement file in row.GetProperty("closureFiles").EnumerateArray())
            {
                string path = file.GetProperty("path").GetString()!;
                string expected = file.GetProperty("sha256").GetString()!;
                Sha256(ReadBytes(path)).ShouldBe(expected, $"ASSERTION_STRENGTH_WEAKENED: {id} closure source {path} changed.");
                if (frozenRow)
                {
                    Sha256(GitBlob(freeze, path)).ShouldBe(expected, $"ASSERTION_STRENGTH_WEAKENED: {id} closure source {path} differs from the freeze.");
                }
            }

            string[] bound = material.GetProperty("boundAssemblies").EnumerateArray().Select(item => item.GetString()!).ToArray();
            bool reachesServer = reader.ReachableReferences(methods[id]).Any(reference => reference.StartsWith(ServerAssemblyName + "|", StringComparison.Ordinal));
            if (reachesServer)
            {
                bound.ShouldContain(ServerAssemblyName, $"ASSERTION_STRENGTH_WEAKENED: {id} binds Server in IL but its material omits it.");
            }

            checkedRows++;
        }

        checkedRows.ShouldBe(methods.Count, "ASSERTION_STRENGTH_WEAKENED: not every reflected assertion has strength material.");
    }

    /// <summary>
    /// AC-9.1-05: the three manifested suites keep their exact v1-floor identities and the accumulated FR-20
    /// membership maps completely onto frozen rows.
    /// </summary>
    [Fact]
    public void DenominatorSuitesShouldRemainUnchanged()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        using JsonDocument manifest = LoadJson(AccumulatedManifestPath);
        using JsonDocument baseline = LoadJson(BaselinePath);
        using JsonDocument decision = LoadJson(DecisionPath);
        JsonElement root = disposition.RootElement;
        JsonElement denominator = manifest.RootElement.GetProperty("testDenominator");
        string[] floor = denominator.GetProperty("originalV1Floor").GetProperty("testIds").EnumerateArray().Select(item => item.GetString()!).ToArray();
        string[] accumulated = denominator.GetProperty("laterApprovedAdditions").GetProperty("cumulativeTestIds").EnumerateArray().Select(item => item.GetString()!).ToArray();
        floor.Length.ShouldBe(baseline.RootElement.GetProperty("conformanceOracle").GetProperty("conformanceSuiteTestCount").GetInt32(), "FR20_DENOMINATOR_DRIFT");
        accumulated.Length.ShouldBe(384, "FR20_DENOMINATOR_DRIFT: the approved accumulated membership changed.");
        decision.RootElement.GetProperty("fr20Reclassification").GetProperty("manifestedSuitesReclassified").EnumerateArray()
            .Select(item => item.GetString()).ShouldBe(ReclassifiedSuites, "FR20_DENOMINATOR_DRIFT");
        Dictionary<string, JsonElement> rows = root.GetProperty("assertions").EnumerateArray()
            .ToDictionary(row => row.GetProperty("id").GetString()!, StringComparer.Ordinal);
        HashSet<string> reflected = ReflectedTestMethods().Select(method => TestIdentity(method)).ToHashSet(StringComparer.Ordinal);
        JsonElement[] suites = root.GetProperty("denominatorSuites").EnumerateArray().ToArray();
        suites.Select(suite => suite.GetProperty("suite").GetString()).ShouldBe(ReclassifiedSuites, "FR20_DENOMINATOR_DRIFT");
        foreach (JsonElement suite in suites)
        {
            string className = $"{TestNamespace}.{suite.GetProperty("suite").GetString()}";
            suite.GetProperty("class").GetString().ShouldBe(className, "FR20_DENOMINATOR_DRIFT");
            string[] expectedFloor = floor.Where(id => id.StartsWith(className + ".", StringComparison.Ordinal)).ToArray();
            expectedFloor.ShouldNotBeEmpty($"FR20_DENOMINATOR_DRIFT: {className} left the v1 floor.");
            suite.GetProperty("v1FloorTestIds").EnumerateArray().Select(item => item.GetString()).ShouldBe(expectedFloor, $"FR20_DENOMINATOR_DRIFT: {className} floor membership changed.");
            string[] current = reflected.Where(id => id.StartsWith(className + ".", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            suite.GetProperty("currentIdentities").EnumerateArray().Select(item => item.GetString()).ShouldBe(current, $"FR20_DENOMINATOR_DRIFT: {className} identities changed.");
            expectedFloor.Except(current).ShouldBeEmpty($"FR20_DENOMINATOR_DRIFT: {className} lost a v1 floor identity.");
            suite.GetProperty("membershipChanged").GetBoolean().ShouldBeFalse("FR20_DENOMINATOR_DRIFT");
            JsonElement update = suite.GetProperty("manifestUpdate");
            update.GetProperty("testsRemoved").GetInt32().ShouldBe(0, "FR20_DENOMINATOR_DRIFT");
            update.GetProperty("testsWeakened").GetInt32().ShouldBe(0, "FR20_DENOMINATOR_DRIFT");
            update.GetProperty("membershipChanged").GetBoolean().ShouldBeFalse("FR20_DENOMINATOR_DRIFT");
            update.GetProperty("previousManifest").GetProperty("sha256").GetString().ShouldBe(Sha256(ReadCommitted(BaselinePath)), "FR20_DENOMINATOR_DRIFT");
        }

        foreach (string id in floor.Concat(accumulated))
        {
            string method = id.Split('(', 2)[0];
            rows.ShouldContainKey(method, $"FR20_DENOMINATOR_DRIFT: {id} has no frozen row.");
            reflected.ShouldContain(method, $"FR20_DENOMINATOR_DRIFT: {id} is no longer declared.");
            JsonElement identity = rows[method].GetProperty("preSplitResultIdentity");
            if (id.Contains('(', StringComparison.Ordinal) && identity.GetProperty("lane").GetString() == "executed")
            {
                identity.GetProperty("results").EnumerateArray().Select(item => item.GetProperty("testName").GetString())
                    .ShouldContain(id.Replace("\\\"", "\"", StringComparison.Ordinal), $"FR20_DENOMINATOR_DRIFT: theory case {id} lost its result identity.");
            }
        }

        JsonElement membership = root.GetProperty("fr20Membership");
        membership.GetProperty("missingIdentities").GetArrayLength().ShouldBe(0, "FR20_DENOMINATOR_DRIFT");
        membership.GetProperty("v1Floor").GetProperty("testCount").GetInt32().ShouldBe(floor.Length, "FR20_DENOMINATOR_DRIFT");
        membership.GetProperty("v1Floor").GetProperty("testIdsSha256").GetString()
            .ShouldBe(Sha256(Encoding.UTF8.GetBytes(string.Join('\n', floor) + "\n")), "FR20_DENOMINATOR_DRIFT");
        membership.GetProperty("approvedCumulative").GetProperty("testIdsSha256").GetString()
            .ShouldBe(Sha256(Encoding.UTF8.GetBytes(string.Join('\n', accumulated) + "\n")), "FR20_DENOMINATOR_DRIFT");
    }

    /// <summary>
    /// AC-9.1-06: every reclassified assertion and denominator-suite row carries the named Quality owner, the
    /// approval identity, its rationale, and a digest that the owner's decision approved; the historical suite
    /// approval stays distinct and approves no row.
    /// </summary>
    [Fact]
    public void ReclassificationsShouldBindApprovals()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        using JsonDocument approvals = LoadJson(ApprovalsPath);
        using JsonDocument decision = LoadJson(DecisionPath);
        JsonElement root = disposition.RootElement;
        JsonElement approvalRoot = approvals.RootElement;
        approvalRoot.GetProperty("schemaVersion").GetString().ShouldBe("hexalith.conversations.conformance-oracle-tiering-approvals.v2");
        approvalRoot.GetProperty("status").GetString().ShouldBe("approved", "TIER_APPROVAL_MISSING: the proposal is not approved.");
        JsonElement ownerDecision = approvalRoot.GetProperty("decision");
        ownerDecision.ValueKind.ShouldBe(JsonValueKind.Object, "TIER_APPROVAL_MISSING: no owner decision is recorded.");
        ownerDecision.GetProperty("state").GetString().ShouldBe("approved", "TIER_APPROVAL_MISSING");
        ownerDecision.GetProperty("role").GetString().ShouldBe(OwnerRole, "TIER_APPROVAL_MISSING");
        foreach (string field in new[] { "approver", "approvalId", "approvedOn", "evidence" })
        {
            NonEmpty(ownerDecision, field).ShouldBeTrue($"TIER_APPROVAL_MISSING: the owner decision lacks {field}.");
        }

        string approvalId = ownerDecision.GetProperty("approvalId").GetString()!;
        string ownerLabel = $"{ownerDecision.GetProperty("approver").GetString()} ({OwnerRole})";
        JsonElement binding = root.GetProperty("approvals");
        binding.GetProperty("sha256").GetString().ShouldBe(Sha256(ReadBytes(ApprovalsPath)), "TIER_APPROVAL_MISSING: the approvals file changed after binding.");
        binding.GetProperty("approvalId").GetString().ShouldBe(approvalId, "TIER_APPROVAL_MISSING");

        JsonElement result = root.GetProperty("preSplitResult");
        List<object?> assertions = [];
        List<object?> additions = [];
        List<object?> suites = [];
        Dictionary<string, string> rowDigests = new(StringComparer.Ordinal);
        foreach (JsonElement row in root.GetProperty("assertions").EnumerateArray())
        {
            string digest = Sha256(Canonical(Project(row, RowProposalFields)));
            assertions.Add(new List<object?> { row.GetProperty("id").GetString(), digest });
            rowDigests[row.GetProperty("id").GetString()!] = digest;
        }

        foreach (JsonElement row in root.GetProperty("validationAdditions").EnumerateArray())
        {
            string digest = Sha256(Canonical(Project(row, RowProposalFields)));
            additions.Add(new List<object?> { row.GetProperty("id").GetString(), digest });
            rowDigests[row.GetProperty("id").GetString()!] = digest;
        }

        foreach (JsonElement suite in root.GetProperty("denominatorSuites").EnumerateArray())
        {
            suites.Add(new List<object?> { suite.GetProperty("suite").GetString(), Sha256(Canonical(Project(suite, SuiteProposalFields))) });
        }

        SortedDictionary<string, object?> membership = new(StringComparer.Ordinal)
        {
            ["assertions"] = assertions,
            ["contractSha256"] = root.GetProperty("authority").GetProperty("contractSha256").GetString(),
            ["decisionSha256"] = Sha256(ReadBytes(DecisionPath)),
            ["denominatorSuites"] = suites,
            ["preSplitResultSha256"] = result.GetProperty("result").GetProperty("sha256").GetString(),
            ["sourceCommit"] = result.GetProperty("sourceCommit").GetString(),
            ["storyId"] = "9.1",
            ["validationAdditions"] = additions,
        };
        string membershipDigest = Sha256(Canonical(membership));
        ownerDecision.GetProperty("approvedMembershipSha256").GetString().ShouldBe(membershipDigest,
            "TIER_APPROVAL_MISSING: the owner approved a different membership than the disposition rows.");
        approvalRoot.GetProperty("proposal").GetProperty("membershipSha256").GetString().ShouldBe(membershipDigest, "TIER_APPROVAL_MISSING");
        binding.GetProperty("membershipSha256").GetString().ShouldBe(membershipDigest, "TIER_APPROVAL_MISSING");
        Dictionary<string, string> proposedDigests = approvalRoot.GetProperty("proposal").GetProperty("assertions").EnumerateArray()
            .Concat(approvalRoot.GetProperty("proposal").GetProperty("validationAdditions").EnumerateArray())
            .ToDictionary(row => row.GetProperty("id").GetString()!, row => row.GetProperty("rowSha256").GetString()!, StringComparer.Ordinal);
        proposedDigests.Count.ShouldBe(rowDigests.Count, "TIER_APPROVAL_MISSING: the approved inventory hides or adds rows.");

        int reclassified = 0;
        foreach (JsonElement row in AllRows(root))
        {
            string id = row.GetProperty("id").GetString()!;
            row.GetProperty("owner").GetString().ShouldBe(ownerLabel, $"TIER_APPROVAL_MISSING: {id} has no named owner.");
            JsonElement approval = row.GetProperty("approval");
            approval.ValueKind.ShouldBe(JsonValueKind.Object, $"TIER_APPROVAL_MISSING: {id}");
            approval.GetProperty("approvalId").GetString().ShouldBe(approvalId, $"TIER_APPROVAL_MISSING: {id}");
            approval.GetProperty("membershipSha256").GetString().ShouldBe(membershipDigest, $"TIER_APPROVAL_MISSING: {id}");
            approval.GetProperty("rowSha256").GetString().ShouldBe(rowDigests[id], $"TIER_APPROVAL_MISSING: {id} row digest differs from its content.");
            proposedDigests[id].ShouldBe(rowDigests[id], $"TIER_APPROVAL_MISSING: {id} was not the approved row.");
            NonEmpty(row, "rationale").ShouldBeTrue($"TIER_APPROVAL_MISSING: {id} has no rationale.");
            if (row.GetProperty("tier").GetString() == "module-internal")
            {
                reclassified++;
            }
        }

        reclassified.ShouldBeGreaterThan(0, "TIER_APPROVAL_MISSING: no reclassified row was checked.");
        JsonElement fr20 = decision.RootElement.GetProperty("fr20Reclassification");
        foreach (JsonElement suite in root.GetProperty("denominatorSuites").EnumerateArray())
        {
            string name = suite.GetProperty("suite").GetString()!;
            suite.GetProperty("owner").GetString().ShouldBe(ownerLabel, $"TIER_APPROVAL_MISSING: {name} has no named owner.");
            suite.GetProperty("approval").GetProperty("approvalId").GetString().ShouldBe(approvalId, $"TIER_APPROVAL_MISSING: {name}");
            NonEmpty(suite, "rationale").ShouldBeTrue($"TIER_APPROVAL_MISSING: {name} has no rationale.");
            JsonElement update = suite.GetProperty("manifestUpdate");
            update.GetProperty("version").GetInt32().ShouldBe(2, $"TIER_APPROVAL_MISSING: {name} lacks a versioned manifest update.");
            update.GetProperty("path").GetString().ShouldBe(DispositionPath, $"TIER_APPROVAL_MISSING: {name}");
            JsonElement historical = suite.GetProperty("historicalApproval");
            historical.GetProperty("approvesRows").GetBoolean().ShouldBeFalse($"TIER_APPROVAL_MISSING: {name} treats suite approval as row approval.");
            historical.GetProperty("approvedBy").GetString().ShouldBe(fr20.GetProperty("namedOwnerApproval").GetString(), "TIER_APPROVAL_MISSING");
            historical.GetProperty("sha256").GetString().ShouldBe(Sha256(ReadBytes(DecisionPath)), "TIER_APPROVAL_MISSING");
        }

        approvalRoot.GetProperty("historicalSuiteApproval").GetProperty("approvesRows").GetBoolean().ShouldBeFalse("TIER_APPROVAL_MISSING");
    }

    /// <summary>
    /// AC-9.1-07: the candidate's public module sources equal the frozen surface, the pre-PC Contracts baseline is
    /// byte-identical, and the built Client exports exactly the reviewed types.
    /// </summary>
    [Fact]
    public void TieringShouldNotWidenPublicContracts()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        JsonElement contract = disposition.RootElement.GetProperty("publicContract");
        JsonElement baseline = contract.GetProperty("protectedBaseline");
        baseline.GetProperty("path").GetString().ShouldBe(ContractsBaselinePath);
        Sha256(ReadCommitted(ContractsBaselinePath)).ShouldBe(baseline.GetProperty("sha256").GetString(), "PUBLIC_CONTRACT_WIDENED: the pre-PC public baseline changed.");
        JsonElement surface = contract.GetProperty("freezeSurface");
        string freeze = surface.GetProperty("commit").GetString()!;
        freeze.ShouldBe(disposition.RootElement.GetProperty("preSplitResult").GetProperty("sourceCommit").GetString());
        List<object?> projects = [];
        foreach (JsonElement project in surface.GetProperty("projects").EnumerateArray())
        {
            string directory = project.GetProperty("path").GetString()!;
            string[] frozen = Git("ls-tree", "-r", "--full-tree", freeze, "--", directory + "/").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('\t', 2)).Select(parts => $"{parts[1]} {parts[0].Split(' ')[2]}").Order(StringComparer.Ordinal).ToArray();
            string[] staged = Git("ls-files", "-s", "--", directory + "/").Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('\t', 2)).Select(parts => $"{parts[1]} {parts[0].Split(' ')[1]}").Order(StringComparer.Ordinal).ToArray();
            staged.ShouldBe(frozen, $"PUBLIC_CONTRACT_WIDENED: {directory} differs from the freeze commit.");
            Git("diff", "--name-only", "--", directory + "/").Trim().ShouldBeEmpty($"PUBLIC_CONTRACT_WIDENED: {directory} has unstaged changes.");
            Git("ls-files", "--others", "--exclude-standard", "--", directory + "/").Trim().ShouldBeEmpty($"PUBLIC_CONTRACT_WIDENED: {directory} has new files.");
            string listing = string.Concat(frozen.Select(entry => entry.Split(' ')[0]).Select(path => $"{path}\0{Sha256(ReadBytes(path))}\n"));
            Sha256(Encoding.UTF8.GetBytes(listing)).ShouldBe(project.GetProperty("sha256").GetString(), $"PUBLIC_CONTRACT_WIDENED: {directory} source digest changed.");
            project.GetProperty("fileCount").GetInt32().ShouldBe(frozen.Length, $"PUBLIC_CONTRACT_WIDENED: {directory} file count changed.");
            projects.Add(new SortedDictionary<string, object?>(StringComparer.Ordinal)
            {
                ["fileCount"] = frozen.Length,
                ["path"] = directory,
                ["sha256"] = project.GetProperty("sha256").GetString(),
            });
        }

        projects.Count.ShouldBe(4, "PUBLIC_CONTRACT_WIDENED: a public module project is missing from the frozen surface.");
        surface.GetProperty("sha256").GetString().ShouldBe(Sha256(Canonical(projects)), "PUBLIC_CONTRACT_WIDENED");
        using JsonDocument client = LoadJson(ClientBaselinePath);
        string[] reviewed = client.RootElement.GetProperty("types").EnumerateArray().Select(type => type.GetProperty("name").GetString()!).Order(StringComparer.Ordinal).ToArray();
        typeof(ClientAssemblyMarker).Assembly.GetExportedTypes().Select(type => type.FullName!).Order(StringComparer.Ordinal)
            .ShouldBe(reviewed, Case.Sensitive, "PUBLIC_CONTRACT_WIDENED: the built Client exports differ from the reviewed baseline.");
        contract.GetProperty("preExistingDrift").GetProperty("approvalClaimed").GetBoolean().ShouldBeFalse();
    }

    /// <summary>
    /// AC-9.1-08: the v1 artifacts keep their protected hashes, the decision keeps its protected bytes, and the v2
    /// disposition links each by digest without editing it.
    /// </summary>
    [Fact]
    public void V2ShouldSupersedeWithoutEditingV1()
    {
        using JsonDocument disposition = LoadJson(DispositionPath);
        using JsonDocument inventory = LoadJson(ProtectedInventoryPath);
        JsonElement supersedes = disposition.RootElement.GetProperty("supersedes");
        supersedes.GetProperty("v1MutationAllowed").GetBoolean().ShouldBeFalse("V1_ARTIFACT_DRIFT");
        supersedes.GetProperty("protectedInventory").GetProperty("sha256").GetString().ShouldBe(Sha256(ReadCommitted(ProtectedInventoryPath)), "V1_ARTIFACT_DRIFT");
        JsonElement[] protectedBindings = inventory.RootElement.GetProperty("immutableV1Bindings").EnumerateArray().ToArray();
        JsonElement[] linked = supersedes.GetProperty("v1Artifacts").EnumerateArray().ToArray();
        linked.Select(item => (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString()))
            .ShouldBe(protectedBindings.Select(item => (item.GetProperty("path").GetString(), item.GetProperty("sha256").GetString())),
                "V1_ARTIFACT_DRIFT: the v2 links differ from the protected v1 inventory.");
        string freeze = disposition.RootElement.GetProperty("preSplitResult").GetProperty("sourceCommit").GetString()!;
        foreach (JsonElement item in protectedBindings)
        {
            string path = item.GetProperty("path").GetString()!;
            Sha256(ReadCommitted(path)).ShouldBe(item.GetProperty("sha256").GetString(), $"V1_ARTIFACT_DRIFT: {path}");
            Git("diff", "--name-only", freeze, "HEAD", "--", path).Trim().ShouldBeEmpty($"V1_ARTIFACT_DRIFT: {path} changed after the freeze.");
        }

        string decisionSha = Sha256(ReadCommitted(DecisionPath));
        inventory.RootElement.GetProperty("sourceBindings").EnumerateArray()
            .Single(item => item.GetProperty("path").GetString() == DecisionPath).GetProperty("sha256").GetString()
            .ShouldBe(decisionSha, "V1_ARTIFACT_DRIFT: the approved decision changed.");
        supersedes.GetProperty("decision").GetProperty("sha256").GetString().ShouldBe(decisionSha, "V1_ARTIFACT_DRIFT");
        disposition.RootElement.GetProperty("decision").GetProperty("sha256").GetString().ShouldBe(decisionSha, "V1_ARTIFACT_DRIFT");
        using JsonDocument decision = JsonDocument.Parse(ReadCommitted(DecisionPath));
        decision.RootElement.GetProperty("triageResults").ValueKind.ShouldBe(JsonValueKind.Null, "V1_ARTIFACT_DRIFT: the decision was edited instead of superseded.");
        foreach (JsonElement item in supersedes.GetProperty("tieringLineage").EnumerateArray())
        {
            string path = item.GetProperty("path").GetString()!;
            Sha256(ReadCommitted(path)).ShouldBe(item.GetProperty("sha256").GetString(), $"V1_ARTIFACT_DRIFT: {path}");
            Sha256(GitBlob(freeze, path)).ShouldBe(item.GetProperty("sha256").GetString(), $"V1_ARTIFACT_DRIFT: {path} changed after the freeze.");
        }

        Sha256(ReadBytes(MarkdownPath)).ShouldBe(disposition.RootElement.GetProperty("renderedMarkdownSha256").GetString(), "V1_ARTIFACT_DRIFT: the v2 Markdown is not digest-bound.");
        using JsonDocument schema = LoadJson(SchemaPath);
        schema.RootElement.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    }

    private static IEnumerable<JsonElement> AllRows(JsonElement root)
        => root.GetProperty("assertions").EnumerateArray().Concat(root.GetProperty("validationAdditions").EnumerateArray());

    private static void AssertSourceBinding(JsonElement row, string id)
    {
        string path = row.GetProperty("sourcePath").GetString()!;
        byte[] content = ReadBytes(path);
        Sha256(content).ShouldBe(row.GetProperty("sourceSha256").GetString(), $"CONFORMANCE_ASSERTION_RENAMED: {id} source bytes changed.");
        string[] lines = Encoding.UTF8.GetString(content).Split('\n');
        int line = row.GetProperty("sourceLine").GetInt32();
        string method = id[(id.LastIndexOf('.') + 1)..];
        line.ShouldBeInRange(1, lines.Length, $"CONFORMANCE_ASSERTION_MISSING: {id} line is outside its source.");
        lines.Skip(line - 1).Take(40).Any(text => text.Contains($" {method}(", StringComparison.Ordinal))
            .ShouldBeTrue($"CONFORMANCE_ASSERTION_RENAMED: {id} is not declared at its source line.");
    }

    private static List<MethodInfo> ReflectedTestMethods()
        => typeof(ConformanceOracleTieringValidationTest).Assembly.GetTypes()
            .Where(type => type.Namespace == TestNamespace && type.IsClass && !type.IsAbstract && !type.IsNested)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<FactAttribute>() is not null)
            .ToList();

    private static string TestIdentity(MethodInfo method) => $"{method.DeclaringType!.FullName}.{method.Name}";

    private static HashSet<string> ServerSourceTypes()
    {
        string project = Path.Combine(FindRoot(), "src", ServerAssemblyName);
        HashSet<string> types = new(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
        {
            string first = Path.GetRelativePath(project, path).Split(Path.DirectorySeparatorChar)[0];
            if (first is "bin" or "obj")
            {
                continue;
            }

            string text = Regex.Replace(File.ReadAllText(path), @"//[^\n]*|/\*[\s\S]*?\*/", string.Empty);
            Match space = Regex.Match(text, @"^\s*namespace\s+([A-Za-z0-9_.]+)\s*[;{]", RegexOptions.Multiline);
            if (!space.Success)
            {
                continue;
            }

            foreach (Match declaration in Regex.Matches(text, @"\b(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+([A-Za-z_][A-Za-z0-9_]*)"))
            {
                _ = types.Add($"{space.Groups[1].Value}.{declaration.Groups[1].Value}");
            }
        }

        return types;
    }

    private static bool NonEmpty(JsonElement element, string property)
        => element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());

    private static byte[] StrengthJson(JsonElement material) => Canonical(Project(material, ["boundAssemblies", "behaviorIdentity", "negativeCaseCount"]));

    private static SortedDictionary<string, object?> Project(JsonElement element, string[] names)
    {
        SortedDictionary<string, object?> projection = new(StringComparer.Ordinal);
        foreach (string name in names)
        {
            if (element.TryGetProperty(name, out JsonElement value))
            {
                projection[name] = Materialize(value);
            }
        }

        return projection;
    }

    private static object? Materialize(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => new SortedDictionary<string, object?>(
            value.EnumerateObject().ToDictionary(property => property.Name, property => Materialize(property.Value), StringComparer.Ordinal),
            StringComparer.Ordinal),
        JsonValueKind.Array => value.EnumerateArray().Select(Materialize).ToList(),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetInt64(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null,
    };

    /// <summary>
    /// Writes the generator's canonical JSON: sorted keys, compact separators, raw UTF-8, Python string escapes.
    /// </summary>
    private static byte[] Canonical(object? value)
    {
        StringBuilder builder = new();
        Write(builder, value);
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void Write(StringBuilder builder, object? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case bool flag:
                builder.Append(flag ? "true" : "false");
                break;
            case long number:
                builder.Append(number.ToString(CultureInfo.InvariantCulture));
                break;
            case int number:
                builder.Append(number.ToString(CultureInfo.InvariantCulture));
                break;
            case string text:
                WriteString(builder, text);
                break;
            case SortedDictionary<string, object?> map:
                builder.Append('{');
                bool first = true;
                foreach ((string key, object? item) in map)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteString(builder, key);
                    builder.Append(':');
                    Write(builder, item);
                }

                builder.Append('}');
                break;
            case IEnumerable<object?> items:
                builder.Append('[');
                bool firstItem = true;
                foreach (object? item in items)
                {
                    if (!firstItem)
                    {
                        builder.Append(',');
                    }

                    firstItem = false;
                    Write(builder, item);
                }

                builder.Append(']');
                break;
            default:
                throw new InvalidOperationException($"Unsupported canonical JSON value {value.GetType()}.");
        }
    }

    private static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (char character in text)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                default:
                    if (character < 0x20)
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }

    private static JsonDocument LoadJson(string relativePath) => JsonDocument.Parse(ReadBytes(relativePath));

    private static byte[] ReadBytes(string relativePath) => File.ReadAllBytes(Path.Combine(FindRoot(), relativePath));

    private static byte[] ReadCommitted(string relativePath) => GitBlob("HEAD", relativePath);

    private static string Sha256(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

    private static byte[] GitBlob(string revision, string relativePath)
    {
        ProcessStartInfo start = new("git")
        {
            WorkingDirectory = FindRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("show");
        start.ArgumentList.Add($"{revision}:{relativePath}");
        using Process process = Process.Start(start)!;
        using MemoryStream buffer = new();
        process.StandardOutput.BaseStream.CopyTo(buffer);
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return buffer.ToArray();
    }

    private static string Git(params string[] arguments)
    {
        ProcessStartInfo start = new("git")
        {
            WorkingDirectory = FindRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, error);
        return output;
    }

    private static string FindRoot()
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

        throw new DirectoryNotFoundException("Unable to locate repository root.");
    }
}
