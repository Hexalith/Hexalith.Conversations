The retained `ReadModelBatchCanonicalJson.cs` is an exact source fixture from
Hexalith.EventStore, `src/Hexalith.EventStore.Client/Projections/ReadModelBatchCanonicalJson.cs`.
Its SHA-256 is `1f9430e33cefaa51757c36f27d57ed509a31d08aa5f35b05a4159b7d806362f9`.
Copyright Hexalith contributors; MIT license, retained in `EventStore-LICENSE`.
The fixed helper compiles this fixture in an isolated temporary directory; default
Python tests do not access a dependency checkout. Runtime acceptance separately
hashes the clean, candidate-pinned direct EventStore source.

`Program.cs` accepts only the fixed oracle and inspection operations described here.
It reads PE/CLI metadata and method IL structure without loading or executing the
evidence assembly. No artifact can supply a command, compiler option or path.

The metadata protocol pins the actual centrally governed xUnit 4.0.2 identity:
`xunit.v3.core, Version=4.0.2.0, Culture=neutral, PublicKeyToken=8d05b1bb7a6fdb6c`.
The selected class is public, nonabstract and nongeneric, with exactly one public
constructor (including xUnit fixture/output-helper injection constructors).
The selected method is public, nongeneric, nonabstract and instance, takes no
parameters, returns void/Task/ValueTask, and has one plain external Xunit.Fact
attribute with the genuine framework identity. Compiler-supplied caller-source
arguments are accepted; skip/explicit/conditional named overrides, local fake
attributes and wrong framework identities are rejected. Neither constructors,
methods, state machines nor attributes from evidence binaries execute.

Helper directory creation, source copying, build and execution failures return
structured prerequisite-unavailable diagnostics. The consumer compares captured
caller and loaded-local-dependency/helper bytes with bound repository bytes
before claiming them as executed tooling.

The fixed `partyId` operation applies the exact source-defined
`ArgumentException.ThrowIfNullOrWhiteSpace`, `Trim()` and `ToLowerInvariant()`
operations from Conversations `PartyId`. The attributed source and actual native
constructor/JSON wire goldens are retained in the local domain-source and protocol
fixtures. This operation does not normalize opaque non-Party identifiers or raw
payload bytes. Managed metadata additionally rejects void-returning methods with
an async state-machine marker, without executing their bodies.

The `opaqueId` test operation invokes native `ThrowIfNullOrWhiteSpace` without
normalizing its result. The `whitespace` operation enumerates all UTF-16 code units
classified by native `Char.IsWhiteSpace`; the Python predicate compares the exact
set in a regression. These fixed operations are protocol oracles only, and accept
no executable command or evidence-assembly invocation.

The helper reads the repository's exact `global.json` with the contained reader,
copies those bytes into its isolated workspace, and independently verifies the
selected SDK satisfies its explicit `latestPatch` policy. That policy joins the
executing-source inventory. Missing approved SDKs and incompatible selected SDKs
produce `NATIVE_HELPER_UNAVAILABLE`; malformed policy is invalid input. The fixed
`provenance` operation reports its actual runtime, target framework and core-library
identity. Local build provenance retains the selected SDK, policy digest, actual
helper binary and runtime configuration; it remains separate from portable native
capture/final-record identities, so relocating evidence under the same approved
policy does not rewrite records. No policy or artifact can supply executable options.


`ProductMetadata.cs` and `SignatureNames.cs` inspect each required owned product
assembly with PEReader/MetadataReader. They report its actual assembly name,
version, culture/public key, MVID, source-stamped informational version, declared
types and complete supported public/protected declaration inventory. The exact
.NET SDK GenerateAssemblyInfo convention appends SourceRevisionId after `+`, or
`.` if informational metadata already contains `+`; both forms are admitted.
An optional AssemblyMetadata SourceRevisionId must agree. Evidence assemblies,
their attributes and their commands are never loaded or executed by the consumer.

The API comparison is conservative ECMA-335 declaration preservation, not general
binary/behavioral compatibility. It retains type visibility/base/interfaces,
generic constraints, method calling shape and flags, parameter names/defaults/
attributes, return signatures, property/accessor contracts, fields/constants and
events. Every baseline member must survive. Concrete members may be added; new
abstract methods/accessors may not impose implementor obligations. Names containing
Abstract are ordinary names. Custom attributes are retained conservatively, so
some equivalent recompilations can fail. PInvoke, native marshalling, field RVAs
and exported/forwarded types fail as unsupported rather than being silently
omitted. The exact global.json still governs compilation of the fixed inspector.

Decoded signatures retain the ECMA-335 class/value-type marker for referenced,
defined and specified types, including parameter/return, property, field, generic
and by-reference uses. Declaration names stay separate and unprefixed for required
surface lookup and attribute identity. A referenced type changing from class to
struct therefore changes the supported API contract even when its qualified name
and owning assembly name are unchanged.

Iteration 10 retains complete referenced assembly name/version/culture/signing
bytes and flags in signatures, and the custom attributes on each generic
constraint (including nullable constraint annotations). Both test and product
inspectors accept the SDK's direct and existing-metadata SourceRevisionId forms.

