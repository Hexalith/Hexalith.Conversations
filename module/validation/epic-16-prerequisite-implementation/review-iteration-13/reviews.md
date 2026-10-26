# Independent review reports

All three context-free reviewers were launched before any report was handled;
all three final reports were received before triage. Review content:
`/tmp/epic16-review-next-4q9n86rc/complete.diff`, SHA-256
`ff594ee801a30bef3e9368781741c8ec9e33de3705be6bb50d5ea110a7595622`.

## Blind hunter

- Finding floor: `134,763,806 / 1,000 = 134,763.806 kB`; `N = min(floor(sqrt(134,763.806) + 1), 10) = 10`.

- Validate constructor default values against parameter types in `_bmad/scripts/epic16_native_helper/TestConstruction.cs:114`. The inspector accepts any nonempty Constant handle. A compiled constructor mutated from `(int value = 8675309, string other = "")` to `(string value = 8675309, string other = "")` passed inspection, while the actual pinned xUnit runner exited 1 with `System.Int32 cannot be converted to System.String`. Probe: `/tmp/review13-native-shape-z_y5nh90/default/`.

- Preserve the encoded class/value-type distinction in `_bmad/scripts/epic16_native_helper/Program.cs:254`. `ReadSignatureTypeCode()` collapses both encodings to `TypeHandle`, so the approved `Task` name and owner are accepted even when its signature incorrectly declares a value type. The mutated assembly passed inspection; the actual runner exited 1 with `TypeLoadException`, reporting a value-type mismatch and executing zero tests. Probe: `/tmp/review13-native-shape-z_y5nh90/return-kind/`.

- Fully validate fixture and collection attribute constructors and blobs in `_bmad/scripts/epic16_native_helper/TestConstruction.cs:180`. `FirstString()` reads the prologue and first argument but ignores named arguments and trailing bytes; generic fixture attributes receive even less blob validation. Changing a collection attribute’s named-argument count from zero to one without supplying an argument still passed inspection. Probe assembly: `/tmp/review13-blind-9r644fs3/bin/Debug/net10.0/Evidence.dll`; the mutation was applied in memory.

- Revalidate native TRX contents during final publication in `_bmad/scripts/epic16_capture.py:116`. `inventory()` rereads retained binaries and capture manifests, but excludes `row['bindings']['native']`. A TRX changed after scenario evaluation can therefore survive final inventory checks while the generated record retains its earlier hash. Include native results in the final digest and alias checks, with a regression that changes a TRX between evaluation and publication.

- Distinguish helper rejection from helper-host failure in `_bmad/scripts/epic16_native.py:139`. Every nonzero process exit becomes `ValueError('invalid native evidence')`, including missing-runtime or host-startup failures. Simulating .NET host exit 150 with a missing-runtime diagnostic produced that exception, which becomes evidence `FAIL` rather than prerequisite `BLOCKED`. Reserve the helper’s documented rejection exit for invalid evidence and retain bounded diagnostics for infrastructure failures.

- Extend the required loaded-product inventory in `_bmad/scripts/epic16_products.py:9` to cover the implementations whose behavior is asserted. It omits `Hexalith.EventStore.Server`, which owns the reconciliation coordinator/retry worker, and `Hexalith.Commons.TenantAccess`, which owns access evaluation. Those assemblies can differ between captures without affecting the seven-product consistency check. Bind their actual bytes and source identities, or explicitly limit the resulting implementation-consistency claim.

- Address private instance fields in public layout-bearing types in `_bmad/scripts/epic16_native_helper/ProductMetadata.cs:69`. The inspector records packing and explicit size but discards private fields that determine struct layout. Changing `public struct Api { private int value; public void Existing() {} }` to a `long` field produced identical API inventories. Include layout-relevant fields or explicitly reject unsupported layout preservation; the current inventory cannot establish that property.

- Capture `MethodImplementation` mappings in `_bmad/scripts/epic16_native_helper/ProductMetadata.cs:49`. Only public/protected method declarations and interface names are retained. Removing `void I.Existing() {}` from a class implementing an interface with a default `Existing()` method produced identical inventories, despite changing the interface dispatch target. Preserve these declaration-to-body mappings, or document and reject this unsupported contract shape.

- Reduce duplicated schema definitions in `_bmad/schemas/epic-16-tooling-v1.schema.json`. The new schema contains 36,023 lines and 1,376,094 bytes, with repeated nested observation structures. Extract shared `$defs`/`$ref` definitions or retain a deterministic schema generator so future changes to common captures cannot accidentally update only some scenario copies.

- Add a deduplication and retention strategy for `module/validation/epic-16-prerequisite-implementation/`. The supplied diff contains 11,818 validation paths accounting for 130,628,702 bytes of its 134,763,806 bytes. Repeated source trees, build outputs, and serialized inventories overwhelm the implementation review. A content-addressed artifact store with per-run manifests could preserve the evidence while substantially reducing repeated checked-in content.

## Edge-case hunter

```json
[
  {
    "location": "tests/tooling/test_ext_conv_ai_1_installed_delivery_packet.py:59-61",
    "trigger_condition": "Default pytest discovery runs on a checkout lacking the hardcoded external evidence directory",
    "guard_snippet": "if not PACKET.is_file(): self.skipTest(\"External installed evidence is unavailable\")",
    "potential_consequence": "All three tests fail with FileNotFoundError before assertions on clean machines."
  }
]
```

## Verification-gap reviewer

No verification gaps found.
