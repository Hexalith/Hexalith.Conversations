
param([string]$AssemblyPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'

function Get-ContainedResourcePath([string]$Directory, [string]$Name) {
 if([string]::IsNullOrWhiteSpace($Name) -or $Name.Contains('..') -or $Name.IndexOfAny([char[]]'/\:') -ge 0 -or [IO.Path]::IsPathRooted($Name)) { throw 'Invalid embedded resource name' }
 $root=[IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar)+[IO.Path]::DirectorySeparatorChar
 $path=[IO.Path]::GetFullPath([IO.Path]::Combine($root,$Name))
 if(-not $path.StartsWith($root,[StringComparison]::Ordinal)) { throw 'Embedded resource path escape' }
 return $path
}

$assembly=[Reflection.Assembly]::LoadFrom($AssemblyPath)
$resources=@()
foreach($name in @($assembly.GetManifestResourceNames() | Sort-Object)) {
 if(-not $name.EndsWith('.json',[StringComparison]::Ordinal)){continue}
 $path=Get-ContainedResourcePath $OutputDirectory $name
 $stream=$assembly.GetManifestResourceStream($name)
 if($stream.Length -gt 8388608){throw 'Embedded resource exceeds 8 MiB'}
 $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
 try{$stream.CopyTo($output)}finally{$output.Dispose();$stream.Dispose()}
 $resources+=@{name=$name;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$version=@($assembly.GetCustomAttributesData() | Where-Object {$_.AttributeType.Name -ceq 'AssemblyInformationalVersionAttribute'})[0].ConstructorArguments[0].Value
@{informationalVersion=$version;assemblyLocation=$assembly.Location;resources=$resources}|ConvertTo-Json -Depth 5
