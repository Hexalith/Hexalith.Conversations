// <copyright file="DerivedKeyLifecycleAdrValidationTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.TestSupport;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>
/// Story 12.1 AC-12.1-03: the decided derived-key families stay replaceable projection state that only the
/// EventStore-delivered projection route writes, and no read repairs durable state.
/// </summary>
/// <remarks>
/// Pinned here: the ADR file's SHA-256, which the test recomputes, the decision's recorded signable-payload digest,
/// the family set, the decided write sequences and the runtime key-builder set. The test compares the recorded
/// payload digest with its pin but does not recompute it; <c>verify_derived_key_decision.py</c> (AC-12.1-04) does.
/// It reads files only and never Git history, so it is safe in CI.
/// </remarks>
[Collection(ReleaseEvidenceArtifactCollection.Name)]
public sealed class DerivedKeyLifecycleAdrValidationTest
{
    private const string AdrPath = "docs/adrs/0005-conversation-derived-key-lifecycle.md";
    private const string AdrSha256 = "2c946907e438850b643f17db704e702c71ece3b7a2826c113b1251df5a0cf068";
    private const string DecisionPath = "docs/release-evidence/derived-key-lifecycle-decision-v1.json";
    private const string DecisionSignablePayloadSha256 = "13943cc3b30e3183af09ba5549b0984a4d7170db2e2929c1b85658c22514bbbe";
    private const string SchemaVersion = "hexalith.conversations.derived-key-lifecycle-decision.v1";
    private const string Violation = "EVENTSTORE_AUTHORITY_VIOLATED";
    private const string KeysTypeName = "Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelKeys";

    private static readonly string[] _families =
    [
        "conversation",
        "tenant-index",
        "dispatch-ledger",
        "resolution-metadata",
        "resolution-selection",
        "resolution-proof",
        "resolution-fence",
    ];

    private static readonly Dictionary<string, string> _keyBuilders = new(StringComparer.Ordinal)
    {
        ["conversation"] = "ConversationKey",
        ["tenant-index"] = "TenantIndexKey",
        ["dispatch-ledger"] = "DispatchLedgerKey",
        ["resolution-metadata"] = "ResolutionMetadataKey",
        ["resolution-selection"] = "ResolutionSelectionKey",
        ["resolution-proof"] = "ResolutionProofKey",
        ["resolution-fence"] = "ResolutionFenceKey",
    };

    private static readonly string[] _liveSequence =
    [
        "1:dispatch-ledger:acquire",
        "2:resolution-proof:pending",
        "3:resolution-fence:pending",
        "4:tenant-index:pending",
        "5:conversation:write",
        "6:tenant-index:merge",
        "7:resolution-metadata:batch",
        "7:resolution-selection:batch",
        "7:resolution-fence:batch",
        "8:resolution-fence:complete",
        "9:resolution-proof:complete",
        "10:dispatch-ledger:complete",
    ];

    private static readonly string[] _rebuildSequence =
    [
        "1:resolution-proof",
        "2:resolution-metadata",
        "3:conversation",
        "4:tenant-index",
        "5:dispatch-ledger",
        "6:resolution-fence",
        "7:resolution-selection",
    ];

    // Two identity sets whose encodings differ at their first character, so the common prefix of their runtime
    // keys is exactly the runtime key prefix.
    private static readonly (string Tenant, string Conversation, string Dispatch) _first = ("tenant-α/1", "conversation+1", "dispatch-1");
    private static readonly (string Tenant, string Conversation, string Dispatch) _second = ("Zeta tenant", "Zeta conversation", "other dispatch");

