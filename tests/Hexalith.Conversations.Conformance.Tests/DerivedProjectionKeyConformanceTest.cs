// <copyright file="DerivedProjectionKeyConformanceTest.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Reflection;
using System.Security.Cryptography;
using System.Text;

using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Conformance.Tests;

/// <summary>Checks runtime projection key families, independent encodings and tenant isolation.</summary>
public sealed class DerivedProjectionKeyConformanceTest
{
    private const string Violation = "PROJECTION_KEY_CONTRACT_VIOLATED";
    private const string KeysTypeName = "Hexalith.Conversations.Server.Projections.ConversationProjectionReadModelKeys";
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

    // Encoders are the only static string methods that do not build a family key.
    private static readonly string[] _keyEncoders = ["EncodeKeySegment", "ResolutionStorageToken"];

    private static readonly (string Id, string Template, string Prefix, bool TenantSegment)[] _expectedFamilies =
    [
        ("conversation", "projection:conversations:<seg(tenantId)>:<seg(conversationId)>", "projection:conversations:", true),
        ("tenant-index", "projection:conversations-index:<seg(tenantId)>", "projection:conversations-index:", true),
        ("dispatch-ledger", "projection:conversations-dispatch:<sha256(dispatchId)>", "projection:conversations-dispatch:", false),
        ("resolution-metadata", "projection:conversations-resolution:<sha256(tenantId)>:<sha256(conversationId)>", "projection:conversations-resolution:", true),
        ("resolution-selection", "projection:conversations-resolution-selection:<sha256(tenantId)>:<sha256(conversationId)>", "projection:conversations-resolution-selection:", true),
        ("resolution-proof", "projection:conversations-resolution-proof:<sha256(tenantId)>:<sha256(conversationId)>", "projection:conversations-resolution-proof:", true),
        ("resolution-fence", "projection:conversations-resolution-fence:<sha256(tenantId)>:<sha256(conversationId)>", "projection:conversations-resolution-fence:", true),
    ];

    // Two identity sets whose encodings differ at their first character, so the common prefix of their runtime
    // keys is exactly the runtime key prefix.
    private static readonly (string Tenant, string Conversation, string Dispatch) _first = ("tenant-α/1", "conversation+1", "dispatch-1");
    private static readonly (string Tenant, string Conversation, string Dispatch) _second = ("Zeta tenant", "Zeta conversation", "other dispatch");

    /// <summary>Every runtime family preserves its reviewed key grammar and tenant boundary.</summary>
    [Fact]
    public void RuntimeProjectionKeysPreserveTheirContract()
    {
        Type keys = typeof(ConversationProjectionStoreOptions).Assembly.GetType(KeysTypeName, throwOnError: true)!;
        keys.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(static method => method.ReturnType == typeof(string) && !method.IsSpecialName)
            .Select(static method => method.Name)
            .Order(StringComparer.Ordinal)
            .ShouldBe(_keyBuilders.Values.Concat(_keyEncoders).Order(StringComparer.Ordinal), $"{Violation}: the runtime key-builder type declares an unknown static string method.");
        foreach ((string id, string template, string prefix, bool tenantSegment) in _expectedFamilies)
        {
            string firstKey = RuntimeKey(keys, id, _first);
            string secondKey = RuntimeKey(keys, id, _second);

            prefix.ShouldBe(template[..template.IndexOf('<', StringComparison.Ordinal)], $"{Violation}: the decided {id} prefix differs from its template.");
            CommonPrefix(firstKey, secondKey).ShouldBe(prefix, $"{Violation}: the runtime {id} key prefix differs from the decision.");
            firstKey.ShouldBe(Expand(template, _first), $"{Violation}: the runtime {id} key differs from its decided template.");
            secondKey.ShouldBe(Expand(template, _second), $"{Violation}: the runtime {id} key differs from its decided template.");
            (RuntimeKey(keys, id, (_second.Tenant, _first.Conversation, _first.Dispatch)) != firstKey)
                .ShouldBe(tenantSegment, $"{Violation}: {id} tenant isolation differs from the decision.");
        }

        ConversationProjectionStoreOptions.ConfigurationKey.ShouldBe("EventStore:Projections:ReadModelStateStoreName");
    }

    /// <summary>Invokes the internal runtime key builder for one family through its declared parameters.</summary>
    /// <param name="keys">The runtime key-builder type.</param>
    /// <param name="family">The decided family identifier.</param>
    /// <param name="identities">The identities to bind.</param>
    /// <returns>The runtime state key.</returns>
    private static string RuntimeKey(Type keys, string family, (string Tenant, string Conversation, string Dispatch) identities)
    {
        MethodInfo builder = keys.GetMethod(_keyBuilders[family], BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
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

}