`TestConstruction.cs` admits a bounded, non-executing constructor-provider proof.
One public constructor is required. Test arguments may come from the pinned
xUnit output helper/context accessor, declared class/collection/assembly fixtures,
or the framework's default/optional/parameter-array rules. Local nongeneric base
classes supply inherited fixture interfaces and collection attributes. The nearest
declaration suppresses inherited declarations only of the same concrete attribute
type; distinct inherited collection attribute types are ambiguous and reject. Collection names resolve to exactly one local
collection definition, and type/generic collection/assembly attributes are
supported. All declared local nongeneric fixtures must themselves have a sole
public constructor whose inputs come from the parent fixture scope or xUnit's
supported diagnostic/context provider. An external/generic fixture or inheritance
shape that cannot be resolved from these bytes fails as unsupported. This does
not prove constructor bodies, arbitrary custom factories or fixture lifecycle
behavior. The supplied assembly, attributes and constructors never execute here.

The rules follow pinned xUnit source revision `0c7aa4991f`:
[constructor defaults](https://github.com/xunit/xunit/blob/0c7aa4991f/src/xunit.v3.core/Runners/Reflection/XunitTestClassRunnerBase_reflection.cs),
[fixture scopes](https://github.com/xunit/xunit/blob/0c7aa4991f/src/xunit.v3.core/Utility/FixtureMappingManager_reflection.cs),
and [provider declarations](https://github.com/xunit/xunit/blob/0c7aa4991f/src/xunit.v3.core/Utility/ExtensibilityPointFactory.TestClass.cs).
Independent disposable tests run the actual pinned runner to check this supported
subset, including failed missing-provider controls. That execution belongs to
test-authored fixtures, not the evidence inspector.


Iteration 11 retains each InterfaceImplementation's annotations, and every retained
attribute's complete declaring type, selected constructor name/signature and value
blob, for local and referenced constructors. Method implementation flags (including
PreserveSig) and owning assembly flags participate in preservation. Owning assembly
and module contract attributes are compared exactly. The only excluded external
framework build stamps are AssemblyInformationalVersion and AssemblyFileVersion,
and AssemblyMetadata whose exact key is SourceRevisionId. Informational/source
revision is independently validated; other AssemblyMetadata keys remain contracts.
Owning assembly versions keep the existing upstream policy and may change. Public
root declarations remain unchanged; upstream additions remain conservative.

Test discovery requires one plain pinned Fact and exactly one resolvable Fact-derived
attribute. Theory and local direct/indirect Fact-derived conflicts reject. Unresolved
external custom method-attribute inheritance is outside the supported boundary and
fails explicitly. Attribute constructors and test code are never executed.

Framework exclusions and ordinary method-attribute admission require the actual
.NET 10 System.Runtime or System.Private.CoreLib assembly version, neutral culture,
flags and signing token. System namespaces and attribute-name suffixes alone grant
no framework identity; unresolved external declarations fail closed or remain
retained contracts.

Iteration 12 applies the same approved framework identity to both test and product
source stamps, exact supported constructor signatures and complete attribute blobs.
Local or external aliased informational/source metadata cannot attest a revision.
Task and ValueTask require the approved framework reference, and parameter-array
constructor supply requires the genuine ParamArrayAttribute constructor and blob.
Both genuine SDK informational-version forms and ordinary fixture/default/optional
constructor supply remain supported. An unsigned custom Task passing the runner
is still outside this inspector's supported framework-return subset. Prerequisite
SDK/build errors retain bounded separate diagnostic tails and the real exit code;
no evidence assembly or attribute is loaded or executed by inspection.

The fixed `tenantAccessInputs` operation uses the exact retained Commons
TryValidateTenantValue/TryValidateCallerPrincipalId methods. Native .NET validates
NFC, Trim, whitespace/control, reserved punctuation, and Unicode categories before
a claimed access read is admitted. Genuine Unicode tenants remain supported.
Default tests compile the locally retained actual Commons evaluator and compare
its allowed/denied result and zero-read behavior with this helper. These stricter
access-request rules do not change unrelated opaque identifier admission.

Iteration 13 validates the default actually selected after fixture/framework
provider lookup: exact supported constant type and byte length, Boolean/null/string
encoding, local enum underlying type, nullable primitive assignment, and genuine
DecimalConstant/DateTimeConstant constructor/blob. Null/default value types and
Optional fallback are admitted only for supported local/framework shapes. Nonnull
Nullable<Enum> constants reject, matching the actual pinned runner's conversion
failure. Unused defaults do not invalidate a supplied genuine provider. Unsupported
conversions fail explicitly rather than being assumed runnable.

AssemblyFixture, Collection and CollectionDefinition attributes require approved
xUnit ownership, exact supported constructor parameter types, prolog and complete
blob consumption. String, local Type, generic and inherited forms remain supported;
CollectionDefinition allows its actual DisableParallelization Boolean property.
All other named options, malformed counts/values and trailing data fail closed.
Task is encoded as class and ValueTask as valuetype; correct names alone do not
replace that signature check. These remain static metadata checks, not proof of
constructor bodies or arbitrary reflection behavior.

The helper's fixed invalid-evidence response is exit 1, empty stdout, and exact
stderr `Invalid native evidence.`. Other nonzero host exits (including missing
framework exit 150) are prerequisite failures with the real exit code and bounded
separate stream tails. They produce HelperUnavailable/BLOCKED, never evidence FAIL.

Decimal defaults use the trusted native sign semantics: zero is positive and every nonzero byte is negative. Scale remains bounded by the native decimal constructor, and the complete attribute signature and blob are required.
