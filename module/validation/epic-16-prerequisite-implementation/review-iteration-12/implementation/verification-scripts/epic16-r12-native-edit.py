from pathlib import Path
root=Path.cwd()
p=root/'_bmad/scripts/epic16_native.py';s=p.read_text();s=s.replace("raise HelperUnavailable('repository-approved SDK unavailable')", "raise HelperUnavailable(_process_failure('repository-approved SDK unavailable', sdk))");s=s.replace("raise HelperUnavailable('fixed helper build failed: ' + build.stdout.decode(errors='replace')[-2000:])", "raise HelperUnavailable(_process_failure('fixed helper build failed', build))");s=s.replace("raise HelperUnavailable('helper SDK provenance unavailable')\n    sdk", "raise HelperUnavailable(_process_failure('helper SDK provenance unavailable', selected))\n    sdk")
pos=s.index('\ndef validate_sdk');s=s[:pos]+'''\ndef _process_failure(stage, result):
    """Retain separate bounded diagnostic tails and the actual prerequisite exit."""
    def bounded(value):
        if isinstance(value, bytes): value = value.decode('utf-8', errors='replace')
        return (value or '')[-2000:]
    return f"{stage}: exit={result.returncode}; stdout={bounded(result.stdout)!r}; stderr={bounded(result.stderr)!r}"

'''+s[pos:];p.write_text(s)
p=root/'_bmad/scripts/epic16_native_helper/ProductMetadata.cs';s=p.read_text();pos=s.index('    /// <summary>Excludes only');s=s[:pos]+'''    /// <summary>Admits only an exact approved framework attribute constructor signature.</summary>
    internal static bool IsFrameworkAttribute(MetadataReader reader, CustomAttribute attribute, string name, byte[] signature)
    {
        if (attribute.Constructor.Kind != HandleKind.MemberReference) return false;
        MemberReference constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
        if (constructor.Parent.Kind != HandleKind.TypeReference || reader.GetString(constructor.Name) != ".ctor"
            || !IsFrameworkType(reader, (TypeReferenceHandle)constructor.Parent)) return false;
        TypeReference type = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
        return reader.GetString(type.Namespace) + "." + reader.GetString(type.Name) == name
            && reader.GetBlobBytes(constructor.Signature).AsSpan().SequenceEqual(signature);
    }

'''+s[pos:];p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'_bmad/scripts/epic16_native_helper/Program.cs';s=p.read_text();start=s.index('        TypeReference reference = reader.GetTypeReference((TypeReferenceHandle)handle);');end=s.index('\n    }',start);s=s[:start]+'''        return ProductMetadata.IsFrameworkType(reader, (TypeReferenceHandle)handle);'''+s[end:];start=s.index('            BlobReader blob = reader.GetBlobReader(attribute.Value);',s.index('var versions'));end=s.index('\n        }\n        if (versions.Count',start);s=s[:start]+'''            bool informational = name == "System.Reflection.AssemblyInformationalVersionAttribute";
            byte[] signature = informational ? [0x20, 1, 1, 0x0e] : [0x20, 2, 1, 0x0e, 0x0e];
            if (!ProductMetadata.IsFrameworkAttribute(reader, attribute, name, signature))
                throw new InvalidDataException("Source stamp has no approved framework constructor.");
            BlobReader blob = reader.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1) throw new BadImageFormatException();
            string? key = blob.ReadSerializedString();
            string? value = informational ? key : blob.ReadSerializedString();
            if (blob.ReadUInt16() != 0 || blob.RemainingBytes != 0) throw new BadImageFormatException();
            if (informational) versions.Add(value ?? string.Empty);
            else if (key == "SourceRevisionId") revisions.Add(value ?? string.Empty);'''+s[end:];p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'_bmad/scripts/epic16_native_helper/TestConstruction.cs';s=p.read_text();old='parameter.GetCustomAttributes().Any(h => AttributeName(reader, reader.GetCustomAttribute(h)).EndsWith("]System.ParamArrayAttribute", StringComparison.Ordinal))';assert old in s;s=s.replace(old,'parameter.GetCustomAttributes().Any(h => IsParamArray(reader, reader.GetCustomAttribute(h)))');pos=s.index('    private static IEnumerable<TypeDefinitionHandle> Hierarchy');s=s[:pos]+'''    private static bool IsParamArray(MetadataReader reader, CustomAttribute attribute)
        => ProductMetadata.IsFrameworkAttribute(reader, attribute, "System.ParamArrayAttribute", [0x20, 0, 1])
            && reader.GetBlobBytes(attribute.Value).AsSpan().SequenceEqual(new byte[] { 1, 0, 0, 0 });

'''+s[pos:];p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'_bmad/scripts/epic16_lifecycle.py';s=p.read_text();s=s.replace('deliveries=set();transactions=set();last_order=0','deliveries=set();transactions={stage[\'cas\'][\'transactionId\'] for stage in row[\'atomicity\'][\'stages\']};last_order=0');p.write_text(s)
