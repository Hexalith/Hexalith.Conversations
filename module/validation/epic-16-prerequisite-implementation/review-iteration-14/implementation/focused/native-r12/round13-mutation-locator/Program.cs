using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Hexalith.Conversations.Tooling;
using var pe = new PEReader(File.OpenRead(args[0])); var reader=pe.GetMetadataReader(); var names=new SignatureNames();
int Offset(BlobHandle h) => pe.PEHeaders.MetadataStartOffset+reader.GetHeapMetadataOffset(HeapIndex.Blob)+MetadataTokens.GetHeapOffset(h);
var attributes=new List<object>(); var constants=new List<object>(); var methods=new List<object>();
foreach(var h in reader.CustomAttributes) { var a=reader.GetCustomAttribute(h); if(a.Constructor.Kind!=HandleKind.MemberReference) continue; var c=reader.GetMemberReference((MemberReferenceHandle)a.Constructor); attributes.Add(new{type=names.Name(reader,c.Parent),signature=Convert.ToHexString(reader.GetBlobBytes(c.Signature)),signatureOffset=Offset(c.Signature),value=Convert.ToHexString(reader.GetBlobBytes(a.Value)),valueOffset=Offset(a.Value)}); }
foreach(var h in reader.TypeDefinitions) { var t=reader.GetTypeDefinition(h); foreach(var mh in t.GetMethods()) { var m=reader.GetMethodDefinition(mh); methods.Add(new{type=names.Name(reader,h),name=reader.GetString(m.Name),signature=Convert.ToHexString(reader.GetBlobBytes(m.Signature)),signatureOffset=Offset(m.Signature)}); foreach(var ph in m.GetParameters()) { var p=reader.GetParameter(ph); var ch=p.GetDefaultValue(); if(ch.IsNil) continue; var c=reader.GetConstant(ch); constants.Add(new{type=names.Name(reader,h),parameter=reader.GetString(p.Name),kind=c.TypeCode.ToString(),offset=Offset(c.Value),value=Convert.ToHexString(reader.GetBlobBytes(c.Value))}); } } }
var types=reader.TypeDefinitions.Select(h=>new{name=names.Name(reader,h),coded=MetadataTokens.GetRowNumber(h)*4}).Concat(reader.TypeReferences.Select(h=>new{name=names.Name(reader,h),coded=MetadataTokens.GetRowNumber(h)*4+1})).ToArray();
var nested=new{offset=pe.PEHeaders.MetadataStartOffset+reader.GetTableMetadataOffset(TableIndex.NestedClass),size=reader.GetTableRowSize(TableIndex.NestedClass),rows=reader.GetTableRowCount(TableIndex.NestedClass)};
Console.Write(JsonSerializer.Serialize(new{attributes,constants,methods,types,nested}));