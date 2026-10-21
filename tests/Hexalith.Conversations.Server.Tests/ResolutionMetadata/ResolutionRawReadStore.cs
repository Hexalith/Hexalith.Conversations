using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Testing.Fakes;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

/// <summary>Isolated reflection bridge to the real native fake accessor and production envelope resolver.</summary>
internal sealed class ResolutionRawReadStore(InMemoryReadModelStore inner) : IReadModelStore
{
    internal ResolutionRawReadProxy Observer { get; private set; } = null!;
    internal int Writes { get; private set; }
    internal Func<string, Task>? AfterRawRead { get; set; }

    public Task<ReadModelEntry<T>> GetAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => ReadAsync<T>(storeName, key, false, cancellationToken);

    public Task<ReadModelEntry<T>> GetCompactedAsync<T>(string storeName, string key, CancellationToken cancellationToken = default) where T : class
        => ReadAsync<T>(storeName, key, true, cancellationToken);

    private async Task<ReadModelEntry<T>> ReadAsync<T>(string storeName, string key, bool compacted, CancellationToken cancellationToken) where T : class
    {
        Type accessorType = typeof(InMemoryReadModelStore).GetNestedType("InMemoryBatchAccessor", BindingFlags.NonPublic)!;
        Type contract = typeof(IReadModelStore).Assembly.GetType("Hexalith.EventStore.Client.Projections.IReadModelBatchStateAccessor")!;
        Observer ??= (ResolutionRawReadProxy)DispatchProxy.Create(contract, typeof(ResolutionRawReadProxy));
        Observer.Inner = Activator.CreateInstance(accessorType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, args: [inner, storeName], culture: null)!;
        Observer.AfterRead = AfterRawRead;
        Type protocol = typeof(IReadModelStore).Assembly.GetType("Hexalith.EventStore.Client.Projections.ReadModelBatchProtocol")!;
        Task resolution;
        try
        {
            resolution = (Task)protocol.GetMethod(compacted ? "ReadCompactedAsync" : "ResolveVisibleAsync",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [Observer, key, cancellationToken, null])!;
            await resolution;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
        {
            throw exception.InnerException;
        }
        object entry = resolution.GetType().GetProperty("Result")!.GetValue(resolution)!;
        Type entryType = entry.GetType();
        bool exists = (bool)entryType.GetProperty("Exists")!.GetValue(entry)!;
        ReadOnlyMemory<byte> bytes = (ReadOnlyMemory<byte>)entryType.GetProperty("Value")!.GetValue(entry)!;
        return exists ? new(JsonSerializer.Deserialize<T>(bytes.Span, JsonSerializerOptions.Web),
            (string)entryType.GetProperty("ETag")!.GetValue(entry)!) : new(null, null);
    }

    public Task SaveAsync<T>(string storeName, string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        Writes++;
        throw new Xunit.Sdk.XunitException("Native read attempted a write.");
    }

    public Task<bool> TrySaveAsync<T>(string storeName, string key, T value, string etag, CancellationToken cancellationToken = default) where T : class
    {
        Writes++;
        throw new Xunit.Sdk.XunitException("Native read attempted a conditional write.");
    }
}
