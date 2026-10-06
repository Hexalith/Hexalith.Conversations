# Current Client Public API Baseline

`client-public-api-baseline-v1.json` captures the seven exported types in
`Hexalith.Conversations.Client` after the committed Agents integration at source
revision `e6611b2c6a2f5e3270122bd3515d43f7645be9c4`. It includes the eight Agents
methods on both `IConversationClient` and `ConversationClient`.

The inventory includes every exported type, including exports outside the Client
namespace. It sorts types, interfaces, and declared public CLR members ordinally.
Member signatures include constructors, methods, property/event accessors, fields,
generic constraints, parameter names/defaults, reflected nullability, custom
modifiers such as `IsExternalInit`, and compiler-visible API attributes such as
extension, required-member, and obsolete annotations. It
excludes assembly versions and paths. This is a current API comparison, not a
replacement for the historical Contracts or rc.2 baselines or a release approval.

The Client test project is already selected by `ci.yml`'s `unit-test-projects`.
`PublicClientApiSnapshotTest.CurrentClientApiShouldMatchReviewedBaselineWithoutWriting`
compares the built assembly with this file. Every run writes the observed JSON to
`client-public-api-current.json` in the test output directory for review; it never
writes the committed baseline. The release-tooling test retains the existing
conformance exclusion guard and pins enrollment of the Client unit-test project.
The observed JSON is a local build artifact; the shared CI workflow retains TRX
results rather than this file. Reproduce a failing comparison locally to inspect
the complete observed JSON.

Run the focused check locally:

```bash
dotnet build tests/Hexalith.Conversations.Client.Tests/Hexalith.Conversations.Client.Tests.csproj --configuration Debug -m:1 -p:UseSharedCompilation=false -nr:false
dotnet tests/Hexalith.Conversations.Client.Tests/bin/Debug/net10.0/Hexalith.Conversations.Client.Tests.dll -class Hexalith.Conversations.Client.Tests.PublicClientApiSnapshotTest -noLogo
```

For an intentional API change, review the source change and diff the observed
JSON against the baseline. After explicit approval of that API change, copy the
reviewed output to `client-public-api-baseline-v1.json` and rerun the comparison.
Do not automatically refresh this file in CI or ordinary test execution. Keep
the JSON as UTF-8 with LF line endings.
