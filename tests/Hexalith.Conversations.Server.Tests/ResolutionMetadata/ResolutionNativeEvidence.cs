using System.Security.Cryptography;
using System.Text;
using Hexalith.Conversations.Server.Projections;
using Hexalith.Conversations.Server.Queries;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

internal sealed class ResolutionNativeEvidence(ResolutionMetadataFixture fixture) : IConversationResolutionNativeEvidence
{
    public TimeSpan TenantVerifiedAtOffset { get; set; }
    public bool Available { get; set; } = true;
    public string? Receipt { get; set; }
    public string Catalog { get; set; } = "fixture-catalog";
    /// <summary>Gets the number of native permission samples.</summary>
    public int TenantReads { get; private set; }
    /// <summary>Gets or sets the verification clock override for an individual native permission sample.</summary>
    public Func<int, DateTimeOffset, DateTimeOffset>? TenantVerificationTime { get; set; }
    public int ProjectionReads { get; private set; }
    public Action<int>? OnProjection { get; set; }
    public string Incarnation { get; set; } = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    public NativeSourceProof? LastResource { get; private set; }
    public NativeSourceProof? LastTenant { get; private set; }

    public Task<TenantReadAuthorityResult?> ReadTenantAsync(QueryEnvelope query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int sample = ++TenantReads;
        var health = fixture.Access.Health;
        if (!Available || health.Version is not > 0 || string.IsNullOrWhiteSpace(health.Watermark))
            return Task.FromResult<TenantReadAuthorityResult?>(null);
        var identity = new AggregateIdentity("system", "tenants", query.TenantId);
        var proof = NativeSourceProofCodec.CreateSeed(identity, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
        string digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(health.Watermark)));
        for (int index = 1; index <= health.Version.Value; index++)
            proof = NativeSourceProofCodec.ExtendDigest(proof, identity, index, "tenant-message-" + index, digest);
        LastTenant = proof;
        DateTimeOffset now = fixture.Time.Now + TenantVerifiedAtOffset;
        DateTimeOffset verifiedAt = TenantVerificationTime?.Invoke(sample, now) ?? now;
        return Task.FromResult<TenantReadAuthorityResult?>(new(query.TenantId, query.UserId, !fixture.Access.Denied, proof, verifiedAt));
    }

    public Task<ProjectionCurrentProof?> ReadProjectionAsync(ConversationResolutionMetadataReadModel metadata, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnProjection?.Invoke(++ProjectionReads);
        var selection = fixture.Store.Snapshot<ConversationResolutionNativeSelection>("statestore",
            ConversationProjectionReadModelKeys.ResolutionSelectionKey(metadata.TenantId, metadata.ConversationId));
        if (!Available || selection?.ProjectionOwner.OwnerGeneration != metadata.Digest())
            return Task.FromResult<ProjectionCurrentProof?>(null);
        NativeSourceProof? proof = metadata.NativeSourceProof;
        if (proof is null) return Task.FromResult<ProjectionCurrentProof?>(null);
        proof = proof with { SourceIncarnation = Incarnation };
        LastResource = proof;
        return Task.FromResult<ProjectionCurrentProof?>(new(proof, new(selection.ProjectionOwner, "fixture-receipt", Receipt ?? metadata.RequestFingerprint), Catalog));
    }
}
