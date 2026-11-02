from pathlib import Path
import subprocess
r=Path('/home/administrator/projects/hexalith/conversations');d=Path('/tmp/conversations-catalog-review-fixes/historical');d.mkdir(exist_ok=True)
s=subprocess.run(['git','show','7c77a41e8bcdcb00fa1bcb070e19027ad0a98a25:src/Hexalith.Conversations/Aggregates/ConversationAggregate.Agents.cs'],cwd=r,capture_output=True,text=True,check=True).stdout
start=s.index('    private static string PostFingerprint(');end=s.index('    private static DomainResult AgentSuccess',start);code=s[start:end].replace('private static string PostFingerprint','public static string PostFingerprint')
(d/'HistoricalPostFingerprint.cs').write_text('using System.Text.Json;\nusing Hexalith.Conversations.Contracts.Commands;\nusing Hexalith.Conversations.Idempotency;\ninternal static class HistoricalPostFingerprint\n{\n'+code+'}\n')
(d/'Program.cs').write_text('''using Hexalith.Conversations.Contracts.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Contracts.Versioning;
var at = new DateTimeOffset(2026,11,2,12,0,0,TimeSpan.Zero);
var request = new AppendMessageCommand(new(SchemaVersion.Current,new("tenant-one"),new("party-one"),"correlation-one","causation-one","retry-one"),
 new("conversation-one"),new("message-one"),new("party-one"),"original-text",new("provider-one","model",SchemaVersion.Current),
 new(SchemaVersion.Current,ClientName:"client-one"),new("trace-one",true,false),at);
Console.WriteLine(HistoricalPostFingerprint.PostFingerprint(request,at));
''')
outputs=r/'tests/Hexalith.Conversations.Server.Tests/bin/Debug/net10.0'
refs=''.join(f'<Reference Include="{name}" HintPath="{outputs}/{name}.dll" />' for name in ['Hexalith.Conversations','Hexalith.Conversations.Contracts','Hexalith.Commons.Serialization'])
(d/'Historical.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup>'+refs+'</ItemGroup></Project>')
(d/'baseline-source.cs.txt').write_text(s)
