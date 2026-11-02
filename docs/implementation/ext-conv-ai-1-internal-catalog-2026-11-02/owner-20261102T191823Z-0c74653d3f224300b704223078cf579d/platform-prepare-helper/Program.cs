using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Hexalith.Builds.Tooling.Workspace;

string binaries = "/home/administrator/projects/hexalith/conversations/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Module.Cli/bin/Debug/net10.0";
AssemblyLoadContext.Default.Resolving += (context, name) => File.Exists(Path.Combine(binaries, name.Name + ".dll"))
    ? context.LoadFromAssemblyPath(Path.Combine(binaries, name.Name + ".dll")) : null;
SourceMapping producer = SourceMapping.LoadVerified(args[0], args[1]);
SourceMapping current = await WorkspaceRootResolver.ResolveAsync(Path.Combine(args[2], "hexalith-platform.json"), WorkspaceMode.Source, CancellationToken.None);
SourceMapping mapping = producer with
{
    Root = args[2], ActiveModule = "Hexalith.Platform",
    Entries = producer.Entries.Select(entry => entry.Identity == "Hexalith.Platform" ? entry with { Path = args[2] } : entry).ToArray(),
    PlatformIdentity = current.PlatformIdentity, IdentityFindings = current.IdentityFindings,
};
await SourceMappingMaterializer.WriteAsync(mapping, args[3], CancellationToken.None);
File.WriteAllBytes(args[4], JsonSerializer.SerializeToUtf8Bytes(new { workspace = args[3], mappingHash = mapping.ContentHash,
    environment = SourceMappingMaterializer.Environment(mapping, args[3]), platformIdentity = mapping.PlatformIdentity,
    identityFindings = mapping.IdentityFindings }));
Console.WriteLine(mapping.ContentHash);
