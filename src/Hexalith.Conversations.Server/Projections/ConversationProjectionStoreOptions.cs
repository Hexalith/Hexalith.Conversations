using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Configuration;

namespace Hexalith.Conversations.Server.Projections;

/// <summary>Immutable, provider-local binding to the enrolled SDK read-model component.</summary>
public sealed class ConversationProjectionStoreOptions
{
    /// <summary>The full configuration key injected by Platform.</summary>
    public const string ConfigurationKey = "EventStore:Projections:ReadModelStateStoreName";

    /// <summary>Initializes an explicit component binding without changing state-key bytes.</summary>
    /// <param name="stateStoreName">The injected Dapr component name.</param>
    public ConversationProjectionStoreOptions(string stateStoreName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateStoreName);
        StateStoreName = stateStoreName;
    }

    /// <summary>Gets the component used by reads, writes, recovery, batches, and owner proofs.</summary>
    public string StateStoreName { get; }

    /// <summary>
    /// Gets the existing SDK component default for legacy direct-construction callers only.
    /// Enrolled configuration never selects this compatibility path.
    /// </summary>
    internal static ConversationProjectionStoreOptions Legacy { get; } = new(new EventStoreProcessOptions().StateStoreName);

    /// <summary>Resolves the required enrollment input before any store operation.</summary>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The immutable component binding.</returns>
    public static ConversationProjectionStoreOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? value = configuration[ConfigurationKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Enrollment requires '{ConfigurationKey}'.");
        }

        return new(value);
    }
}
