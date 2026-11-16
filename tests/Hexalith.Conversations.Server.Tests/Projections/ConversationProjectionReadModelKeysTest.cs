using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Projections;

namespace Hexalith.Conversations.Server.Tests.Projections;

/// <summary>
/// Pins the persisted format of every ADR 0005 derived-key family to literal keys.
/// </summary>
/// <remarks>
/// ADR 0005 migrates v1 keys by identity, so a changed builder orphans existing derived state. The expected keys are
/// literals computed from the decided templates, never by the builders under test. The decision-artifact checks stay in
/// the BMAD-only <c>DerivedKeyLifecycleAdrValidationTest</c>; this test keeps the product guard in CI.
/// </remarks>
public sealed class ConversationProjectionReadModelKeysTest
{
    private const string Dispatch = "dispatch-1";

    // SHA-256 tokens of the tenant and conversation below, as the resolution families persist them.
    private const string TenantToken = "af0ffa9f8b80ed3a6a6dd07527db6efcd8a3d47025ba6538b607affeb9410a2a";
    private const string ConversationToken = "6fe2b2e038a38d2f89b840b1fa6e332ec81be74a0394b28e37e70ce377065a19";

    // The tenant's UTF-8 bytes encode to base64 containing '+', '/' and '=' padding, exercising every base64url rule.
    private static readonly TenantId _tenant = new("tenant>>>???\u03B1");
    private static readonly TenantId _otherTenant = new("tenant-b");
    private static readonly ConversationId _conversation = new("conversation+1");

    /// <summary>Every derived-key family keeps its decided persisted format.</summary>
    [Fact]
    public void DerivedKeysShouldKeepTheirPersistedFormat()
    {
        ConversationProjectionReadModelKeys.ConversationKey(_tenant, _conversation)
            .ShouldBe("projection:conversations:dGVuYW50Pj4-Pz8_zrE:Y29udmVyc2F0aW9uKzE");
        ConversationProjectionReadModelKeys.TenantIndexKey(_tenant)
            .ShouldBe("projection:conversations-index:dGVuYW50Pj4-Pz8_zrE");
        ConversationProjectionReadModelKeys.DispatchLedgerKey(Dispatch)
            .ShouldBe("projection:conversations-dispatch:539ed001eef41a4c78b4182d5dfa70e3fcae1d01d368c989f9f680072df2ce76");
        ConversationProjectionReadModelKeys.ResolutionMetadataKey(_tenant, _conversation)
            .ShouldBe($"projection:conversations-resolution:{TenantToken}:{ConversationToken}");
        ConversationProjectionReadModelKeys.ResolutionSelectionKey(_tenant, _conversation)
            .ShouldBe($"projection:conversations-resolution-selection:{TenantToken}:{ConversationToken}");
        ConversationProjectionReadModelKeys.ResolutionProofKey(_tenant, _conversation)
            .ShouldBe($"projection:conversations-resolution-proof:{TenantToken}:{ConversationToken}");
        ConversationProjectionReadModelKeys.ResolutionFenceKey(_tenant, _conversation)
            .ShouldBe($"projection:conversations-resolution-fence:{TenantToken}:{ConversationToken}");
    }

    /// <summary>Every tenant-scoped family resolves another key for another tenant; the dispatch ledger has no tenant segment.</summary>
    [Fact]
    public void TenantScopedKeysShouldDifferAcrossTenants()
    {
        Func<TenantId, string>[] builders =
        [
            tenant => ConversationProjectionReadModelKeys.ConversationKey(tenant, _conversation),
            ConversationProjectionReadModelKeys.TenantIndexKey,
            tenant => ConversationProjectionReadModelKeys.ResolutionMetadataKey(tenant, _conversation),
            tenant => ConversationProjectionReadModelKeys.ResolutionSelectionKey(tenant, _conversation),
            tenant => ConversationProjectionReadModelKeys.ResolutionProofKey(tenant, _conversation),
            tenant => ConversationProjectionReadModelKeys.ResolutionFenceKey(tenant, _conversation),
        ];
        foreach (Func<TenantId, string> build in builders)
        {
            build(_otherTenant).ShouldNotBe(build(_tenant));
        }
    }
}
