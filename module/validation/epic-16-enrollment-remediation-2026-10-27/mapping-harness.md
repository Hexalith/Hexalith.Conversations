# Offline synthetic mapping harness

This disposable helper was built and run in Debug outside the module. It uses
Builds' public materializer and complete generated environment; it is not a
public map-only CLI, ordinary Platform run, owner registration, or qualified
release tool. Its explicit synthetic choice is every already-initialized
root-declared repository from this checkout, with no sibling discovery or
nested initialization. No mapping imports were hand-authored.

The retained `source-mapping.json`, `source-environment.json`,
`package-mapping.json` and `package-environment.json` bind the choices used by
local checks. The `PlatformEnrollmentMappingTest` production-XML fixture is the
maintained source regression. Package mode only evaluated references and Compile
items; it compiled and executed no Release artifacts.

Temporary project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup>
  <ItemGroup><ProjectReference Include="/home/administrator/projects/hexalith/conversations/references/Hexalith.Builds/src/libraries/Hexalith.Builds.Tooling/Hexalith.Builds.Tooling.csproj" /></ItemGroup>
</Project>
```

Temporary program:

```csharp
using System.Text.Json;
using Hexalith.Builds.Tooling.Workspace;
string root = Path.GetFullPath(args[0]);
string workspace = Path.GetFullPath(args[1]);
WorkspaceMode mode = args.Length > 2 && args[2] == "package" ? WorkspaceMode.Package : WorkspaceMode.Source;
List<SourceMappingEntry> entries = [new("Hexalith.Conversations", "source", root)];
foreach (string line in File.ReadAllLines(Path.Combine(root, ".gitmodules")))
{
    string text = line.Trim();
    if (!text.StartsWith("path = ", StringComparison.Ordinal)) continue;
    string relative = text[7..];
    entries.Add(new(Path.GetFileName(relative), mode == WorkspaceMode.Source ? "source" : "package", mode == WorkspaceMode.Source ? Path.Combine(root, relative) : null));
}
SourceMapping mapping = new(mode, root, "Hexalith.Conversations", entries, true);
await SourceMappingMaterializer.WriteAsync(mapping, workspace, CancellationToken.None);
await File.WriteAllTextAsync(Path.Combine(workspace, "environment.json"), JsonSerializer.Serialize(SourceMappingMaterializer.Environment(mapping, workspace), new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(mapping.ContentHash);
```
