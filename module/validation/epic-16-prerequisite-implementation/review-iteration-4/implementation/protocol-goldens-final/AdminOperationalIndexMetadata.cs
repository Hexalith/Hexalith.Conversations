using System.Text.Json.Serialization;
using Hexalith.EventStore.Contracts.Projections;
namespace Hexalith.EventStore.DomainService;
public static class AdminOperationalIndexMetadata {
    public sealed record Response(IReadOnlyList<DomainMetadata> Domains) {
        /// <summary>Gets the optional named dispatch protocol version.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DispatchVersion { get; init; }

        /// <summary>Gets the optional named dispatch capability marker.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DispatchCapability { get; init; }

        /// <summary>Gets the DAPR app id bound to the named route catalog.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? AppId { get; init; }

        /// <summary>Gets the domain-service version bound to the named route catalog.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ServiceVersion { get; init; }

        /// <summary>Gets the deterministic fingerprint of the bound named route catalog.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CatalogFingerprint { get; init; }
    }

    /// <summary>Operational metadata for a single domain.</summary>
    /// <param name="Domain">The kebab-case domain name.</param>
    /// <param name="EventTypes">Full names of the domain's event payload types.</param>
    /// <param name="RejectionEventTypes">Full names of the domain's rejection event types.</param>
    /// <param name="CommandTypes">Full names of the domain's command types.</param>
    /// <param name="AggregateTypes">Full names of the domain's aggregate types.</param>
    /// <param name="ProjectionNames">The domain's projection names.</param>
    /// <param name="QueryTypes">The query types the domain serves via <see cref="IDomainQueryHandler"/>.</param>
    public sealed record DomainMetadata(
        string Domain,
        IReadOnlyList<string> EventTypes,
        IReadOnlyList<string> RejectionEventTypes,
        IReadOnlyList<string> CommandTypes,
        IReadOnlyList<string> AggregateTypes,
        IReadOnlyList<string> ProjectionNames,
        IReadOnlyList<string> QueryTypes) {
        /// <summary>Gets explicitly enrolled read-only source-guard proof descriptors by exact named route.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, ProjectionSourceGuardCapability>? SourceGuardCapabilities { get; init; }

        /// <summary>Gets optional opaque host recovery bindings tied to the unchanged catalog identity.</summary>
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, ProjectionReplayRecoveryBinding>? ReplayRecoveryBindings { get; init; }
        /// <summary>Gets the exact canonical named asynchronous projection types for this domain.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? NamedProjectionTypes { get; init; }

        /// <summary>Gets named routes that accept ordinary delivery; an empty set keeps rebuild-only routes available.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? OrdinaryProjectionTypes { get; init; }

        /// <summary>Gets named routes that support aggregate-owned full replay for one owner.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? FullReplayProjectionTypes { get; init; }

        /// <summary>Gets streamed full-replay routes whose domain has no legacy projection handler.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? MappedOnlyStreamedProjectionTypes { get; init; }

        /// <summary>Gets full-replay routes that support contiguous ordinary suffix delivery.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? IncrementalReplayProjectionTypes { get; init; }

        /// <summary>Gets routes whose normal delivery uses the fenced shared epoch journal.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? SharedEpochProjectionTypes { get; init; }

        /// <summary>Gets named routes whose normal handler safely accepts repeated individual source events.</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? SingleEventProjectionTypes { get; init; }
    }
}
