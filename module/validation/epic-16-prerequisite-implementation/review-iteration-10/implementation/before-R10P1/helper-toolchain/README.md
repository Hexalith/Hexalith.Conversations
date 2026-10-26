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
classes supply inherited fixture interfaces and collection attributes; the nearest
collection declaration wins. Collection names resolve to exactly one local
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
