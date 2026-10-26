from pathlib import Path
root=Path.cwd();p=root/'_bmad/scripts/epic16_native_helper/SignatureNames.cs';s=p.read_text()
s=s.replace('''    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
    {''','''    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => WithKind(DefinitionName(reader, handle), rawTypeKind);
    /// <summary>Names a declaration without a signature-only class/value marker.</summary>
    private string DefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
    {''').replace('GetTypeFromDefinition(reader, type.GetDeclaringType(), 0)', 'DefinitionName(reader, type.GetDeclaringType())')
s=s.replace('''    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
    {''','''    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => WithKind(ReferenceName(reader, handle), rawTypeKind);
    /// <summary>Names a referenced declaration independently from its use in a signature.</summary>
    private string ReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {''').replace('GetTypeFromReference(reader, (TypeReferenceHandle)type.ResolutionScope, 0)', 'ReferenceName(reader, (TypeReferenceHandle)type.ResolutionScope)')
s=s.replace('=> reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);','=> WithKind(reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext), rawTypeKind);')
s=s.replace('GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0)', 'DefinitionName(reader, (TypeDefinitionHandle)handle)').replace('GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0)', 'ReferenceName(reader, (TypeReferenceHandle)handle)')
s=s.replace('    /// <summary>Retains calling convention, generic arity, return and parameter shape.</summary>', '''    /// <summary>Retains encoded class/value kind; unknown is used only where metadata supplies no kind.</summary>
    private static string WithKind(string name, byte rawTypeKind) => rawTypeKind switch
    {
        (byte)SignatureTypeKind.Unknown => name,
        (byte)SignatureTypeKind.Class => "class " + name,
        (byte)SignatureTypeKind.ValueType => "valuetype " + name,
        _ => throw new InvalidDataException("Unsupported signature type kind."),
    };
    /// <summary>Retains calling convention, generic arity, return and parameter shape.</summary>''')
p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'_bmad/scripts/epic16_native_helper/README.md';s=p.read_text();s+='''
Decoded signatures retain the ECMA-335 class/value-type marker for referenced,
defined and specified types, including parameter/return, property, field, generic
and by-reference uses. Declaration names stay separate and unprefixed for required
surface lookup and attribute identity. A referenced type changing from class to
struct therefore changes the supported API contract even when its qualified name
and owning assembly name are unchanged.
''';p.write_text(s)
p=root/'_bmad/scripts/tests/test_epic16_tooling.py';s=p.read_text();assert 'def round9_referenced_product' not in s;s+='''


def round9_referenced_product(directory, kind, revision, surface, *, added=False):
    external=directory/'external';api=directory/'api';external.mkdir(parents=True);api.mkdir()
    (directory/'global.json').write_bytes((ROOT/'global.json').read_bytes())
    (external/'External.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>External</AssemblyName></PropertyGroup></Project>')
    (external/'Value.cs').write_text('namespace External; public '+kind+' Value {}')
    (api/'Api.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>Probe.Api</AssemblyName><GenerateAssemblyInfo>false</GenerateAssemblyInfo><Nullable>disable</Nullable></PropertyGroup><ItemGroup><ProjectReference Include="../external/External.csproj" /></ItemGroup></Project>')
    (api/'Surface.cs').write_text('namespace Probe; public class Surface { '+surface+(' public int Added() => 1;' if added else '')+' }')
    (api/'Stamp.cs').write_text('[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+'+revision+'")]')
    argv=['dotnet','build','Api.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']
    result=subprocess.run(argv,cwd=api,capture_output=True,timeout=120)
    for name,raw in [('stdout',result.stdout),('stderr',result.stderr)]:(directory/('build.'+name)).write_bytes(raw)
    (directory/'build-command.json').write_text(json.dumps(dict(argv=argv,cwd=str(api),exitCode=result.returncode),indent=2)+'\\n')
    assert result.returncode==0,result.stdout.decode()
    binary=(api/'bin/Debug/net10.0/Probe.Api.dll').read_bytes();metadata=tooling.native_helper.inspect_product(binary,revision)
    (directory/'metadata.json').write_text(json.dumps(metadata,indent=2)+'\\n')
    assert 'Probe.Surface' in metadata['declaredTypes'] and 'Probe.Surface' in metadata['api']['types']
    return metadata


@pytest.mark.parametrize('shape',[
    'public External.Value Get() => default;',
    'public void Consume(External.Value value) {}',
    'public External.Value Item { get; set; }',
    'public External.Value Item;',
    'public System.Collections.Generic.List<External.Value> Get() => default;',
    'public ref External.Value Echo(ref External.Value value) => ref value;',
])
def test_round9_referenced_class_value_signature_change_cannot_pass(tmp_path,shape):
    before=round9_referenced_product(tmp_path/'baseline','class','a'*40,shape)
    after=round9_referenced_product(tmp_path/'candidate','struct','b'*40,shape)
    assert before['api']!=after['api']
    with pytest.raises(tooling.Stop) as error:tooling.products.additive(before,after,tooling.need,'external class/value signature')
    assert error.value.code=='UPSTREAM_API_DRIFT'


@pytest.mark.parametrize('kind',['class','struct'])
def test_round9_referenced_same_kind_concrete_addition_remains_supported(tmp_path,kind):
    surface='public External.Value Echo(External.Value value) => value; public External.Value Item { get; set; } public class Nested {}'
    before=round9_referenced_product(tmp_path/'baseline',kind,'a'*40,surface)
    after=round9_referenced_product(tmp_path/'candidate',kind,'b'*40,surface,added=True)
    assert 'Probe.Surface+Nested' in after['declaredTypes']
    assert before['api']!=after['api']
    tooling.products.additive(before,after,tooling.need,'unchanged external kind with concrete addition')


@pytest.mark.parametrize('kind,marker',[('class','class'),('struct','valuetype')])
def test_round9_defined_signature_kind_keeps_declaration_lookup_plain(kind,marker):
    product=dict(assemblyName='Synthetic.Api',surfaces=['Synthetic.Surface'])
    source='namespace Synthetic; public class Container { public '+kind+' Value {} } public class Surface { public Container.Value Echo(Container.Value value) => value; }'
    metadata=tooling.native_helper.inspect_product(round9_compile_product(product,'a'*40,source),'a'*40)
    assert 'Synthetic.Surface' in metadata['declaredTypes'] and 'Synthetic.Container+Value' in metadata['declaredTypes']
    member=next(m for m in metadata['api']['types']['Synthetic.Surface']['members'] if m.startswith('method:Echo:'))
    assert member.count(marker+' Synthetic.Container+Value')==2
''';p.write_text(s)
