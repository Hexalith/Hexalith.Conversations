from pathlib import Path
import json
p=Path('_bmad/scripts/epic16_native_helper/ProductMetadata.cs');s=p.read_text();at=s.index('    /// <summary>Excludes only')
s=s[:at]+'''    /// <summary>Recognizes the approved .NET 10 framework owner, never a System namespace lookalike.</summary>
    internal static bool IsFrameworkType(MetadataReader reader, TypeReferenceHandle handle)
    {
        TypeReference type = reader.GetTypeReference(handle);
        if (type.ResolutionScope.Kind != HandleKind.AssemblyReference) return false;
        AssemblyReference owner = reader.GetAssemblyReference((AssemblyReferenceHandle)type.ResolutionScope);
        string name = reader.GetString(owner.Name);
        string token = Convert.ToHexString(reader.GetBlobBytes(owner.PublicKeyOrToken));
        return owner.Version == new Version(10, 0, 0, 0) && owner.Flags == 0
            && reader.GetString(owner.Culture) == string.Empty
            && (name == "System.Runtime" && token == "B03F5F7F11D50A3A"
                || name == "System.Private.CoreLib" && token == "7CEC85D7BEA7798E");
    }

''' + s[at:]
s=s.replace('&& reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent.Kind == HandleKind.TypeReference;', '&& reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent.Kind == HandleKind.TypeReference\n                && IsFrameworkType(reader, (TypeReferenceHandle)reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent);')
p.write_bytes(s.replace('\n','\r\n').encode())
p=Path('_bmad/scripts/epic16_native_helper/Program.cs');s=p.read_text().replace('if (!name.StartsWith("System.", StringComparison.Ordinal))','if (!ProductMetadata.IsFrameworkType(reader, (TypeReferenceHandle)type))');s=s.replace('// System attributes are sealed/non-Fact metadata. An unresolved external custom','// Approved framework attributes are non-Fact metadata. An unresolved external custom');p.write_bytes(s.replace('\n','\r\n').encode())
p=Path('_bmad/schemas/epic-16-tooling-v1.schema.json');s=json.loads(p.read_bytes());ack=s['$defs']['AC-16.1-04']['properties']['acknowledgements']['items']['properties'];proof=ack['durableEvidence']['anyOf'][0]['properties']
projections=[proof['sourceState'],proof['before']['properties']['projection'],proof['after']['properties']['projection'],ack['durableCommit']['anyOf'][0]['properties']['persisted']['properties']['projection'],ack['acknowledgement']['anyOf'][0]['properties']['observedRecord']['properties']['projection']]
for state in projections:
 state['properties']['members']=dict(type='object',additionalProperties=dict(type='string',enum=['TenantOwner','TenantContributor','TenantReader']))
 state['properties']['configuration']=dict(type='object',additionalProperties=dict(type='string'))
p.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n')
p=Path('_bmad/scripts/tests/test_epic16_tooling.py');s=p.read_text()
s=s.replace("build_args=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'];build=subprocess.run(build_args", "build_args=['dotnet','build','Evidence.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false']+(['-p:RunAnalyzers=false'] if shape.endswith('conflict') else []);build=subprocess.run(build_args")
# Keep metadata build streams/artifacts when fixture evidence is requested.
s=s.replace("    _PRODUCT_BYTES[key]=binary\n    shutil.rmtree(directory)", "    _PRODUCT_BYTES[key]=binary\n    if os.environ.get('EPIC16_R11_NATIVE_EVIDENCE'):\n        round11_retain('product-build-'+digest(json.dumps(key).encode()), {**{name:(directory/name).read_bytes() for name in ('Product.cs','Product.csproj','Stamp.cs','global.json')}, 'Product.dll.bytes':binary,'build.stdout':result.stdout,'build.stderr':result.stderr}, dict(argv=['dotnet','build','Product.csproj','-c','Debug','--nologo','-m:1','-p:NuGetAudit=false'],cwd=str(directory),exitCode=result.returncode))\n    shutil.rmtree(directory)")
# Update the prior nullable positive's newly bound operation digests only.
s=s.replace("        durable['persistedAfterSha256']=digest(canonical(data['concurrentRecovery']['current']['after']))\n    round7_submit", "        durable['persistedAfterSha256']=digest(canonical(data['concurrentRecovery']['current']['after']))\n        round11_refresh_recovery(data)\n    round7_submit")
p.write_text(s)
