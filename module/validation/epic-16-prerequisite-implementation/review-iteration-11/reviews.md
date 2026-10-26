# Independent review 11

All three context-free reviewers completed before triage. These are their reports;
verdicts and independent reproduction evidence are recorded separately in the spec.

## Blind hunter

- Finding floor: 39,420,134 bytes ÷ 1,000 = 39,420.134 kB; N = min(floor(√39,420.134 + 1), 10) = 10.
- `_bmad/scripts/epic16_native_helper/ProductMetadata.cs:86` drops custom attributes on interface implementations. Compiled `Api : IBase<string>` and `Api : IBase<string?>` produce identical inventories and pass `additive`. Include interface-implementation attributes so nullable inheritance changes remain visible.
- `ProductMetadata.cs:125–128` identifies custom attributes by type and value blob, omitting their constructor signature. Compiled `[Stamp(1)]` and `[Stamp(1u)]`, with both constructor overloads present, compare identically. Retain the constructor identity and signature.
- `ProductMetadata.cs:61` omits `MethodDefinition.ImplAttributes`. Adding `[PreserveSig]` to a COM interface method produced an identical API inventory and passed preservation, although its interop calling contract changes. Compare these flags or reject unsupported interop declarations explicitly.
- `ProductMetadata.cs:24–38` reads assembly attributes only for source stamps; other assembly attributes never enter the API comparison. A compiled `CLSCompliant(true)` → `CLSCompliant(false)` change passes unchanged. Retain contract-relevant assembly/module attributes, with explicit exclusions for expected build metadata.
- `ProductMetadata.cs:89–95` omits the owning assembly’s flags. Adding `AssemblyFlags.Retargetable` produces identical represented identity fields and passes preservation. Include assembly flags in the identity comparison, consistent with the flags already retained for referenced assemblies.
- `_bmad/scripts/verify_story_16_1.py:12`, and the other two launchers, treat every missing name beginning `epic16_` as an unavailable dependency. A scratch reproduction importing nonexistent `epic16_typo` exits 2 with `TOOLING_DEPENDENCY_UNAVAILABLE`, hiding a coding defect. Use the generator’s exact dependency inventory and availability check.
- `_bmad/scripts/tests/test_epic16_tooling.py` has no automated CI entry point. Current workflows invoke `npm test`, which selects five unrelated `tests/tooling` unittest modules; default pytest collection also selects `tests/tooling`. Add an explicit CI lane for the new suite, install its Python dependencies, and give it a realistic timeout—the retained combined run took over 22 minutes.
- `_bmad/scripts/epic16_measurements.py:98–118` accepts acknowledgement durability from outcome names, IDs, timestamps, and summary booleans. Its capture has no successful transaction identity or persisted outcome contents. Extend this proof to assert executed commit/ack operations and the stored record, following the stronger `commit_before_ack` protocol already used elsewhere.
- `_bmad/scripts/epic16_schedules.py:108–132` lacks execution/completion evidence for reconciliation’s six stale attempts and duplicate retry. Expected fences, unchanged snapshots, and `succeeded=False`/`None` cannot distinguish an executed rejection from an operation never attempted. Require correlated executed operation observations, including the duplicate retry after clearance.
- `_bmad-output/planning-artifacts/epic-16-tooling-v1/README.md:21–23` describes runtime output as containing exactly five fields, omitting `loadedProducts`. Runtime admission requires that sixth field and rejects the documented payload with `PRODUCT_INVENTORY_INVALID`. Update the primary protocol description to distinguish runtime and fixture payloads and their required capture inventory.

## Edge-case hunter

```json
[
  {
    "location": "_bmad/scripts/epic16_native_helper/Program.cs:253-254",
    "trigger_condition": "A compiled scenario method has both Fact and Theory attributes.",
    "guard_snippet": "RequireExactlyOneFactDerivedAttribute(reader, method);",
    "potential_consequence": "Metadata admission accepts a method that pinned xUnit rejects without executing."
  },
  {
    "location": "_bmad/scripts/epic16_lifecycle.py:90-101",
    "trigger_condition": "An erase capture contains two distinct conversations, as permitted by the schema.",
    "guard_snippet": "index['dispatchReferences'] = [ref for part in components if part['kind'] == 'dispatch' for ref in part['value']]",
    "potential_consequence": "Generated component references contradict the index, preventing valid multi-conversation erasure evidence from passing."
  }
]
```

## Verification-gap reviewer

No verification gaps found.

## Reproduction provenance

The blind review's inline probes retained no binaries. The edge review's original
two-conversation clone did not repair its physical key or metadata digest; it was
only a helper-level observation. Parent reproductions therefore retain independent
compiled binaries and caller diagnostics, all three wrapper subprocesses, and a
corrected two-resource capture that passes the closed schema, target identity,
transaction and delivery guards before the reported query failure. The parent's
actual pinned xUnit run repeats the Fact/Theory discovery failure. These synthetic
counterexamples confer no runtime qualification.
