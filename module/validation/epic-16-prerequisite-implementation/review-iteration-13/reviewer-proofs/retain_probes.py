from pathlib import Path
import json
import hashlib
import subprocess
import sys
ROOT = Path('/home/administrator/projects/hexalith/conversations')
OUT = Path(__file__).resolve().parent
REVISION = 'a' * 40
BUILD = ['dotnet', 'build', 'Product.csproj', '-c', 'Debug', '--nologo', '-m:1', '-p:NuGetAudit=false']
PROJECT = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><GenerateAssemblyInfo>false</GenerateAssemblyInfo><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings><AssemblyName>Probe</AssemblyName></PropertyGroup></Project>'
PAIRS = {
    'private-struct-layout': (
        'public struct Api { private int value; public void Existing() {} }',
        'public struct Api { private long value; public void Existing() {} }'),
    'interface-method-mapping': (
        'public interface I { void Existing() {} } public class Api:I { void I.Existing() {} }',
        'public interface I { void Existing() {} } public class Api:I {}'),
}
def retain(command, directory, prefix):
    result = subprocess.run(command, cwd=directory, capture_output=True, timeout=180)
    (directory / (prefix + '.stdout')).write_bytes(result.stdout)
    (directory / (prefix + '.stderr')).write_bytes(result.stderr)
    (directory / (prefix + '.command.json')).write_text(json.dumps(dict(argv=command,cwd=str(directory),exitCode=result.returncode),indent=2)+'\n')
    if result.returncode:
        raise RuntimeError(str(directory / (prefix + '.stderr')))
    return result
for name in ('constructor-default', 'return-kind', 'collection-blob'):
    directory = OUT / name
    info = json.loads((directory / 'provenance.json').read_text())
    valid = (directory / 'valid.dll.bytes').read_bytes()
    before = bytes.fromhex(info['mutation']['beforeHex'])
    after = bytes.fromhex(info['mutation']['afterHex'])
    assert valid.count(before) == 1
    assert valid.replace(before, after) == (directory / 'mutated.dll.bytes').read_bytes()
    for phase in ('valid', 'mutated'):
        command = [sys.executable, str(OUT / 'metadata_inspect.py'), 'test', str(directory / (phase + '.dll.bytes')), '--revision', REVISION, '--type', 'Synthetic.TestClass', '--method', 'Scenario01']
        retain(command, directory, phase + '.inspection')
    print(name + ': exact retained mutation verified; both original byte sets accepted by metadata inspector')
for name, sources in PAIRS.items():
    values = []
    directory = OUT / name
    directory.mkdir(exist_ok=True)
    for phase, source in zip(('baseline', 'candidate'), sources):
        build_directory = directory / phase
        build_directory.mkdir(exist_ok=True)
        (build_directory / 'global.json').write_bytes((ROOT / 'global.json').read_bytes())
        (build_directory / 'Product.csproj').write_text(PROJECT)
        (build_directory / 'Product.cs').write_text(source)
        (build_directory / 'Stamp.cs').write_text('[assembly: System.Reflection.AssemblyInformationalVersion("1.0.0+' + REVISION + '")]')
        retain(BUILD, build_directory, 'build')
        binary = (build_directory / 'bin/Debug/net10.0/Probe.dll').read_bytes()
        artifact = build_directory / 'Probe.dll.bytes'
        artifact.write_bytes(binary)
        command = [sys.executable, str(OUT / 'metadata_inspect.py'), 'product', str(artifact), '--revision', REVISION]
        result = retain(command, build_directory, 'inspection')
        values.append(json.loads(result.stdout))
    result = dict(apiEqual=values[0]['api'] == values[1]['api'],revision=REVISION,assemblyName='Probe',scope='Same-source reproduction of the existing original probe; original ephemeral binary bytes were deleted by round9_compile_product and were not retained.',originalCompilerCommand=BUILD,originalSources=dict(zip(('baseline','candidate'),sources)))
    (directory / 'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
    assert result['apiEqual']
    print(name + ': API inventories equal in retained same-source reproduction')
files = {}
for path in OUT.rglob('*'):
    if path.is_file() and not path.name.startswith('retention.') and path.name != 'sha256-manifest.json':
        files[str(path.relative_to(OUT))] = hashlib.sha256(path.read_bytes()).hexdigest()
(OUT / 'sha256-manifest.json').write_text(json.dumps(files,indent=2,sort_keys=True)+'\n')
