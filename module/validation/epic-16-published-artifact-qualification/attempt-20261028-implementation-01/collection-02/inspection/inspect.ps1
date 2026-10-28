
param([string]$AssemblyPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$assembly=[Reflection.Assembly]::LoadFrom($AssemblyPath)
$resources=@()
foreach($name in @($assembly.GetManifestResourceNames() | Sort-Object)) {
 if(-not $name.EndsWith('.json',[StringComparison]::Ordinal)){continue}
 $stream=$assembly.GetManifestResourceStream($name)
 if($stream.Length -gt 8388608){throw 'Embedded resource exceeds 8 MiB'}
 $path=Join-Path $OutputDirectory $name
 $output=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
 try{$stream.CopyTo($output)}finally{$output.Dispose();$stream.Dispose()}
 $resources+=@{name=$name;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$version=@($assembly.GetCustomAttributesData() | Where-Object {$_.AttributeType.Name -ceq 'AssemblyInformationalVersionAttribute'})[0].ConstructorArguments[0].Value
@{informationalVersion=$version;assemblyLocation=$assembly.Location;resources=$resources}|ConvertTo-Json -Depth 5