    /// <summary>
    /// The decided families equal the runtime key builders, every family names the projection route as its sole
    /// writer with read repair false, and derived state is replaced only by a full replay from EventStore.
    /// </summary>
    [Fact]
    public void EventStoreShouldRemainOnlyWriteAuthority()
    {
        string root = RepositoryLocator.FindRepositoryRoot();
        string adrDigest = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, AdrPath))));
        adrDigest.ShouldBe(AdrSha256, "DERIVED_KEY_DECISION_DRIFT: ADR 0005 differs from its pinned decision.");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, DecisionPath)));
        JsonElement decision = document.RootElement;
        decision.GetProperty("schemaVersion").GetString().ShouldBe(SchemaVersion);
        decision.GetProperty("candidate").GetProperty("adr").GetProperty("sha256").GetString().ShouldBe(AdrSha256);
        decision.GetProperty("digests").GetProperty("adrSha256").GetString().ShouldBe(AdrSha256);
        decision.GetProperty("digests").GetProperty("signablePayloadSha256").GetString()
            .ShouldBe(DecisionSignablePayloadSha256, "DERIVED_KEY_DECISION_DRIFT: the decision payload differs from its pin.");

        foreach (JsonElement section in new[]
        {
            decision.GetProperty("keyFamilies"),
            decision.GetProperty("ownership"),
            decision.GetProperty("writeOrdering").GetProperty("families"),
            decision.GetProperty("rebuild"),
            decision.GetProperty("backfill"),
            decision.GetProperty("deletion"),
            decision.GetProperty("rollback").GetProperty("families"),
        })
        {
            section.EnumerateArray().Select(static row => row.GetProperty("family").GetString()).ToArray()
                .ShouldBe(_families, $"{Violation}: a decision section does not cover exactly the seven families.");
        }

        Type keys = typeof(ConversationProjectionStoreOptions).Assembly.GetType(KeysTypeName, throwOnError: true)!;
        keys.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(static method => method.ReturnType == typeof(string) && method.Name.EndsWith("Key", StringComparison.Ordinal))
            .Select(static method => method.Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(_keyBuilders.Values.Order(StringComparer.Ordinal), $"{Violation}: the runtime key builders differ from the decided families.");
        foreach (JsonElement family in decision.GetProperty("keyFamilies").EnumerateArray())
        {
            string id = family.GetProperty("family").GetString()!;
            string template = family.GetProperty("rule").GetString()!;
            string prefix = family.GetProperty("keyPrefix").GetString()!;
            string firstKey = RuntimeKey(keys, id, _first);
            string secondKey = RuntimeKey(keys, id, _second);

            prefix.ShouldBe(template[..template.IndexOf('<', StringComparison.Ordinal)], $"{Violation}: the decided {id} prefix differs from its template.");
            CommonPrefix(firstKey, secondKey).ShouldBe(prefix, $"{Violation}: the runtime {id} key prefix differs from the decision.");
            firstKey.ShouldBe(Expand(template, _first), $"{Violation}: the runtime {id} key differs from its decided template.");
            secondKey.ShouldBe(Expand(template, _second), $"{Violation}: the runtime {id} key differs from its decided template.");
            bool tenantSegment = family.GetProperty("tenantSegment").GetBoolean();
            (RuntimeKey(keys, id, (_second.Tenant, _first.Conversation, _first.Dispatch)) != firstKey)
                .ShouldBe(tenantSegment, $"{Violation}: {id} tenant isolation differs from the decision.");
        }

        foreach (JsonElement row in decision.GetProperty("ownership").EnumerateArray())
        {
            string id = row.GetProperty("family").GetString()!;
            Rule(row.GetProperty("owner")).ShouldBe("eventstore-derived", $"{Violation}: {id} claims durable domain authority.");
            Rule(row.GetProperty("writer")).ShouldBe("projection-route", $"{Violation}: {id} has a second write authority.");
            Rule(row.GetProperty("readRepair")).ShouldBe("false", $"{Violation}: {id} permits read repair.");
        }

        foreach (JsonElement row in decision.GetProperty("expiry").EnumerateArray())
        {
            string id = row.GetProperty("family").GetString()!;
            Rule(row).ShouldBe(id == "dispatch-ledger" ? "redelivery-window-ttl" : "none", $"DERIVED_KEY_TRANSITION_UNSAFE: {id} expiry changed.");
        }

        RulesOf(decision.GetProperty("compatibility").GetProperty("families")).ShouldAllBe(static rule => rule == "identity");
        JsonElement ordering = decision.GetProperty("writeOrdering");
        ordering.GetProperty("live").EnumerateArray()
            .Select(static step => $"{step.GetProperty("position").GetInt32()}:{step.GetProperty("family").GetString()}:{step.GetProperty("action").GetString()}")
            .ToArray().ShouldBe(_liveSequence, "DERIVED_KEY_TRANSITION_UNSAFE: the accepted-append write order changed.");
        ordering.GetProperty("rebuild").EnumerateArray()
            .Select(static step => $"{step.GetProperty("position").GetInt32()}:{step.GetProperty("family").GetString()}")
            .ToArray().ShouldBe(_rebuildSequence, "DERIVED_KEY_TRANSITION_UNSAFE: the rebuild plan order changed.");
        RulesOf(decision.GetProperty("rebuild")).ShouldAllBe(static rule => rule == "full-replay-fail-closed");
        RulesOf(decision.GetProperty("backfill")).ShouldAllBe(static rule => rule == "none");
        RulesOf(decision.GetProperty("deletion")).ShouldAllBe(static rule => rule == "operator-rebuild-only");
        RulesOf(decision.GetProperty("rollback").GetProperty("families")).ShouldAllBe(static rule => rule == "redeploy-then-rebuild");
        decision.GetProperty("compatibility").GetProperty("grammar").GetProperty("stateStore").GetProperty("configurationKey")
            .GetString().ShouldBe(ConversationProjectionStoreOptions.ConfigurationKey);
    }

    /// <summary>Invokes the internal runtime key builder for one family through its declared parameters.</summary>
    /// <param name="keys">The runtime key-builder type.</param>
    /// <param name="family">The decided family identifier.</param>
    /// <param name="identities">The identities to bind.</param>
    /// <returns>The runtime state key.</returns>
    private static string RuntimeKey(Type keys, string family, (string Tenant, string Conversation, string Dispatch) identities)
    {
        MethodInfo builder = keys.GetMethod(_keyBuilders[family], BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{Violation}: no runtime key builder for {family}.");
        object[] arguments = [.. builder.GetParameters().Select(parameter => parameter.ParameterType switch
        {
            Type type when type == typeof(TenantId) => (object)new TenantId(identities.Tenant),
            Type type when type == typeof(ConversationId) => new ConversationId(identities.Conversation),
            Type type when type == typeof(string) => identities.Dispatch,
            _ => throw new InvalidOperationException($"{Violation}: unexpected {family} key parameter."),
        })];
        return (string)builder.Invoke(null, arguments)!;
    }

    /// <summary>Expands a decided key template with independently computed encodings.</summary>
    /// <param name="template">The decided key template.</param>
    /// <param name="identities">The identities to bind.</param>
    /// <returns>The expected state key.</returns>
    private static string Expand(string template, (string Tenant, string Conversation, string Dispatch) identities)
    {
        string key = template
            .Replace("<seg(tenantId)>", Segment(identities.Tenant), StringComparison.Ordinal)
            .Replace("<seg(conversationId)>", Segment(identities.Conversation), StringComparison.Ordinal)
            .Replace("<sha256(tenantId)>", Digest(identities.Tenant), StringComparison.Ordinal)
            .Replace("<sha256(conversationId)>", Digest(identities.Conversation), StringComparison.Ordinal)
            .Replace("<sha256(dispatchId)>", Digest(identities.Dispatch), StringComparison.Ordinal);
        key.ShouldNotContain('<', $"{Violation}: the template uses an undecided key segment.");
        return key;
    }

    /// <summary>Returns the longest common prefix of two keys.</summary>
    /// <param name="first">The first key.</param>
    /// <param name="second">The second key.</param>
    /// <returns>The shared prefix.</returns>
    private static string CommonPrefix(string first, string second)
    {
        int length = 0;
        while (length < first.Length && length < second.Length && first[length] == second[length])
        {
            length++;
        }

        return first[..length];
    }

    private static string Segment(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string Digest(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string? Rule(JsonElement cell) => cell.GetProperty("rule").GetString();

    private static string?[] RulesOf(JsonElement section) => [.. section.EnumerateArray().Select(Rule)];
}
