using System.Reflection;

namespace Hexalith.Conversations.Server.Tests.ResolutionMetadata;

/// <summary>Observes the actual SDK raw accessor without replacing its envelope resolver.</summary>
public class ResolutionRawReadProxy : DispatchProxy
{
    internal object Inner { get; set; } = null!;
    internal List<(string Key, byte[] Payload)> Reads { get; } = [];
    internal Func<string, Task>? AfterRead { get; set; }
    internal int Writes { get; private set; }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == "get_SupportsTransaction") return targetMethod.Invoke(Inner, args);
        if (targetMethod.Name != "ReadAsync")
        {
            Writes++;
            throw new Xunit.Sdk.XunitException("Native query resolver attempted a raw write.");
        }
        object task = targetMethod.Invoke(Inner, args)!;
        return typeof(ResolutionRawReadProxy).GetMethod(nameof(ObserveAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(targetMethod.ReturnType.GenericTypeArguments[0]).Invoke(this, [task, args![0]!]);
    }

    private async Task<T> ObserveAsync<T>(Task<T> task, string key)
    {
        T entry = await task;
        ReadOnlyMemory<byte> payload = (ReadOnlyMemory<byte>)typeof(T).GetProperty("Value")!.GetValue(entry)!;
        Reads.Add((key, payload.ToArray()));
        if (AfterRead is not null) await AfterRead(key);
        return entry;
    }
}
