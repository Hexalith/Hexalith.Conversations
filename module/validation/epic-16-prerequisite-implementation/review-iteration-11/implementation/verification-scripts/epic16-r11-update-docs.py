from pathlib import Path
import json
root=Path.cwd();review=root/'module/validation/epic-16-prerequisite-implementation/review-iteration-11';v=json.loads((review/'verification.json').read_bytes());out=review/'implementation';native=json.loads((out/'native-capture-fixture/provenance.json').read_bytes());helper=json.loads((out/'helper-toolchain/provenance.json').read_bytes())
combined=v['combined']['counts']['tests'];new=v['combined']['newSuccessorTests'];successor=v['combined']['successorTests'];seconds=round(v['combined']['command']['durationSeconds'],2);legacy_seconds=round(v['legacy']['command']['durationSeconds'],2)
body=f'''\n\n### Re-derived implementation verification — review iteration 11

All authorized prerequisite changes are implemented and verified. The parent retains
independent review and completion orchestration. Exactly 42 owned files were
hash-verified before restoration; the narrow evidence-packaging rule and
TestConstruction.cs remain preserved. All five protected inputs and all 1,623
prior indexed artifact hashes are unchanged. Final verification freezes 86 owned,
source, test and policy files, including the unchanged packaging rule; all 71 bound
source paths are preserved and no frozen bytes changed during verification.

- Conservative native declarations now retain interface implementation annotations,
  complete local/referenced attribute constructor identities/signatures/blobs,
  method implementation flags, owning assembly flags and assembly/module contract
  attributes. Only independently verified informational/source stamps and expected
  file-version metadata are excluded; other AssemblyMetadata remains contractual.
  Upstream version changes and legitimate additions remain supported. Both upstream
  additivity and unchanged root compiled-shape callers reject the compiled negative
  controls. These checks do not claim general binary or behavioral compatibility.
- P1's retained counterexamples showed that System namespaces/suffixes do not prove
  framework ownership. The fixed inspector now requires the approved .NET 10
  framework assembly name/version/culture/flags/signing identity for discovery
  and build-attribute exclusions. Genuine pinned xUnit execution proves plain Fact,
  Task/ValueTask and ordinary attributes, and rejects Fact+Theory plus local,
  inherited and external Fact-derived conflicts. Supplied evidence assemblies,
  attributes and constructors are never executed by the inspector.
- Tenant acknowledgement captures retain native source/delivery inputs, complete
  controlled before/after projection/checkpoint/health/pending contents, expected
  ETag and actual durable transaction. Executed successful commit and subsequent
  acknowledgement must observe those exact derived contents. A duplicate observes
  an already committed outcome with zero new writes; provider failure and retry
  exhaustion have no acknowledgement. Equal timestamps remain valid with ordered
  operations. P2's alternate/extra/empty native maps remain open to opaque keys,
  with actual role/string value validation instead of fixture-specific map keys.
- Each of the six isolated stale reconciliation branches retains correlated,
  executed and completed request/CAS/result operations, exact scope and immutable
  expected/proposed/stored-state digests. Duplicate retry executes after current
  wire clearance, observes the final durable state and returns AlreadyCompleted
  without writing. Independent branch clocks remain unordered. Multi-resource
  erase retains every per-resource dispatch reference and its multiplicity through
  all lifecycle boundaries and executed queries.
- All verifier launchers use exact declared dependency names and verify actual
  absence before emitting BLOCKED. Internal typo/import defects retain tracebacks.
  The primary producer documentation now specifies six runtime JSON fields,
  including loadedProducts, and distinguishes the five-field synthetic fixture
  shape and required retained product/capture inventory.
- Final combined verification: **{combined:,} passed** in **{seconds}s**, including
  all 2,152 prior cases plus {new} new cases ({successor:,} successor and 162 historical
  adoption-v2). Final legacy verification: **72 passed** in **{legacy_seconds}s**.
  Both runs have zero failures/errors/skips. Exact commands, environment, separate
  streams, JUnit and source hashes are retained in test-commands.json.
- **35 distinct guards across 36 recorded mutation runs** produce **162 targeted
  assertion failures and zero setup errors/skips**. One initial selector excluded
  the entire epic16 module and selected zero tests; it is retained as a selector
  error, never guard-sensitivity evidence. Its corrected run selects seven cases
  and detects all six stale-operation failures. The additional branch/scope and
  native-map schema mutations fail as required. Every mutant failure passes on
  final source. Exact mutant bytes, patches and the narrow mutated-schema trust
  adjustment are retained. Parent native/protocol/source/data checks are separate.
- Intermediate epochs remain intact: 245 passes/three analyzer-blocked deliberately
  conflicting Fact fixtures; then 279 passes/three external-library package-shape
  failures. Only the intentionally invalid Fact builds disable analyzers to reach
  actual runner discovery. The synthetic attribute library uses pinned 4.0.2
  extensibility-core; its corrected three controls pass. No product dependency,
  consumer validation or expected failure was weakened. Exact source snapshots,
  commands and separate streams describe each superseded epoch.
- Genuine pinned xUnit execution retains untouched raw TRX and seven explicitly
  synthetic product stand-ins. Final publication derives **{native['publicationPathCount']} paths**,
  including **{native['retainedAssemblyCount']} PE binaries**. Fresh-checkout regeneration and insertion
  pass after original measurement/build/payload locations disappear. All
  **{len(native['retained'])} exact committed fixture artifacts** and candidate/publication/lifecycle
  revisions are retained. Fixed helper provenance again verifies approved SDK
  10.0.402 and actual .NET 10.0.13. Fixtures grant no actual runtime or owner authority.
- All eighteen actual runtime/final-record commands remain nonempty **BLOCKED /
  exit 2 / ENTRY_QUALIFICATION_PENDING**. All 54 runtime paths remain absent
  (51 files and three binary directories). Both exact Story 7.3 guards and
  git diff --check pass, with prior operational bytes restored. All eighteen
  scenarios, thirty-two requirements, thirty-eight nodes, sixty-one edges,
  rollback obligations and predecessor pins are preserved. Entry remains null,
  trust empty, accepted CLI 4.35.2, Story 16.1 draft/backlog and hold ACTIVE.
- Evidence is indexed by
  `module/validation/epic-16-prerequisite-implementation/review-iteration-11/verification.json`.
  Exact disposable commit messages were validated with pinned commitlint before
  use. No skill was read/rendered, no secondary workflow, child agent or completion
  orchestration ran, and no actual repository staging/commit/push, dependency
  checkout update, application start or runtime publication occurred.
- The prohibited broad lane was not rerun. REFRESH_UV_DRIFT and Story 10.4 guidance
  binding remain unresolved and unweakened. Actual Platform integration qualification,
  seventeen adoption findings, provider/environment parity, recovery drills,
  operator/deputy coverage, capacity/waiver decisions, human SM-2 and separate owner
  entry remain outside this prerequisite implementation.
'''
for path in ('_bmad-output/implementation-artifacts/spec-epic-16-prerequisite-implementation.md','_bmad-output/implementation-artifacts/epic-16-prerequisite-implementation-handoff-2026-10-25.md'):
 p=root/path;s=p.read_text();assert '### Re-derived implementation verification — review iteration 11' not in s,path;p.write_text(s.rstrip()+body+'\n')
print(json.dumps(dict(updated=['spec','handoff'],combined=combined,newTests=new,legacy=72,publication=native['publicationPathCount'],retainedNative=len(native['retained']))))
